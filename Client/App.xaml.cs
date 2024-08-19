using System.Windows;

namespace Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize logging
        Logging.Instance.Load();
        
        // Initialize config
        Config.Instance.Load();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logging.Instance.Dispose();
        
        Config.Instance.Dispose();
        
        base.OnExit(e);
    }
}