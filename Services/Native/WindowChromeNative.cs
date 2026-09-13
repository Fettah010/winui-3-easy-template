using System;
using System.Runtime.InteropServices;

namespace DevTemWinUi3.Services.Native;

/// <summary>
/// Win32 imports for window chrome: minimum-size subclassing and
/// bring-to-front activation. Pure declarations — behavior lives in
/// <see cref="Services.WindowChromeService"/> and
/// <see cref="Services.WindowActivator"/>.
/// </summary>
internal static class WindowChromeNative
{
    internal const uint WM_GETMINMAXINFO = 0x0024;

    internal static readonly IntPtr HWND_TOPMOST = new(-1);
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("comctl32.dll", SetLastError = true)]
    internal static extern bool SetWindowSubclass(
        IntPtr hWnd, IntPtr pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll")]
    internal static extern int DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    internal static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MinMaxPoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MinMaxInfo
    {
        public MinMaxPoint ptReserved;
        public MinMaxPoint ptMaxSize;
        public MinMaxPoint ptMaxPosition;
        public MinMaxPoint ptMinTrackSize;
        public MinMaxPoint ptMaxTrackSize;
    }

    internal delegate int SubclassProc(
        IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);
}
