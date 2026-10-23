using System.Windows;
using System.Windows.Interop;
using static DishAuto.Native.NativeMethods;

namespace DishAuto.Native;

/// <summary>RegisterHotKey によるグローバルホットキー。ゲームが前面でも反応する。</summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = [];
    private int _nextId = 1;

    public HotkeyManager(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(hwnd);
        _source.AddHook(WndProc);
    }

    public bool Register(uint modifiers, uint vk, Action action)
    {
        int id = _nextId++;
        if (!RegisterHotKey(_source.Handle, id, modifiers | MOD_NOREPEAT, vk))
            return false;
        _actions[id] = action;
        return true;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _actions.TryGetValue((int)wParam, out var action))
        {
            action();
            handled = true;
        }
        return 0;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys)
            UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
        _source.RemoveHook(WndProc);
    }
}
