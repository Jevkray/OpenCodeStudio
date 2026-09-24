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

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _serverController = new ServerController(() => GetSpendSettings()?.UseOpenCodeV2 ?? false);

            Services.SpendSettings.Applied += OnSpendSettingsApplied;

            await ShowOpenCodeWindowCommand.InitializeAsync(this);
            await ShowStatsCommand.InitializeAsync(this);
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

        internal async Task ShowStatsWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var window = new UsageStatsWindow(_serverController);
            window.Show();
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
                _serverController?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
