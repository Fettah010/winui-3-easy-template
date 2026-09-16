using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Services.Native;

namespace DevTemWinUi3.Services;

/// <summary>
/// Window dressing: Mica backdrop, custom title bar, theme-aware title-bar
/// colors and window icon, native minimum size, entrance animation. Owned
/// here so <c>MainWindow</c> stays composition (routes, state, wiring).
/// Process-lifetime singleton: it pins the subclass delegate for the
/// window's life.
/// </summary>
public sealed class WindowChromeService
{
    public static WindowChromeService Current { get; } = new();

    // Held in a field so the GC never collects the native callback.
    private WindowChromeNative.SubclassProc? _subclassProc;

    private WindowChromeService() { }

    /// <summary>
    /// Applies the full chrome to a fresh window: Mica, extended title bar,
    /// icon, native min size, and title-bar colors for the current theme.
    /// </summary>
    public void Initialize(Window window, UIElement titleBar)
    {
        try
        {
            window.SystemBackdrop = new MicaBackdrop();
            window.ExtendsContentIntoTitleBar = true;
            window.SetTitleBar(titleBar);
            window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }
        catch { }

        RefreshWindowIcon(window);
        RefreshTitleBarColors(window);
        EnforceMinWindowSize(window);
    }

    public void RefreshTitleBarColors(Window window) =>
        SetTitleBarColors(window, (window.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark);

    /// <summary>Refreshes colors + icon together when the theme changes.</summary>
    public void RefreshForThemeChange(Window window, bool isDark)
    {
        SetTitleBarColors(window, isDark);
        RefreshWindowIcon(window, isDark);
    }

    public void RefreshWindowIcon(Window window, bool? isDark = null)
    {
        try
        {
            // Taskbar/titlebar icon matches the app/installer icon (theme
            // variant when present, app.ico fallback). Guard File.Exists:
            // SetIcon with a missing file can blank the taskbar icon
            // instead of throwing.
            bool dark = isDark ?? (window.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;
            var iconPath = AppIconService.ResolveIconPath(AppContext.BaseDirectory, dark);
            if (System.IO.File.Exists(iconPath))
                window.AppWindow.SetIcon(iconPath);
        }
        catch { }
    }

    /// <summary>
    /// Fades in window content after the splash screen transition.
    /// Called by App after splash closes (fire-and-forget: time-to-
    /// interactive does not wait for it). Completes on the animation
    /// itself with a bounded fallback — never a fixed sleep.
    /// </summary>
    public async Task PlayEntranceAnimation(FrameworkElement root)
    {
        try
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(LaunchAnimations.Scale(TimeSpan.FromMilliseconds(350))),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeIn, root);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            var story = new Storyboard();
            story.Children.Add(fadeIn);

            await LaunchAnimations.AwaitStoryboardAsync(
                story, TimeSpan.FromMilliseconds(500));
        }
        catch { }
    }

    private static void SetTitleBarColors(Window window, bool isDark)
    {
        try
        {
            var titleBar = window.AppWindow.TitleBar;

            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonForegroundColor = isDark
                ? Microsoft.UI.Colors.White
                : Microsoft.UI.Colors.Black;

            titleBar.ButtonHoverBackgroundColor = isDark
                ? Microsoft.UI.ColorHelper.FromArgb(40, 255, 255, 255)
                : Microsoft.UI.ColorHelper.FromArgb(30, 0, 0, 0);
            titleBar.ButtonHoverForegroundColor = isDark
                ? Microsoft.UI.Colors.White
                : Microsoft.UI.Colors.Black;

            titleBar.ButtonPressedBackgroundColor = isDark
                ? Microsoft.UI.ColorHelper.FromArgb(60, 255, 255, 255)
                : Microsoft.UI.ColorHelper.FromArgb(50, 0, 0, 0);
            titleBar.ButtonPressedForegroundColor = isDark
                ? Microsoft.UI.Colors.White
                : Microsoft.UI.Colors.Black;

            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveForegroundColor = isDark
                ? Microsoft.UI.ColorHelper.FromArgb(120, 255, 255, 255)
                : Microsoft.UI.ColorHelper.FromArgb(120, 0, 0, 0);
        }
        catch { }
    }

    /// <summary>
    /// Enforces the minimum window size natively via <c>WM_GETMINMAXINFO</c>,
    /// so the OS itself blocks shrinking below the limit (no flicker, works
    /// with snap/Aero and per-monitor DPI). Limits come from
    /// <see cref="ResponsiveLayout"/> and are scaled to physical pixels.
    /// </summary>
    private void EnforceMinWindowSize(Window window)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (hwnd == IntPtr.Zero)
                return;
            _subclassProc = MainWindowSubclassProc;
            WindowChromeNative.SetWindowSubclass(
                hwnd, Marshal.GetFunctionPointerForDelegate(_subclassProc), IntPtr.Zero, IntPtr.Zero);
        }
        catch { }
    }

    private static int MainWindowSubclassProc(
        IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WindowChromeNative.WM_GETMINMAXINFO && lParam != IntPtr.Zero)
        {
            try
            {
                var dpi = WindowChromeNative.GetDpiForWindow(hWnd);
                if (dpi == 0)
                    dpi = 96;
                var info = Marshal.PtrToStructure<WindowChromeNative.MinMaxInfo>(lParam);
                info.ptMinTrackSize.X = ResponsiveLayout.ScaleLogicalToPhysical(ResponsiveLayout.MinWindowWidth, dpi);
                info.ptMinTrackSize.Y = ResponsiveLayout.ScaleLogicalToPhysical(ResponsiveLayout.MinWindowHeight, dpi);
                Marshal.StructureToPtr(info, lParam, false);
                return 0;
            }
            catch { }
        }
        return WindowChromeNative.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
}
