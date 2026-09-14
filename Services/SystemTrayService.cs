using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using static DevTemWinUi3.Services.Native.TrayNative;

namespace DevTemWinUi3.Services;

/// <summary>
/// Minimize-to-tray: notify icon, message-only window, context menu.
/// Win32 declarations live in <c>Native/TrayNative.cs</c>; auto-start lives
/// in <see cref="AutoStartService"/>; showing the window lives in
/// <see cref="WindowActivator"/>.
/// </summary>
public sealed class SystemTrayService : IDisposable
{
    private const int ID_TRAY_SHOW = 1001;
    private const int ID_TRAY_CHECK_UPDATES = 1002;
    private const int ID_TRAY_SETTINGS = 1003;
    private const int ID_TRAY_EXIT = 1004;

    private IntPtr _windowHandle;
    private IntPtr _iconHandle;
    private NOTIFYICONDATAW _notifyIconData;
    private WndProcDelegate? _wndProcDelegate;
    private GCHandle _wndProcHandle;
    private uint _taskbarCreatedMsg;
    private bool _disposed;

    private readonly SettingsService _settings = SettingsService.Current;
    private Window? _mainWindow;
    private bool _isVisible;
    private bool _iconVisible;
    private bool _isDarkIcon;

    public static SystemTrayService Current { get; } = new();

    /// <summary>
    /// Raised when a tray menu item targets a page (check-updates, settings).
    /// MainWindow shows itself and navigates.
    /// </summary>
    public event EventHandler<TrayNavigationRequest>? NavigationRequested;

    private SystemTrayService() { }

    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;

