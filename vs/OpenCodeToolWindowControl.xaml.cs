using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using OpenCodeStudio.Commands;
using OpenCodeStudio.Models;
using OpenCodeStudio.Resources;
using OpenCodeStudio.Services;

namespace OpenCodeStudio
{
    /// <summary>
    /// Hosts the OpenCode web UI inside a WebView2.
    /// <para>
    /// Unlike the original implementation, this control does <b>not</b> proxy
    /// requests through a fake origin nor inject scripts to fake per-project
    /// storage. It simply points WebView2 straight at the OpenCode server's own
    /// web UI (http://127.0.0.1:port/{dir}/session/{id}), which is a
    /// self-contained single-page app that manages projects, sessions and its
    /// own theming. This removes an entire, fragile translation layer.
    /// </para>
    /// </summary>
    public partial class OpenCodeToolWindowControl : UserControl, IDisposable
    {
        private IServiceProvider _serviceProvider;

        private CoreWebView2Environment _environment;
        private IProjectRootResolver _projectRootResolver;
        private ServerController _serverController;

        private string _currentProjectRoot;
        private bool _isDisposed;
        private bool _isStarting;
        private bool _retryDisabled;
        private System.Threading.Timer _projRootTimer;

        private SpendMonitor _spendMonitor;
        private SpendSettings _spendSettings;
        private bool _loginInProgress;
        private bool _loginMode;
        private bool _autoOpenAfterLogin;
        private System.Threading.Tasks.TaskCompletionSource<bool> _loginTcs;

        // Токен жизни окна: отменяет длительные операции (ожидание логина) при закрытии.
        private readonly System.Threading.CancellationTokenSource _lifetimeCts = new System.Threading.CancellationTokenSource();

        /// <summary>Обновление снапшота расходов (на UI-потоке).</summary>
        public event Action<SpendSnapshot> SnapshotUpdated;

        /// <summary>Последний полученный снапшот расходов.</summary>
        public SpendSnapshot LatestSnapshot { get; private set; }

        private bool _isReconnecting;
        private System.Threading.CancellationTokenSource _reconnectCts;
        private const int MaxReconnectAttempts = 30;
        private const int ReconnectDelayMs = 3000;

        private static readonly string ErrorPageTemplate;
        private static readonly string LoadingPageTemplate;

        static OpenCodeToolWindowControl()
        {
            var assembly = Assembly.GetExecutingAssembly();
            ErrorPageTemplate = LoadResourceString(assembly, "OpenCodeStudio.Resources.ErrorPage.html");
            LoadingPageTemplate = LoadResourceString(assembly, "OpenCodeStudio.Resources.LoadingPage.html");
        }

