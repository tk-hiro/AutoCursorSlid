using System.Windows;
using System.Windows.Threading;
using DishAuto.Native;

namespace DishAuto;

public partial class App : Application
{
    public App()
    {
        // 数値の入力欄で「0.」のような途中の入力が「0」に戻され、小数を打てなくなるのを防ぐ
        FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;
    }

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
