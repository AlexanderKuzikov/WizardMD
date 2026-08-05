using System.Windows;

namespace WizardMD.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string? file = e.Args.Length > 0 ? e.Args[0] : null;
        new MainWindow(file).Show();
    }
}