        private static string LoadResourceString(Assembly assembly, string name)
        {
            using (var stream = assembly.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public OpenCodeToolWindowControl()
        {
            InitializeComponent();
            _ = InitWebViewCoreAsync()
                .ContinueWith(_ => WaitServerAsync(), TaskScheduler.Current);
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void SetSpendSettings(SpendSettings settings)
        {
            _spendSettings = settings;
        }

        /// <summary>
        /// Set the shared server controller. Must be called before StartAsync.
        /// </summary>
        public void SetServerController(ServerController controller)
        {
            if (_serverController == controller) return;

            if (_serverController != null)
            {
                _serverController.ConnectionLost -= OnServerConnectionLost;
                _serverController.ConnectionRestored -= OnServerConnectionRestored;
            }

            _serverController = controller;
            if (_serverController != null)
            {
                _serverController.ConnectionLost += OnServerConnectionLost;
                _serverController.ConnectionRestored += OnServerConnectionRestored;
            }
        }

        private void SetCurrentProjectRoot(string root)
        {
            _currentProjectRoot = root;
        }

        private Dictionary<string, string> GetThemeColors()
        {
            var colors = new Dictionary<string, string>();
            try
            {
                colors["bg"] = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                colors["textPrimary"] = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                colors["textSecondary"] = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarTextInactiveColorKey));
                colors["textAccent"] = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.AccentBorderColorKey));
                colors["border"] = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey));
            }
            catch { }
            return colors;
        }

        private string GetThemeStyleBlock()
        {
            var c = GetThemeColors();
            string bg = c.TryGetValue("bg", out var v0) ? v0 : "#252526";
            string text = c.TryGetValue("textPrimary", out var v1) ? v1 : "#f1f1f1";
            string textMuted = c.TryGetValue("textSecondary", out var v2) ? v2 : "#999999";
            string accent = c.TryGetValue("textAccent", out var v3) ? v3 : "#007acc";
            string border = c.TryGetValue("border", out var v4) ? v4 : "#434346";
            var accentHover = LighterHex(accent, 0.20);
            var spinnerTrack = LighterHex(bg, 0.12);

            return $@"<style id=""ocstudio-page-theme"">
    :root {{
        --vs-bg: {bg};
        --vs-text: {text};
        --vs-text-muted: {textMuted};
        --vs-accent: {accent};
        --vs-accent-hover: {accentHover};
        --vs-border: {border};
        --vs-spinner-track: {spinnerTrack};
    }}
</style>";
        }

        private static string LighterHex(string hex, double amount)
        {
            try
            {
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                r = Math.Min(255, (int)(r + (255 - r) * amount));
                g = Math.Min(255, (int)(g + (255 - g) * amount));
                b = Math.Min(255, (int)(b + (255 - b) * amount));
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch { return hex; }
        }

        private static string ToHtml(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private async Task InitWebViewCoreAsync()
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OpenCodeStudio",
                    "WebView2");

                Directory.CreateDirectory(userDataFolder);

                _environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder);

                await webView.EnsureCoreWebView2Async(_environment);

                var core = webView.CoreWebView2;

                core.WebMessageReceived += OnWebMessageReceived;
                core.NewWindowRequested += OnNewWindowRequested;
                core.NavigationStarting += OnNavigationStarting;
                core.NavigationCompleted += OnNavigationCompleted;

                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.IsZoomControlEnabled = true;

                if (_spendSettings == null || _spendSettings.EnableInjection)
                {
                    try
                    {
                        await UsageInjector.InstallAsync(core);
                    }
                    catch (Exception ex)
                    {
                        Services.Log.Error("UsageInjector install failed", ex);
                    }
                }

                await ShowLoadingPageAsync(StringsHelper.UILoading);
            }
            catch (Exception ex)
            {
                Services.Log.Error("WebView2 init failed", ex);
                await ShowErrorPageAsync(
                    $"{StringsHelper.ErrorWebViewInitFailed}: {ex}", false);
            }
        }

        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Во время входа OAuth открывается как новое окно: оставляем навигацию
            // внутри WebView2, иначе вход не завершается (cookie не появляется).
            if (_loginMode)
            {
                try { e.Handled = true; webView.CoreWebView2.Navigate(e.Uri); } catch { }
                return;
            }

            // Let target=_blank links open in the user's real browser instead of
            // spawning an unmanaged popup inside the tool window.
            try
            {
                e.Handled = true;
                Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
            }
            catch { }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!_loginMode) return;
            var u = e.Uri ?? "";
            if (!ConsoleLoginService.IsReturnedToApp(u)) return;
            // opencode вернул нас на свою страницу — вход выполнен. Отменяем загрузку
            // консоли и сразу забираем cookie.
            e.Cancel = true;
            _loginMode = false;
            _ = FinishConsoleCaptureAsync();
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Debug.WriteLine($"Navigation failed: {e.WebErrorStatus}");
                return;
            }
            PushUsageState(_autoOpenAfterLogin);
            _autoOpenAfterLogin = false;
        }

        private async Task WaitServerAsync()
        {
            await Task.Delay(500);
            if (_serverController == null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ShowOpenCodeWindowCommand.Instance.RefreshWindow();
            }
        }

        public async Task StartAsync()
        {
            if (_isStarting) return;
            _isStarting = true;

            try
            {
                if (webView.CoreWebView2 != null && _serverController != null)
                {
                    var isRunning = _serverController.State == ConnectionState.Connected;
                    Debug.WriteLine($"Tool window init: server running={isRunning}, proj={_currentProjectRoot}");
                }

                await StartFlowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Start flow failed: {ex}");
                await ShowErrorPageAsync($"Failed: {ex}", true);
            }
            finally
            {
                _isStarting = false;
            }
        }

        public void OnWindowClosing()
        {
            _projRootTimer?.Dispose();
            _projRootTimer = null;
            CancelReconnect();
        }

        private void OnServerConnectionLost()
        {
            var state = _serverController?.State ?? ConnectionState.Disconnected;
            Debug.WriteLine($"Server connection lost! State: {state}. Auto-reconnecting...");
            BeginAutoReconnect();
        }

        private void OnServerConnectionRestored()
        {
            Debug.WriteLine("Server connection restored!");
            if (_isReconnecting)
                return;
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var url = _serverController?.GetHomeUrl();
                if (url != null && webView.CoreWebView2 != null)
                    webView.CoreWebView2.Navigate(url);
                else
                    await StartFlowAsync();
            });
        }

        /// <summary>
        /// Automatically tries to restore the connection instead of showing an
        /// error page that requires a manual Retry click. Shows a progress page
        /// with the current attempt number and keeps retrying until the server
        /// is healthy again (or the attempt budget is exhausted).
        /// </summary>
        private void BeginAutoReconnect()
        {
            if (_isReconnecting || _isDisposed) return;
            _isReconnecting = true;

            try { _reconnectCts?.Cancel(); } catch { }
            _reconnectCts = new System.Threading.CancellationTokenSource();
            var ct = _reconnectCts.Token;

#pragma warning disable VSSDK007
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    int consecutiveFailures = 0;
                    for (int attempt = 1; attempt <= MaxReconnectAttempts && !ct.IsCancellationRequested; attempt++)
                    {
                        await ShowLoadingPageAsync($"{StringsHelper.UIReconnecting} ({attempt})");

                        try
                        {
                            var healthy = await _serverController.ServerService.CheckHealthAsync();

                            if (!healthy)
                            {
                                consecutiveFailures++;
                                // Give the connection a few quick retries before doing the
                                // heavy restart, so a transient blip does not kill a
                                // perfectly healthy server.
                                if (consecutiveFailures >= 3)
                                {
                                    var started = await _serverController.StartAsync(_currentProjectRoot);
                                    healthy = started && _serverController.State == ConnectionState.Connected;
                                    consecutiveFailures = 0;
                                }
                            }
                            else
                            {
                                consecutiveFailures = 0;
                            }

                            if (healthy)
                            {
                                Debug.WriteLine($"Reconnected on attempt {attempt}");
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                var url = _serverController.GetHomeUrl();
                                if (url != null && webView.CoreWebView2 != null)
                                    NavigateToSession(url);
                                else
                                    await StartFlowAsync();
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Reconnect attempt {attempt} failed: {ex}");
                        }

                        try { await Task.Delay(ReconnectDelayMs, ct); }
                        catch (OperationCanceledException) { return; }
                    }

                    if (!ct.IsCancellationRequested)
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        await ShowErrorPageAsync(StringsHelper.ErrorConnectionLost, true);
                    }
                }
                finally
                {
                    _isReconnecting = false;
                }
            });
