using System.Windows;

namespace Client.Wpf
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main() {
            Client.Wpf.App app = new Client.Wpf.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
