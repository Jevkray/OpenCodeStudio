using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace OpenCodeStudio
{
    [Guid("E3F4A5B6-C7D8-9012-EFAB-CDEF34567890")]
    public sealed class OpenCodeToolWindow : ToolWindowPane
    {
        public OpenCodeToolWindowControl Control { get; private set; }

        public OpenCodeToolWindow() : base(null)
        {
            Caption = "OpenCode Studio";
            Control = new OpenCodeToolWindowControl();
            Content = Control;
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            Control?.SetServiceProvider(serviceProvider);
        }

        protected override void OnClose()
        {
            Control?.OnWindowClosing();
            base.OnClose();
        }
    }
}
