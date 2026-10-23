using System.Windows;
using System.Windows.Threading;
using DishAuto.Native;

namespace DishAuto;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += (_, _) => InputSender.EmergencyRelease();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        InputSender.EmergencyRelease();
        MessageBox.Show(e.Exception.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
