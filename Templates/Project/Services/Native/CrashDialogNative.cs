using System;
using System.Runtime.InteropServices;

namespace DevTemWinUi3.Services.Native;

/// <summary>
/// Win32 message box for the startup-crash path in <c>Program.Main</c>.
/// Pure declarations + one never-throw show helper: at crash time the UI
/// stack may be unusable, so this stays off WinUI entirely. OS-touching code
/// stays untested behind the never-throw guard (same precedent as the tray
/// service); the message text itself is built and tested in <c>Program</c>.
/// </summary>
internal static class CrashDialogNative
{
    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_TOPMOST = 0x00040000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    /// <summary>Best-effort fatal-error box. Never throws.</summary>
    internal static void Show(string title, string message)
    {
        try
        {
            _ = MessageBoxW(IntPtr.Zero, message ?? string.Empty, title ?? string.Empty,
                MB_OK | MB_ICONERROR | MB_TOPMOST);
        }
        catch { }
    }
}
