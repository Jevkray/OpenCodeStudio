using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using OpenCodeStudio.Services;

namespace OpenCodeStudio
{
    /// <summary>
    /// Standalone window that renders the OpenCode usage statistics dashboard
    /// (cost, tokens, requests and charts) in a WebView2, themed like Visual Studio.
    /// </summary>
    public partial class UsageStatsWindow : Window
    {
        private readonly ServerController _serverController;
        private bool _initialized;

        private static readonly string PageTemplate = LoadTemplate();

        public UsageStatsWindow(ServerController serverController)
        {
            InitializeComponent();
            _serverController = serverController;
            Loaded += OnLoaded;
        }

        private static string LoadTemplate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("OpenCodeStudio.Resources.StatsPage.html"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OpenCodeStudio", "StatsWebView2");
                Directory.CreateDirectory(userDataFolder);

                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(environment);

                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stats window init failed: {ex}");
                ShowMessage("Failed to initialize statistics view: " + ex.Message);
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (e.TryGetWebMessageAsString() == "stats-refresh")
                _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            try
            {
                var stats = await new StatsService(_serverController?.ServerService)
                    .BuildAsync(_serverController?.CurrentSessionId);

                var json = stats != null ? stats.ToString(Formatting.None) : "null";

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var html = PageTemplate
                    .Replace("{theme-style}", BuildThemeStyle())
                    .Replace("{stats-json}", json);

                if (webView.CoreWebView2 != null)
                    webView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stats refresh failed: {ex}");
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ShowMessage("Failed to load statistics: " + ex.Message);
            }
        }

        private void ShowMessage(string message)
        {
            try
            {
                var html = "<html><head><meta charset='utf-8'></head><body style='font-family:Segoe UI,sans-serif;padding:24px;background:#1e1e1e;color:#ddd'>"
                    + System.Net.WebUtility.HtmlEncode(message) + "</body></html>";
                if (webView.CoreWebView2 != null)
                    webView.CoreWebView2.NavigateToString(html);
            }
            catch { }
        }

        private static string ToHtml(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static string BuildThemeStyle()
        {
            string bg, panel, text, muted, accent, border;
            try
            {
                bg = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                panel = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTabGradientBeginColorKey));
                text = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                muted = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarTextInactiveColorKey));
                accent = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.AccentBorderColorKey));
                border = ToHtml(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBorderColorKey));
            }
            catch
            {
                bg = "#1e1e1e"; panel = "#252526"; text = "#dddddd";
                muted = "#888888"; accent = "#0078d4"; border = "#3a3a3a";
            }

            var sb = new StringBuilder();
            sb.Append("<style id=\"stats-theme\">:root{");
            sb.Append("--vs-bg:").Append(bg).Append(';');
            sb.Append("--vs-panel:").Append(panel).Append(';');
            sb.Append("--vs-text:").Append(text).Append(';');
            sb.Append("--vs-muted:").Append(muted).Append(';');
            sb.Append("--vs-accent:").Append(accent).Append(';');
            sb.Append("--vs-border:").Append(border).Append(';');
            sb.Append("--c1:#4fc3f7;--c2:#4ec9b0;--c3:#cca700;--c4:#c586c0;--c5:#f14c4c;");
            sb.Append("color-scheme:dark;");
            sb.Append("}</style>");
            return sb.ToString();
        }
    }
}