#pragma warning restore VSSDK007
        }

        private void CancelReconnect()
        {
            try { _reconnectCts?.Cancel(); } catch { }
            _reconnectCts?.Dispose();
            _reconnectCts = null;
            _isReconnecting = false;
        }

        private async Task StartFlowAsync()
        {
            await ShowLoadingPageAsync(StringsHelper.UIConnecting);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_projectRootResolver == null)
                _projectRootResolver = new ProjectRootResolver(_serviceProvider);
            var newProjectRoot = _projectRootResolver.ResolveProjectRoot();
            Debug.WriteLine($"Project root: {newProjectRoot} (current: {_currentProjectRoot})");

            if (_serverController == null) return;

            if (string.Equals(newProjectRoot, _currentProjectRoot, StringComparison.OrdinalIgnoreCase)
                && _serverController.State == ConnectionState.Connected
                && !string.IsNullOrEmpty(_serverController.CurrentSessionId))
            {
                var existingUrl = _serverController.GetHomeUrl();
                if (existingUrl != null)
                {
                    NavigateToSession(existingUrl);
                    return;
                }
            }

            SetCurrentProjectRoot(newProjectRoot);

            var success = await _serverController.StartAsync(_currentProjectRoot);
            if (!success)
            {
                await ShowErrorPageAsync(StringsHelper.ErrorServerStartFailed, true);
                return;
            }

            var sessionUrl = _serverController.GetHomeUrl();
            if (sessionUrl != null)
                NavigateToSession(sessionUrl);

            _projRootTimer?.Dispose();
            _projRootTimer = new System.Threading.Timer(_ =>
            {
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        if (_serverController?.ServerService != null)
                        {
                            var healthy = await _serverController.ServerService.CheckHealthAsync();
                            if (!healthy && _serverController.State != ConnectionState.Connecting
                                && !_isStarting && !_isReconnecting)
                            {
                                BeginAutoReconnect();
                                return;
                            }
                        }

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        if (_projectRootResolver == null)
                            _projectRootResolver = new ProjectRootResolver(_serviceProvider);
                        var resolved = _projectRootResolver.ResolveProjectRoot();
                        if (!string.Equals(resolved, _currentProjectRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"proj_root changed: {_currentProjectRoot} -> {resolved}");
                            SetCurrentProjectRoot(resolved);
                            _serverController?.UpdateProjectRoot(resolved);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"timer: {ex}");
                    }
                });
            }, null, 5000, 5000);

            if (_serverController.State == ConnectionState.Connected)
                StartSpendMonitor();
        }

        /// <summary>Запускает фоновый монитор расхода, если инъекция включена.</summary>
        private void StartSpendMonitor()
        {
            if (_spendSettings == null || !_spendSettings.EnableInjection) return;

            if (_spendMonitor != null) return;

            _spendMonitor = new SpendMonitor(_spendSettings);
            _spendMonitor.Updated += OnSpendUpdated;
            _spendMonitor.AuthRequired += OnSpendAuthRequired;
            _spendMonitor.Start();
        }

        private void OnSpendUpdated(SpendSnapshot s)
        {
#pragma warning disable VSSDK007
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var core = webView?.CoreWebView2;
                    if (core == null) return;
                    LatestSnapshot = s;
                    SnapshotUpdated?.Invoke(s);
                    PushUsageState();
                }
                catch (Exception ex)
                {
                    Services.Log.Error("Failed to push usage to WebView2", ex);
                }
            });
