using System;
using Microsoft.UI.Xaml;
using DevTemWinUi3.Services.Native;

namespace DevTemWinUi3.Services;

/// <summary>
/// The single place that shows a hidden window and pulls it to the front
/// (tray restore, toast click, second-instance activation). The topmost
/// toggle used to be copy-pasted in three spots — now it lives here.
/// </summary>
public static class WindowActivator
{
    public static void ShowAndActivate(Window window)
    {
        try
        {
            window.AppWindow.Show();
            window.Activate();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WindowChromeNative.SetWindowPos(hwnd, WindowChromeNative.HWND_TOPMOST,
                0, 0, 0, 0,
                WindowChromeNative.SWP_NOMOVE | WindowChromeNative.SWP_NOSIZE | WindowChromeNative.SWP_SHOWWINDOW);
            WindowChromeNative.SetWindowPos(hwnd, IntPtr.Zero,
                0, 0, 0, 0,
                WindowChromeNative.SWP_NOMOVE | WindowChromeNative.SWP_NOSIZE | WindowChromeNative.SWP_SHOWWINDOW);
        }
        catch { }
    }
}
