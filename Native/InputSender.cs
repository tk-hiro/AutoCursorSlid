using System.Runtime.InteropServices;
using DishAuto.Core;
using static DishAuto.Native.NativeMethods;

namespace DishAuto.Native;

/// <summary>マウス・キーボード入力の送信。</summary>
public sealed class InputSender
{
    // 押しっぱなしのまま終わらないよう、押下状態はプロセス全体で管理する
    private static volatile bool s_leftDown;
    private static volatile bool s_spaceDown;
    private static nint s_messageTarget; // ウィンドウメッセージ方式で押下中の送り先

    private readonly MoveMode _mode;
    private readonly ClickMethod _click;
    private nint _target;
    private PointD _position;

    public InputSender(MoveMode mode, ClickMethod click)
    {
        _mode = mode;
        _click = click;
    }

    /// <summary>ウィンドウメッセージ方式の送り先を、指定した点の下にあるウィンドウに決める。</summary>
    public void AttachToWindowAt(PointD p) =>
        _target = WindowFromPoint(new POINT { X = (int)Math.Round(p.X), Y = (int)Math.Round(p.Y) });

    public void MoveTo(PointD p)
    {
        _position = p;
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

        if (_click == ClickMethod.WindowMessage && _target != 0)
            PostMouse(_target, WM_MOUSEMOVE, s_leftDown ? MK_LBUTTON : 0, p);
    }

    public void LeftDown()
    {
        if (_click == ClickMethod.WindowMessage && _target != 0)
        {
            s_messageTarget = _target;
            PostMouse(_target, WM_LBUTTONDOWN, MK_LBUTTON, _position);
        }
        else
        {
            SendMouse(0, 0, MOUSEEVENTF_LEFTDOWN);
        }
        s_leftDown = true;
    }

    public void LeftUp()
    {
        if (_click == ClickMethod.WindowMessage && _target != 0)
            PostMouse(_target, WM_LBUTTONUP, 0, _position);
        else
            SendMouse(0, 0, MOUSEEVENTF_LEFTUP);
        s_leftDown = false;
        s_messageTarget = 0;
    }

    /// <summary>ウィンドウを前面にするためのクリック（押してすぐ離す）。</summary>
    public static void Click()
    {
        SendMouse(0, 0, MOUSEEVENTF_LEFTDOWN);
        SendMouse(0, 0, MOUSEEVENTF_LEFTUP);
    }

    public static PointD GetCursor()
    {
        GetCursorPos(out var pt);
        return new PointD(pt.X, pt.Y);
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
        if (s_leftDown)
        {
            if (s_messageTarget != 0)
                PostMouse(s_messageTarget, WM_LBUTTONUP, 0, GetCursor());
            else
                SendMouse(0, 0, MOUSEEVENTF_LEFTUP);
            s_leftDown = false;
            s_messageTarget = 0;
        }
    }

    public static bool IsEscPressed() => (GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0;

    private static void PostMouse(nint hwnd, int msg, int keys, PointD screen)
    {
        var pt = new POINT { X = (int)Math.Round(screen.X), Y = (int)Math.Round(screen.Y) };
        ScreenToClient(hwnd, ref pt);
        nint lParam = ((pt.Y & 0xFFFF) << 16) | (pt.X & 0xFFFF);
        PostMessage(hwnd, msg, keys, lParam);
    }

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
