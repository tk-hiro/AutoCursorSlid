using System.Runtime.InteropServices;
using DishAuto.Core;
using static DishAuto.Native.NativeMethods;

namespace DishAuto.Native;

/// <summary>SendInput によるマウス・キーボード入力の送信。</summary>
public sealed class InputSender
{
    // 押しっぱなしのまま終わらないよう、押下状態はプロセス全体で管理する
    private static volatile bool s_leftDown;
    private static volatile bool s_spaceDown;

    private readonly MoveMode _mode;

    public InputSender(MoveMode mode) => _mode = mode;

    public void MoveTo(PointD p)
    {
        if (_mode == MoveMode.Absolute)
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int width = Math.Max(2, GetSystemMetrics(SM_CXVIRTUALSCREEN));
            int height = Math.Max(2, GetSystemMetrics(SM_CYVIRTUALSCREEN));

            int nx = (int)Math.Round((p.X - left) * 65535.0 / (width - 1));
            int ny = (int)Math.Round((p.Y - top) * 65535.0 / (height - 1));
            SendMouse(nx, ny, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK | MOUSEEVENTF_MOVE_NOCOALESCE);
        }
        else
        {
            // 相対移動: 現在位置との差分を送る（マウス加速で誤差が出ても次のステップで補正される）
            var cur = GetCursor();
            int dx = (int)Math.Round(p.X - cur.X);
            int dy = (int)Math.Round(p.Y - cur.Y);
            if (dx != 0 || dy != 0)
                SendMouse(dx, dy, MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE);
        }
    }

    public static PointD GetCursor()
    {
        GetCursorPos(out var pt);
        return new PointD(pt.X, pt.Y);
    }

    public static void LeftDown()
    {
        SendMouse(0, 0, MOUSEEVENTF_LEFTDOWN);
        s_leftDown = true;
    }

    public static void LeftUp()
    {
        SendMouse(0, 0, MOUSEEVENTF_LEFTUP);
        s_leftDown = false;
    }

    public static void SpaceDown()
    {
        SendKey(SCANCODE_SPACE, KEYEVENTF_SCANCODE);
        s_spaceDown = true;
    }

    public static void SpaceUp()
    {
        SendKey(SCANCODE_SPACE, KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP);
        s_spaceDown = false;
    }

    /// <summary>押しっぱなしになっている入力をすべて離す。</summary>
    public static void EmergencyRelease()
    {
        if (s_spaceDown) SpaceUp();
        if (s_leftDown) LeftUp();
    }

    public static bool IsEscPressed() => (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;

    private static void SendMouse(int dx, int dy, uint flags)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion { mi = new MOUSEINPUT { dx = dx, dy = dy, dwFlags = flags } },
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static void SendKey(ushort scanCode, uint flags)
    {
        var input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion { ki = new KEYBDINPUT { wScan = scanCode, dwFlags = flags } },
        };
        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }
}
