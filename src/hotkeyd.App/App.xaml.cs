using System.Windows;
using Hotkeyd.Core;

namespace Hotkeyd.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var store = ConfigStore.Default();
        Config config;
        try
        {
            config = store.Load();
        }
        catch (Exception error)
        {
            // A corrupt config must not stop the app from opening -- the user
            // needs the window precisely so they can fix the bindings. Start
            // empty and tell them, rather than refusing to launch.
            MessageBox.Show(
                $"Ayar dosyası okunamadı, boş başlatıldı:\n{error.Message}\n\n{store.Path}",
                "hotkeyd", MessageBoxButton.OK, MessageBoxImage.Warning);
            config = new Config();
        }

        var startMinimised = e.Args.Contains("--tray");
        var window = new MainWindow(store, config);
        if (startMinimised) window.WindowState = WindowState.Minimized;
        window.Show();
    }
}