#pragma warning restore VSSDK007
        }

        private void OnSpendAuthRequired()
        {
            // Авто-вход больше не запускаем: показываем состояние входа в поповере.
#pragma warning disable VSSDK007
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                PushUsageState();
            });
#pragma warning restore VSSDK007
        }

        /// <summary>Отправляет текущее состояние использования в веб-UI (UI-поток).</summary>
        private void PushUsageState(bool autoOpen = false)
        {
            try
            {
                var core = webView?.CoreWebView2;
                if (core == null) return;
                var state = ConsoleSessionStore.Load() == null ? "login" : (LatestSnapshot != null ? "ready" : "loading");
                UsageInjector.Push(core, UsageInjector.BuildPayload(state, LatestSnapshot, _spendSettings?.ShowLimits ?? true, autoOpen));
            }
            catch (Exception ex) { Services.Log.Error("Failed to push usage state", ex); }
        }

        private async Task FinishConsoleCaptureAsync()
        {
            try
            {
                var core = webView?.CoreWebView2;
                if (core == null) { _loginTcs?.TrySetResult(false); return; }
                var cookie = await ConsoleLoginService.ReadCookieAsync(core);
                if (string.IsNullOrEmpty(cookie))
                {
                    await Task.Delay(900); // сессия может записаться чуть позже
                    cookie = await ConsoleLoginService.ReadCookieAsync(core);
                }
                if (!string.IsNullOrEmpty(cookie))
                {
                    ConsoleSessionStore.Save(cookie);
                    Services.Log.Info("Console login: cookie captured");
                    _loginTcs?.TrySetResult(true);
                    return;
                }
                Services.Log.Warn("Console login: returned but no cookie found");
                _loginTcs?.TrySetResult(false);
            }
            catch (Exception ex)
            {
                Services.Log.Error("Console login: capture failed", ex);
                _loginTcs?.TrySetResult(false);
            }
        }

        private async Task BeginConsoleLoginAsync()
        {
            if (_loginInProgress) return;
            _loginInProgress = true;
            try
            {
                _loginTcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                _loginMode = true;
                await ShowLoadingPageAsync("Вход в OpenCode Console...");
                var core = webView?.CoreWebView2;
                if (core != null)
                    core.Navigate(ConsoleLoginService.LoginUrl);

                var timeout = Task.Delay(TimeSpan.FromMinutes(5));
                var finished = await Task.WhenAny(_loginTcs.Task, timeout);
                var ok = finished == _loginTcs.Task && _loginTcs.Task.Result;
                Services.Log.Info("Console login finished, success=" + ok);
            }
            catch (Exception ex)
            {
                Services.Log.Error("Console login failed", ex);
            }
            finally
            {
                try
                {
                    _autoOpenAfterLogin = true;
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var home = _serverController?.GetHomeUrl();
                    if (home != null && webView?.CoreWebView2 != null)
                    {
                        Services.Log.Info("Console login: returning to " + home);
                        webView.CoreWebView2.Navigate(home);
                    }
                    else
                    {
                        PushUsageState(true);
                    }
                    _spendMonitor?.Stop();
                    _spendMonitor = null;
                    StartSpendMonitor();
                }
                catch (Exception ex)
                {
                    Services.Log.Error("Console login: failed to return to app", ex);
                }
                _loginInProgress = false;
                _loginMode = false;
            }
        }

        private void NavigateToSession(string sessionUrl)
        {
            if (webView.CoreWebView2 == null) return;
            Services.Log.Info($"Navigating to {sessionUrl}");
            webView.CoreWebView2.Navigate(sessionUrl);
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
            if (message == "login")
            {
#pragma warning disable VSSDK007
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(() => BeginConsoleLoginAsync());
#pragma warning restore VSSDK007
                return;
            }
            if (message == "retry" && !_retryDisabled)
            {
                _retryDisabled = true;
#pragma warning disable VSSDK007
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    try
                    {
                        await StartFlowAsync();
                    }
                    finally
                    {
                        _retryDisabled = false;
                    }
                });
