using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;

namespace Client.Wpf
{
    [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
    public partial class App
    {
        [STAThread]
        [DebuggerNonUserCode]
        public static void Main() {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }

        public static void ChangeMainWindow(Window window)
        {
            Current.MainWindow.Close();
            Current.MainWindow = window;
            window.Show();
        }
    }
}