        try
        {
            _taskbarCreatedMsg = RegisterWindowMessageW("TaskbarCreated");

            _wndProcDelegate = TrayWndProc;
            // Pin the delegate for the process lifetime; freed in Dispose.
            // (Previously allocated and never freed: a permanent handle leak.)
            if (_wndProcHandle.IsAllocated)
                _wndProcHandle.Free();
            _wndProcHandle = GCHandle.Alloc(_wndProcDelegate);

            CreateMessageWindow();

            // Only show the icon when the user opted in. Creating it
            // unconditionally is why the app "went to tray" with the
            // toggle off.
            if (_settings.MinimizeToTray)
            {
                CreateTrayIcon();
                _iconVisible = true;
            }

            _isVisible = true;
            AppLog.Information("System tray initialized");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to initialize system tray");
        }
    }

    public bool HandleWindowClose()
    {
        if (_settings.MinimizeToTray && _isVisible)
        {
            HideToTray();
            return true;
        }
        return false;
    }

    public void HideToTray()
    {
        if (_mainWindow is null || !_isVisible)
            return;

        try
        {
            // The icon may be absent (toggle off at launch): hiding without
            // it would strand the window with no way back. Ensure it first.
            EnsureTrayIcon();
            _mainWindow.AppWindow.Hide();
            AppLog.Information("Window hidden to system tray");
            // Tell the user where the app went; clicking the toast reopens it.
            // Falls back silently when OS toasts are unavailable.
            DesktopToastService.Current.TryShowMinimized();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to hide window to tray");
        }
    }

    public void ShowFromTray()
    {
        if (_mainWindow is null)
            return;

        try
        {
            WindowActivator.ShowAndActivate(_mainWindow);
            AppLog.Information("Window restored from tray");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to show window from tray");
        }
    }

    public void UpdateSettings()
    {
        if (!_settings.MinimizeToTray && _iconVisible)
        {
            RemoveTrayIcon();
            _iconVisible = false;
        }
        else if (_settings.MinimizeToTray && !_iconVisible)
        {
            EnsureTrayIcon();
        }
    }

    /// <summary>
    /// Swaps the tray icon to the light/dark variant. No-op when the icon is
    /// currently hidden (the correct variant is picked on next <see cref="EnsureTrayIcon"/>).
    /// Falls back to <c>app.ico</c> when variants are absent.
    /// </summary>
    public void RefreshThemeIcon(bool isDark)
    {
        try
        {
            _isDarkIcon = isDark;
            if (!_iconVisible)
                return;
            var iconPath = AppIconService.ResolveIconPath(AppContext.BaseDirectory, isDark);
            RemoveTrayIcon();
            CreateTrayIcon(iconPath);
            _iconVisible = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Failed to refresh tray theme icon");
        }
    }

    /// <summary>
    /// Removes the tray icon and tears down the message window. Call on real
    /// app exit — otherwise Windows keeps a ghost icon after the process dies.
    /// Idempotent.
    /// </summary>
    public void Shutdown()
    {
        Dispose();
    }

    private void CreateMessageWindow()
    {
        if (_windowHandle != IntPtr.Zero)
            return;

        var hInstance = GetModuleHandleW(null);
        var className = $"DevTemTray_{Guid.NewGuid():N}";

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate!),
            hInstance = hInstance,
            lpszClassName = className,
        };

        if (RegisterClassExW(ref wc) == 0)
            throw new InvalidOperationException("Failed to register tray window class");

        _windowHandle = CreateWindowExW(
            WS_EX_TOOLWINDOW, className, "", WS_POPUP,
            0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_windowHandle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create tray message window");
    }

    private void DestroyMessageWindow()
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        DestroyWindow(_windowHandle);
        _windowHandle = IntPtr.Zero;
    }

    private void CreateTrayIcon(string? iconPath = null)
    {
        if (_windowHandle == IntPtr.Zero || _iconVisible)
            return;

        iconPath ??= AppIconService.ResolveIconPath(AppContext.BaseDirectory, _isDarkIcon);

        if (!File.Exists(iconPath))
        {
            AppLog.Warning("Tray icon not found at {Path}", iconPath);
            return;
        }

        _iconHandle = LoadImageW(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
        if (_iconHandle == IntPtr.Zero)
        {
            AppLog.Warning("Failed to load tray icon");
            return;
        }

        _notifyIconData = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _windowHandle,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _iconHandle,
            szTip = AppMetadata.TrayTooltip,
            uTimeoutOrVersion = NOTIFYICON_VERSION_4,
        };

        Shell_NotifyIconW(NIM_ADD, ref _notifyIconData);
        Shell_NotifyIconW(NIM_SETVERSION, ref _notifyIconData);
        _iconVisible = true;
    }

    /// <summary>Creates the icon if it is currently absent (no-op otherwise).</summary>
    private void EnsureTrayIcon()
    {
        if (!_iconVisible)
            CreateTrayIcon();
    }

    private void RemoveTrayIcon()
    {
        if (_notifyIconData.cbSize > 0)
        {
            Shell_NotifyIconW(NIM_DELETE, ref _notifyIconData);
            _notifyIconData = new NOTIFYICONDATAW();
        }

        if (_iconHandle != IntPtr.Zero)
        {
            DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }

        _iconVisible = false;
    }

    private IntPtr TrayWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == _taskbarCreatedMsg && _taskbarCreatedMsg != 0)
        {
            // Explorer restarted: restore the icon only if opted in.
            if (_settings.MinimizeToTray)
            {
                RemoveTrayIcon();
                CreateTrayIcon();
                AppLog.Information("Taskbar recreated, tray icon restored");
            }
            return IntPtr.Zero;
        }

        if (msg == WM_TRAYICON)
        {
            HandleTrayIconMessage(lParam);
            return IntPtr.Zero;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void HandleTrayIconMessage(IntPtr lParam)
    {
        var mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);

        switch (mouseMsg)
        {
            case WM_LBUTTONUP:
            case WM_LBUTTONDBLCLK:
                ShowFromTray();
                break;

            case WM_RBUTTONUP:
                ShowContextMenu();
                break;
        }
    }

    private void ShowContextMenu()
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        GetCursorPos(out var point);

        var hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        var loc = LocalizationService.Current;
        AppendMenuW(hMenu, 0x0000, (IntPtr)ID_TRAY_SHOW, loc.GetString("TrayShow"));
        AppendMenuW(hMenu, 0x0000, IntPtr.Zero, null);
        AppendMenuW(hMenu, 0x0000, (IntPtr)ID_TRAY_CHECK_UPDATES, loc.GetString("TrayCheckUpdates"));
        AppendMenuW(hMenu, 0x0000, (IntPtr)ID_TRAY_SETTINGS, loc.GetString("TraySettings"));
        AppendMenuW(hMenu, 0x0000, IntPtr.Zero, null);
        AppendMenuW(hMenu, 0x0000, (IntPtr)ID_TRAY_EXIT, loc.GetString("TrayExit"));

        SetForegroundWindow(_windowHandle);

        var cmd = TrackPopupMenu(hMenu, TPM_RIGHTBUTTON | TPM_NONOTIFY | TPM_RETURNCMD,
            point.X, point.Y, 0, _windowHandle, IntPtr.Zero);

        DestroyMenu(hMenu);

        switch (cmd)
        {
            case ID_TRAY_SHOW:
                ShowFromTray();
                break;
            case ID_TRAY_CHECK_UPDATES:
                ShowFromTray();
                _mainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    NavigationRequested?.Invoke(this, new TrayNavigationRequest("settings", AutoCheckUpdates: true));
                });
                break;
            case ID_TRAY_SETTINGS:
                ShowFromTray();
                _mainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    NavigationRequested?.Invoke(this, new TrayNavigationRequest("settings", AutoCheckUpdates: false));
                });
                break;
            case ID_TRAY_EXIT:
                ExitApp();
                break;
        }
    }

    private void ExitApp()
    {
        try
        {
            RemoveTrayIcon();
            DestroyMessageWindow();
            _isVisible = false;

            _mainWindow?.Close();
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Error exiting app from tray");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RemoveTrayIcon();
        DestroyMessageWindow();

        if (_wndProcHandle.IsAllocated)
            _wndProcHandle.Free();
        _wndProcDelegate = null;
    }
}
