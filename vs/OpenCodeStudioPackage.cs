using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using OpenCodeStudio.Commands;
using OpenCodeStudio.Services;

namespace OpenCodeStudio
{
    [Guid("7E4A9C31-2B6D-4F58-9A10-3C8E7B5D1F20")]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(OpenCodeToolWindow))]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(Services.SpendSettings), "OpenCode Studio", "Usage & Limits", 0, 0, true)]
    public sealed class OpenCodeStudioPackage : AsyncPackage
    {
        public const string PackageGuidString = "7E4A9C31-2B6D-4F58-9A10-3C8E7B5D1F20";

        private DTE _dte;
        private SolutionEvents _solutionEvents;

        /// <summary>
        /// Shared server controller вЂ” survives across tool window open/close.
        /// </summary>
        private ServerController _serverController;

        private OpenCodeToolWindowControl _toolWindowControl;
        private CancellationTokenSource _extensionUpdateCts;
        private volatile bool _updateWindowOpen;
        private CancellationTokenSource _openCodeUpdateCts;
        private volatile bool _openCodeUpdateWindowOpen;

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _serverController = new ServerController(() => GetSpendSettings()?.UseOpenCodeV2 ?? false);

            Services.SpendSettings.Applied += OnSpendSettingsApplied;

            await ShowOpenCodeWindowCommand.InitializeAsync(this);
            await ShowImportCommand.InitializeAsync(this);

            _dte = await GetServiceAsync(typeof(DTE)) as DTE;
            if (_dte != null)
            {
                _solutionEvents = _dte.Events.SolutionEvents;
                _solutionEvents.Opened += OnSolutionOpened;
            }

            // First run (or fresh reinstall): offer to bring in settings, themes,
            // credentials and chat history from any detected OpenCode environment.
            if (!Onboarding.IsCompleted())
            {
                _ = JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        // Let the shell finish initializing before showing a window.
                        await Task.Delay(2500);
                        await JoinableTaskFactory.SwitchToMainThreadAsync();
                        new FirstRunWindow(new ImportService(), GetSolutionDir(), GetSpendSettings()).Show();
                    }
                    catch (Exception ex)
                    {
                        Services.Log.Error("First-run wizard failed", ex);
                    }
                });
            }

            StartExtensionUpdateLoop();
            StartOpenCodeUpdateLoop();
        }

        /// <summary>
        /// Фоновая проверка новой версии самого opencode: первая через ~30 c,
        /// затем каждые 60 c. Показывает окно только если есть новее и пользователь
        /// ещё не отклонил его в этой сессии VS.
        /// </summary>
        private void StartOpenCodeUpdateLoop()
        {
            _openCodeUpdateCts = new CancellationTokenSource();
            var ct = _openCodeUpdateCts.Token;
            var service = new OpenCodeUpdateService();

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(30000, ct);
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            var installed = await service.GetInstalledVersionAsync();
                            var latest = await service.GetLatestVersionAsync();
                            if (!string.IsNullOrEmpty(installed) && !string.IsNullOrEmpty(latest) &&
                                OpenCodeUpdateService.IsNewer(latest, installed) &&
                                !OpenCodeUpdateWindow.Suppressed && !_openCodeUpdateWindowOpen)
                            {
                                await JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                                if (!OpenCodeUpdateWindow.Suppressed && !_openCodeUpdateWindowOpen)
                                {
                                    var window = new OpenCodeUpdateWindow(installed, latest);
                                    _openCodeUpdateWindowOpen = true;
                                    window.Closed += (_, __) => _openCodeUpdateWindowOpen = false;
                                    window.Show();
                                }
                            }
                        }
                        catch (OperationCanceledException) { return; }
                        catch (Exception ex)
                        {
                            Services.Log.Warn("OpenCode update check failed: " + ex.Message);
                        }

                        await Task.Delay(60000, ct);
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        /// <summary>
        /// Фоновая проверка новой версии самого расширения: первая через ~20 c,
        /// затем каждые 60 c. Показывает окно только если есть новее и пользователь
        /// ещё не отклонил его в этой сессии VS.
        /// </summary>
        private void StartExtensionUpdateLoop()
        {
            _extensionUpdateCts = new CancellationTokenSource();
            var ct = _extensionUpdateCts.Token;
            var service = new ExtensionUpdateService();

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(20000, ct);
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            var info = await service.CheckAsync();
                            if (info != null && info.IsNewer &&
                                !UpdateNotificationWindow.Suppressed && !_updateWindowOpen)
                            {
                                await JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                                var checkForUpdates = GetSpendSettings()?.CheckForUpdates ?? true;
                                if (checkForUpdates && !UpdateNotificationWindow.Suppressed && !_updateWindowOpen)
                                {
                                    var window = new UpdateNotificationWindow(info, () =>
                                    {
                                        JoinableTaskFactory.Run(async () =>
                                        {
                                            await JoinableTaskFactory.SwitchToMainThreadAsync();
                                            if (!TryOpenExtensionManager())
                                                throw new InvalidOperationException("Extension Manager command not found");
                                            Services.Log.Info("Opened Extension Manager");
                                        });
                                    });
                                    _updateWindowOpen = true;
                                    window.Closed += (_, __) => _updateWindowOpen = false;
                                    window.Show();
                                }
                            }
                        }
                        catch (OperationCanceledException) { return; }
                        catch (Exception ex)
                        {
                            Services.Log.Warn("Extension update check failed: " + ex.Message);
                        }

                        await Task.Delay(60000, ct);
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        /// <summary>
        /// Надёжно открывает Manage Extensions внутри VS. Вызывать с UI-потока.
        /// </summary>
        private bool TryOpenExtensionManager()
        {
            if (_dte == null) { Services.Log.Info("Extension Manager: DTE is null"); return false; }

            // 1) известные имена команд (VS 2026 — ManageExtensions, затем VS 2022 и старые)
            var candidates = new[] {
                "ManageExtensions", "Extensions.ManageExtensions", "Extensions.ManageExtensionsDialog",
                "Extensions.ExtensionsAndUpdates", "Tools.ManageExtensions",
                "Tools.ExtensionsAndUpdates"
            };

            // 2) открыть сразу вкладку Updates: сначала с аргументом, затем без
            try
            {
                _dte.ExecuteCommand("ManageExtensions", "Updates");
                Services.Log.Info("Extension Manager opened (Updates tab) via command: ManageExtensions Updates");
                return true;
            }
            catch (Exception ex) { Services.Log.Info("Extension Manager: 'ManageExtensions Updates' -> " + ex.Message); }

            try
            {
                _dte.ExecuteCommand("ManageExtensions");
                Services.Log.Info("Extension Manager opened via command: ManageExtensions");
                return true;
            }
            catch (Exception ex) { Services.Log.Info("Extension Manager: 'ManageExtensions' -> " + ex.Message); }

            // 3) поиск среди всех команд DTE: Update + (Extension|Manage)
            try
            {
                foreach (EnvDTE.Command cmd in _dte.Commands)
                {
                    var n = cmd?.Name;
                    if (string.IsNullOrEmpty(n)) continue;
                    if (n.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (n.IndexOf("Extension", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Manage", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        try
                        {
                            _dte.ExecuteCommand(n, "Updates");
                            Services.Log.Info("Extension Manager opened (Updates tab) via DTE search: " + n);
                            return true;
                        }
                        catch
                        {
                            try { _dte.ExecuteCommand(n); Services.Log.Info("Extension Manager opened via DTE search: " + n); return true; } catch { }
                        }
                    }
                }
            }
            catch (Exception ex) { Services.Log.Info("Extension Manager: DTE enumeration failed: " + ex.Message); }

            // 4) остальные известные кандидаты
            foreach (var name in candidates)
            {
                try { _dte.ExecuteCommand(name); Services.Log.Info("Extension Manager opened via command: " + name); return true; }
                catch (Exception ex) { Services.Log.Info("Extension Manager: '" + name + "' -> " + ex.Message); }
            }

            // 5) перебор ManageExtensions / ExtensionsAndUpdates среди всех команд DTE (как было)
            try
            {
                foreach (EnvDTE.Command cmd in _dte.Commands)
                {
                    var n = cmd?.Name;
                    if (string.IsNullOrEmpty(n)) continue;
                    if (n.IndexOf("ManageExtensions", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("ExtensionsAndUpdates", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try { _dte.ExecuteCommand(n); Services.Log.Info("Extension Manager opened via DTE search: " + n); return true; } catch { }
                    }
                }
            }
            catch (Exception ex) { Services.Log.Info("Extension Manager: DTE enumeration failed: " + ex.Message); }

            // 6) поиск по меню/тулбарам (по подписи), устойчиво к локализации
            try
            {
                var needles = new[] { "manage extensions", "extensions and updates", "управление расширениями" };
                if (_dte.CommandBars is System.Collections.IEnumerable bars)
                {
                    foreach (Microsoft.VisualStudio.CommandBars.CommandBar bar in bars)
                    {
                        var hit = FindControlRecursive(bar.Controls, needles);
                        if (hit != null)
                        {
                            try { hit.Execute(); Services.Log.Info("Extension Manager opened via menu caption: " + hit.Caption); return true; } catch { }
                        }
                    }
                }
            }
            catch (Exception ex) { Services.Log.Info("Extension Manager: CommandBars search failed: " + ex.Message); }

            // 7) диагностика в лог: команды со словом extension
            try
            {
                int logged = 0;
                foreach (EnvDTE.Command cmd in _dte.Commands)
                {
                    var n = cmd?.Name;
                    if (!string.IsNullOrEmpty(n) && n.IndexOf("extension", StringComparison.OrdinalIgnoreCase) >= 0 && logged < 30)
                    { Services.Log.Info("DTE ext command candidate: " + n); logged++; }
                }
                Services.Log.Info("Extension Manager: no command matched (logged " + logged + " candidates)");
            }
            catch { }

            return false;
        }

        private static Microsoft.VisualStudio.CommandBars.CommandBarControl FindControlRecursive(Microsoft.VisualStudio.CommandBars.CommandBarControls controls, string[] needles)
        {
            try
            {
                foreach (Microsoft.VisualStudio.CommandBars.CommandBarControl c in controls)
                {
                    var cap = (c?.Caption ?? "").Replace("&", "").Trim().ToLowerInvariant();
                    foreach (var n in needles)
                        if (cap.Contains(n)) return c;

                    if (c is Microsoft.VisualStudio.CommandBars.CommandBarPopup popup)
                    {
                        var sub = FindControlRecursive(popup.Controls, needles);
                        if (sub != null) return sub;
                    }
                }
            }
            catch { }
            return null;
        }

        private Services.SpendSettings GetSpendSettings()
            => (Services.SpendSettings)GetDialogPage(typeof(Services.SpendSettings));

        private void OnSpendSettingsApplied()
        {
            _ = JoinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();
                    _serverController?.Stop();
                    await RefreshOpenCodeWindowAsync();
                }
                catch (Exception ex)
                {
                    Services.Log.Error("Restart after settings change failed", ex);
                }
            });
        }

        private string GetSolutionDir()
        {
            try
            {
                var full = _dte?.Solution?.FullName;
                if (!string.IsNullOrEmpty(full))
                    return System.IO.Path.GetDirectoryName(full);
            }
            catch { }
            return null;
        }

        private void OnSolutionOpened()
        {
            _ = JoinableTaskFactory.RunAsync(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                await Task.Delay(1000);
                await RefreshOpenCodeWindowAsync();
            });
        }

        internal async Task ShowOpenCodeWindowAsync()
        {
            var window = await ShowToolWindowAsync(
                typeof(OpenCodeToolWindow), 0, create: true, DisposalToken);

            if (window is OpenCodeToolWindow toolWindow && toolWindow?.Control != null)
            {
                toolWindow.SetServiceProvider(this);
                _toolWindowControl = toolWindow.Control;
                toolWindow.Control.SetServerController(_serverController);
                toolWindow.Control.SetSpendSettings(GetSpendSettings());

                await toolWindow.Control.StartAsync();
            }
        }

        internal async Task RefreshOpenCodeWindowAsync()
        {
            var window = await FindToolWindowAsync(
                typeof(OpenCodeToolWindow), 0, false, DisposalToken);

            if (window is OpenCodeToolWindow toolWindow && toolWindow?.Control != null)
            {
                _toolWindowControl = toolWindow.Control;
                toolWindow.Control.SetServerController(_serverController);
                toolWindow.Control.SetSpendSettings(GetSpendSettings());
                await toolWindow.Control.StartAsync();
            }
        }

        internal async Task ShowUsageWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var w = new UsageWindow(_toolWindowControl, GetSpendSettings()?.ShowLimits ?? true);
            w.Show();
        }

        /// <summary>
        /// Opens the import/setup wizard on demand (also shown automatically on
        /// first run).
        /// </summary>
        internal async Task ShowImportWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var window = new FirstRunWindow(new ImportService(), GetSolutionDir(), GetSpendSettings());
            window.Show();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _extensionUpdateCts?.Cancel();
                _extensionUpdateCts?.Dispose();
                _openCodeUpdateCts?.Cancel();
                _openCodeUpdateCts?.Dispose();
                _serverController?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
