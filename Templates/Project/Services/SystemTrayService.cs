using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Serilog;

namespace DevTemWinUi3.Services;

public sealed class SystemTrayService : IDisposable
{
    private const uint WM_APP = 0x8000;
    private const uint WM_TRAYICON = WM_APP + 1;

    private const uint NIF_MESSAGE = 0x01;
    private const uint NIF_ICON = 0x02;
    private const uint NIF_TIP = 0x04;
    private const uint NIF_SHOWTIP = 0x10;

    private const uint NIM_ADD = 0x00;
    private const uint NIM_MODIFY = 0x01;
    private const uint NIM_DELETE = 0x02;
    private const uint NIM_SETVERSION = 0x04;
    private const uint NOTIFYICON_VERSION_4 = 4;

    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const uint WM_RBUTTONUP = 0x0205;

    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_NONOTIFY = 0x0080;
    private const uint TPM_RETURNCMD = 0x0100;

    private const int ID_TRAY_SHOW = 1001;
    private const int ID_TRAY_CHECK_UPDATES = 1002;
    private const int ID_TRAY_SETTINGS = 1003;
    private const int ID_TRAY_EXIT = 1004;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x0010;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;

    #region P/Invoke

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImageW(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(
        IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

    [DllImport("user32.dll")]
    private static extern uint RegisterWindowMessageW([MarshalAs(UnmanagedType.LPWStr)] string lpString);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowW(
        [MarshalAs(UnmanagedType.LPWStr)] string? lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)] string? lpWindowName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWLP_WNDPROC = -4;
    private const int GWL_STYLE = -16;
    private const int HWND_TOPMOST = -1;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    #endregion

    private IntPtr _windowHandle;
    private IntPtr _iconHandle;
    private NOTIFYICONDATAW _notifyIconData;
    private WndProcDelegate? _wndProcDelegate;
    private GCHandle _wndProcHandle;
    private uint _taskbarCreatedMsg;
    private bool _disposed;

    private readonly SettingsService _settings = SettingsService.Current;
    private Window? _mainWindow;
    private IntPtr _mainWindowHwnd;
    private bool _isVisible;
    private bool _iconVisible;

    public static SystemTrayService Current { get; } = new();

    private SystemTrayService() { }

    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;

        try
        {
            _mainWindowHwnd = WindowNative.GetWindowHandle(mainWindow);

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
            Log.Information("System tray initialized");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize system tray");
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
            Log.Information("Window hidden to system tray");
            // Tell the user where the app went; clicking the toast reopens it.
            // Falls back silently when OS toasts are unavailable.
            DesktopToastService.Current.TryShowMinimized();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to hide window to tray");
        }
    }

    public void ShowFromTray()
    {
        if (_mainWindow is null)
            return;

        try
        {
            _mainWindow.AppWindow.Show();
            _mainWindow.Activate();

            SetWindowPos(_mainWindowHwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetWindowPos(_mainWindowHwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            Log.Information("Window restored from tray");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to show window from tray");
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
    /// Removes the tray icon and tears down the message window. Call on real
    /// app exit — otherwise Windows keeps a ghost icon after the process dies.
    /// Idempotent.
    /// </summary>
    public void Shutdown()
    {
        Dispose();
    }

    public static void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? string.Empty;
                key.SetValue(AppMetadata.AutoStartRegistryName, $"\"{exePath}\"");
                Log.Information("Auto-start enabled");
            }
            else
            {
                key.DeleteValue(AppMetadata.AutoStartRegistryName, false);
                Log.Information("Auto-start disabled");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to set auto-start");
        }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppMetadata.AutoStartRegistryName) is not null;
        }
        catch
        {
            return false;
        }
    }

    private void CreateMessageWindow()
    {
        if (_windowHandle != IntPtr.Zero)
            return;

        var hInstance = GetModuleHandleW(null);
        var className = $"DevTemWinUi3Tray_{Guid.NewGuid():N}";

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

    private void CreateTrayIcon()
    {
        if (_windowHandle == IntPtr.Zero || _iconVisible)
            return;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");

        if (!File.Exists(iconPath))
        {
            Log.Warning("Tray icon not found at {Path}", iconPath);
            return;
        }

        _iconHandle = LoadImageW(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
        if (_iconHandle == IntPtr.Zero)
        {
            Log.Warning("Failed to load tray icon");
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
                Log.Information("Taskbar recreated, tray icon restored");
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

    public event EventHandler<TrayNavigationRequest>? NavigationRequested;

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
            Log.Error(ex, "Error exiting app from tray");
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
