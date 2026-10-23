using System.ComponentModel;
using System.Windows;
using DishAuto.Core;
using DishAuto.Native;
using DishAuto.Settings;
using DishAuto.ViewModels;
using DishAuto.Views;

namespace DishAuto;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private HotkeyManager? _hotkeys;
    private CancellationTokenSource? _cts;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel(SettingsStore.Load());
        DataContext = _vm;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hotkeys = new HotkeyManager(this);

        var failed = new List<string>();
        if (!_hotkeys.Register(0, NativeMethods.VK_F9, () => _ = StartAsync(fromHotkey: true))) failed.Add("F9");
        if (!_hotkeys.Register(0, NativeMethods.VK_F10, Stop)) failed.Add("F10");
        if (failed.Count > 0)
            _vm.Status = $"{string.Join(", ", failed)} のホットキー登録に失敗しました（他のアプリが使用中）。ボタンで操作してください。";
    }

    private void SelectRegion_Click(object sender, RoutedEventArgs e)
    {
        var current = _vm.Settings.ToRegion();
        var overlay = new RegionSelectWindow(_vm.Settings.Shape, current.IsValid ? current : null);

        Hide();
        try
        {
            if (overlay.ShowDialog() == true && overlay.Result is { } region)
            {
                _vm.Settings.SetRegion(region);
                SettingsStore.Save(_vm.Settings);
                _vm.Status = "範囲を設定しました。F9 で開始します。";
            }
        }
        finally
        {
            Show();
            Activate();
        }
    }

    private void Start_Click(object sender, RoutedEventArgs e) => _ = StartAsync(fromHotkey: false);

    private void Stop_Click(object sender, RoutedEventArgs e) => Stop();

    private async Task StartAsync(bool fromHotkey)
    {
        if (_vm.IsRunning) return;

        // F9 はゲームが前面のまま押せるので、待たずにすぐ始める
        var config = _vm.Settings.ToRunConfig();
        if (fromHotkey) config = config with { CountdownSec = 0 };
        if (!config.Region.IsValid)
        {
            _vm.Status = "先に「範囲を選択…」で範囲を指定してください。";
            return;
        }
        SettingsStore.Save(_vm.Settings);

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _vm.IsRunning = true;
        bool minimized = _vm.Settings.MinimizeWhileRunning;
        if (minimized) WindowState = WindowState.Minimized;

        // 終了後に遅れて届いた進捗で最終メッセージが上書きされないようにする
        var progress = new Progress<string>(s => { if (_vm.IsRunning) _vm.Status = s; });
        string finalStatus;
        try
        {
            var reason = await Task.Factory.StartNew(
                () => new AutomationRunner(config).Run(token, progress),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            finalStatus = reason.ToJapanese();
        }
        catch (Exception ex)
        {
            finalStatus = "エラーで停止しました: " + ex.Message;
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _vm.IsRunning = false;
        }

        _vm.Status = finalStatus;
        if (minimized) WindowState = WindowState.Normal;
    }

    private void Stop() => _cts?.Cancel();

    protected override void OnClosing(CancelEventArgs e)
    {
        // バックグラウンドスレッドはプロセス終了で強制終了されるため、ここで必ず離す
        _cts?.Cancel();
        InputSender.EmergencyRelease();
        SettingsStore.Save(_vm.Settings);
        _hotkeys?.Dispose();
        base.OnClosing(e);
    }
}
