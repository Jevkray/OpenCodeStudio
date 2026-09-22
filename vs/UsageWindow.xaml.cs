using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using OpenCodeStudio.Services;

namespace OpenCodeStudio
{
    /// <summary>
    /// Окно статистики использования в WebView2: лимиты подписки,
    /// расход по моделям и по дням месяца.
    /// </summary>
    public partial class UsageWindow : Window
    {
        private readonly OpenCodeToolWindowControl _source;
        private readonly bool _showLimits;

        public UsageWindow(OpenCodeToolWindowControl source, bool showLimits)
        {
            InitializeComponent();
            _source = source;
            _showLimits = showLimits;
            Loaded += OnLoaded;
        }

        private static string LoadPage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("OpenCodeStudio.Resources.UsagePage.html"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OpenCodeStudio", "WebView2-usage");
                Directory.CreateDirectory(userDataFolder);

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);

                if (_source != null)
                    _source.SnapshotUpdated += OnSnapshot;

                webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                webView.CoreWebView2.NavigateToString(LoadPage());
            }
            catch (Exception ex)
            {
                Services.Log.Error("Usage window init failed", ex);
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_source?.LatestSnapshot != null)
                Push(_source.LatestSnapshot);
        }

        private void OnSnapshot(SpendSnapshot s)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Push(s);
            });
        }

        private void Push(SpendSnapshot s)
        {
            if (webView.CoreWebView2 == null) return;
            try
            {
                var payload = new
                {
                    generatedAt = s.GeneratedAt.ToUnixTimeMilliseconds(),
                    todayCost = s.TodayCost,
                    showLimits = _showLimits,
                    limits = new
                    {
                        hasGo = s.Limits?.HasGo ?? false,
                        fivePct = s.Limits?.FivePct ?? 0, weekPct = s.Limits?.WeekPct ?? 0, monthPct = s.Limits?.MonthPct ?? 0,
                        fiveUsed = s.Limits?.FiveUsed ?? 0, fiveLimit = s.Limits?.FiveLimit ?? 0, fiveResetSec = s.Limits?.FiveResetSec ?? 0,
                        weekUsed = s.Limits?.WeekUsed ?? 0, weekLimit = s.Limits?.WeekLimit ?? 0, weekResetSec = s.Limits?.WeekResetSec ?? 0,
                        monthUsed = s.Limits?.MonthUsed ?? 0, monthLimit = s.Limits?.MonthLimit ?? 0, monthResetSec = s.Limits?.MonthResetSec ?? 0
                    },
                    rows = (s.Rows ?? new List<SpendRow>()).Select(r => new
                    {
                        model = r.Model,
                        provider = r.Provider,
                        createdAt = r.CreatedAt,
                        cost = r.Cost
                    })
                };
                webView.CoreWebView2.PostWebMessageAsJson(Newtonsoft.Json.JsonConvert.SerializeObject(payload));
            }
            catch (Exception ex)
            {
                Services.Log.Error("Usage window push failed", ex);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_source != null)
                _source.SnapshotUpdated -= OnSnapshot;
            base.OnClosed(e);
        }
    }
}