#pragma warning restore VSSDK007
            }
        }

        private async Task ShowLoadingPageAsync(string message)
        {
            var html = LoadingPageTemplate
                .Replace("{theme-style}", GetThemeStyleBlock())
                .Replace("{message}", message);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.NavigateToString(html);
            }
        }

        private async Task ShowErrorPageAsync(string message, bool showRetry)
        {
            var escapedMessage = System.Net.WebUtility.HtmlEncode(message)
                .Replace("\n", "<br>")
                .Replace("\\n", "<br>");
            var retryButton = showRetry
                ? $@"<button id=""retryBtn"" onclick=""handleRetry()"">{StringsHelper.UIRetry}</button>"
                : "";
            var retryScript = showRetry
                ? @"var disabled=false;function handleRetry(){if(disabled)return;disabled=true;var b=document.getElementById('retryBtn');b.disabled=true;b.style.opacity='0.5';b.style.cursor='not-allowed';try{window.chrome.webview.postMessage('retry')}catch(e){}}"
                : "";

            var html = ErrorPageTemplate
                .Replace("{theme-style}", GetThemeStyleBlock())
                .Replace("{message}", escapedMessage)
                .Replace("{retryButton}", retryButton)
                .Replace("{retryScript}", retryScript);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.NavigateToString(html);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try { _lifetimeCts.Cancel(); _lifetimeCts.Dispose(); } catch { }
            _projRootTimer?.Dispose();
            CancelReconnect();
            if (_spendMonitor != null)
            {
                _spendMonitor.Updated -= OnSpendUpdated;
                _spendMonitor.AuthRequired -= OnSpendAuthRequired;
            }
            _spendMonitor?.Dispose();
            _spendMonitor = null;
            webView?.Dispose();
        }
    }
}
