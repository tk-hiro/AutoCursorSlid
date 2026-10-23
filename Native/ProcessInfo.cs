using System.Security.Principal;
using static DishAuto.Native.NativeMethods;

namespace DishAuto.Native;

/// <summary>管理者権限（昇格）の判定。昇格したウィンドウには、昇格していないアプリからクリックを送れない。</summary>
public static class ProcessInfo
{
    public static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>ウィンドウを持つプロセスが昇格しているか。判定できなければ null。</summary>
    public static bool? IsWindowElevated(nint hwnd)
    {
        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0) return null;

        nint process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == 0) return null;
        try
        {
            // 昇格したプロセスのトークンは、昇格していないプロセスからは開けない
            if (!OpenProcessToken(process, TOKEN_QUERY, out nint token)) return true;
            try
            {
                return GetTokenInformation(token, TokenElevation, out int elevated, sizeof(int), out _)
                    ? elevated != 0
                    : null;
            }
            finally
            {
                CloseHandle(token);
            }
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /// <summary>画面上の点にあるトップレベルウィンドウ。</summary>
    public static nint TopLevelWindowAt(int x, int y)
    {
        nint hwnd = WindowFromPoint(new POINT { X = x, Y = y });
        return hwnd == 0 ? 0 : GetAncestor(hwnd, GA_ROOT);
    }
}
