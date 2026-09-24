using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace OpenCodeStudio.Services
{
    /// <summary>
    /// Настройки раздела «OpenCode Studio» в Tools → Options.
    /// Служит точкой расширения для будущих инъекций в веб-UI.
    /// </summary>
    public class SpendSettings : DialogPage
    {
        [Category("Usage & Limits")]
        [DisplayName("Enable usage strip")]
        [Description("Inject a usage/limits strip into the OpenCode web UI status bar.")]
        public bool EnableInjection { get; set; } = true;

        [Category("Usage & Limits")]
        [DisplayName("Show limits percentages")]
        [Description("Show remaining day/week/month limits when an OpenCode Go subscription is present.")]
        public bool ShowLimits { get; set; } = true;

        [Category("Usage & Limits")]
        [DisplayName("Limits poll interval (seconds)")]
        public int LimitsPollSeconds { get; set; } = 5;

        [Category("Usage & Limits")]
        [DisplayName("History refresh interval (seconds)")]
        public int HistoryPollSeconds { get; set; } = 60;

        [Category("Server")]
        [DisplayName("Use OpenCode v2.x (preview)")]
        [Description("Prefer the OpenCode 2 beta binary (opencode2) when both v1 and v2 are installed.")]
        public bool UseOpenCodeV2 { get; set; } = false;

        public static event Action Applied;

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            try { Applied?.Invoke(); } catch { }
        }
    }
}
