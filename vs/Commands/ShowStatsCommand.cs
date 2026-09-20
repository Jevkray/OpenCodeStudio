using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace OpenCodeStudio.Commands
{
    internal sealed class ShowStatsCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("D91F3A72-6C40-4E8B-B25D-8A47F0C63E15");

        private readonly OpenCodeStudioPackage _package;

        private ShowStatsCommand(OpenCodeStudioPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            commandService.AddCommand(new MenuCommand(Execute, menuCommandId));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new ShowStatsCommand((OpenCodeStudioPackage)package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = _package.JoinableTaskFactory.RunAsync(async delegate
            {
                await _package.ShowStatsWindowAsync();
            });
        }
    }
}
