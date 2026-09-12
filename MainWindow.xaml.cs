using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DevTemWinUi3.Pages;
using DevTemWinUi3.Services;

namespace DevTemWinUi3;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, IntPtr pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll")]
    private static extern int DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxPoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public MinMaxPoint ptReserved;
        public MinMaxPoint ptMaxSize;
        public MinMaxPoint ptMaxPosition;
        public MinMaxPoint ptMinTrackSize;
        public MinMaxPoint ptMaxTrackSize;
    }

    private delegate int SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

    // Held in a field so the GC never collects the native callback.
    private SubclassProc? _subclassProc;

    private readonly WindowStateService _windowState = WindowStateService.Current;
    private readonly NavigationService _nav = NavigationService.Current;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "DevTem-WinUI 3";
        this.SystemBackdrop = new MicaBackdrop();

        // Apply the persisted theme on launch (Settings only applies it on change).
        try
        {
            if (Content is FrameworkElement themedRoot)
            {
                themedRoot.RequestedTheme = SettingsService.Current.Theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default,
                };
            }
        }
        catch { }

        // Taskbar/titlebar icon matches the app/installer icon
        try { this.AppWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico")); }
        catch { }

        // Apply localized strings (and keep them live: the nav pane listens
        // for language changes; cached pages refresh in OnNavigatedTo).
        var loc = LocalizationService.Current;
        NavHomeItem.Content = loc.GetString("NavHome");
        NavAboutItem.Content = loc.GetString("NavAbout");
        loc.LanguageChanged += (_, _) =>
        {
            try
            {
                NavHomeItem.Content = LocalizationService.Current.GetString("NavHome");
                NavAboutItem.Content = LocalizationService.Current.GetString("NavAbout");
            }
            catch { }
        };

        // Title bar setup
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        SetTitleBarColors();

        // Native min window size + responsive navigation pane
        EnforceMinWindowSize();
        this.SizeChanged += MainWindow_SizeChanged;
        UpdateNavigationPaneMode(this.AppWindow.Size.Width);

        // Register routes
        _nav.RegisterRoute("home", typeof(HomePage));
        _nav.RegisterRoute("about", typeof(AboutPage));
        _nav.RegisterRoute("settings", typeof(SettingsPage));
        _nav.SetFrame(ContentFrame);
        _nav.Navigated += OnNavigated;

        // Navigate to initial page
        _nav.NavigateTo("home");

        // Restore window state
        RestoreWindowState();

        // Save window state on close
        this.Closed += MainWindow_Closed;

        // Refresh title bar colors when theme changes
        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, _) => SetTitleBarColors();

        // Initialize system tray
        SystemTrayService.Current.Initialize(this);
        SystemTrayService.Current.NavigationRequested += OnTrayNavigationRequested;

        // Initialize notification service
        NotificationService.Current.Initialize(NotificationHost);

        // Show first-run or what's-new dialog after window is shown
        _ = ShowFirstRunDialogIfNeeded();

        // Listen for second-instance activation signals (single-instance enforcement)
        Program.StartActivationListener(this.DispatcherQueue, BringToFront);
    }

    /// <summary>
    /// Fades in the main window content after splash screen transition.
    /// Called by App after splash closes.
    /// </summary>
    public async Task PlayEntranceAnimation()
    {
        var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(350)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
            {
                EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut
            }
        };

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, RootGrid);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");

        var story = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        story.Children.Add(fadeIn);
        story.Begin();

        await Task.Delay(350);
    }

    private void SetTitleBarColors()
    {
        var titleBar = this.AppWindow.TitleBar;
        var isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;

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

    /// <summary>
    /// Enforces the minimum window size natively via <c>WM_GETMINMAXINFO</c>,
    /// so the OS itself blocks shrinking below the limit (no flicker, works
    /// with snap/Aero and per-monitor DPI). Limits come from
    /// <see cref="ResponsiveLayout"/> and are scaled to physical pixels.
    /// </summary>
    private void EnforceMinWindowSize()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hwnd == IntPtr.Zero)
                return;
            _subclassProc = MainWindowSubclassProc;
            SetWindowSubclass(hwnd, Marshal.GetFunctionPointerForDelegate(_subclassProc), IntPtr.Zero, IntPtr.Zero);
        }
        catch { }
    }

    private int MainWindowSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
    {
        const uint WM_GETMINMAXINFO = 0x0024;
        if (uMsg == WM_GETMINMAXINFO && lParam != IntPtr.Zero)
        {
            try
            {
                var dpi = GetDpiForWindow(hWnd);
                if (dpi == 0)
                    dpi = 96;
                var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                info.ptMinTrackSize.X = ResponsiveLayout.ScaleLogicalToPhysical(ResponsiveLayout.MinWindowWidth, dpi);
                info.ptMinTrackSize.Y = ResponsiveLayout.ScaleLogicalToPhysical(ResponsiveLayout.MinWindowHeight, dpi);
                Marshal.StructureToPtr(info, lParam, false);
                return 0;
            }
            catch { }
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        UpdateNavigationPaneMode(args.Size.Width);
    }

    /// <summary>
    /// Switches the nav pane between full and compact based on window width,
    /// mirroring the page-level <c>AdaptiveTrigger</c> breakpoint so content
    /// never gets squeezed into clipping.
    /// </summary>
    private void UpdateNavigationPaneMode(double windowWidth)
    {
        try
        {
            RootNavigationView.PaneDisplayMode = ResponsiveLayout.ShouldUseCompactPane(windowWidth)
                ? NavigationViewPaneDisplayMode.LeftCompact
                : NavigationViewPaneDisplayMode.Left;
        }
        catch { }
    }

    private void RestoreWindowState()
    {
        try
        {
            if (_windowState.HasSavedState)
            {
                this.AppWindow.Move(new Windows.Graphics.PointInt32(
                    (int)_windowState.X,
                    (int)_windowState.Y));

                var presenter = this.AppWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    if (_windowState.IsMaximized)
                        presenter.Maximize();
                    else
                        presenter.Restore();
                }
            }
        }
        catch { }
    }

    private void MainWindow_Closed(object? sender, WindowEventArgs args)
    {
        if (SystemTrayService.Current.HandleWindowClose())
        {
            args.Handled = true;
            return;
        }

        SaveWindowState();

        // Real exit: tear down the tray icon or Windows keeps a ghost
        // icon after the process dies.
        SystemTrayService.Current.Shutdown();
    }

    private void SaveWindowState()
    {
        try
        {
            var pos = this.AppWindow.Position;
            _windowState.X = pos.X;
            _windowState.Y = pos.Y;

            var presenter = this.AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                _windowState.IsMaximized = presenter.State == OverlappedPresenterState.Maximized;
                if (presenter.State == OverlappedPresenterState.Restored)
                {
                    _windowState.Width = this.AppWindow.Size.Width;
                    _windowState.Height = this.AppWindow.Size.Height;
                }
            }
        }
        catch { }
    }

    private void OnNavigated(object? sender, string tag)
    {
        // The built-in settings item lives outside MenuItems/FooterMenuItems,
        // so sync it explicitly — otherwise the previous item stays highlighted.
        if (string.Equals(tag, "settings", StringComparison.OrdinalIgnoreCase))
        {
            RootNavigationView.SelectedItem = RootNavigationView.SettingsItem;
            return;
        }

        // Sync NavigationView selection to the navigated page
        foreach (var item in RootNavigationView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string t && t == tag)
            {
                RootNavigationView.SelectedItem = item;
                return;
            }
        }

        // Check footer items (if any)
        foreach (var item in RootNavigationView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string t && t == tag)
            {
                RootNavigationView.SelectedItem = item;
                return;
            }
        }
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // Settings is handled by NavigationView internally (IsSettingsVisible=true)
        if (args.IsSettingsInvoked)
        {
            _nav.NavigateTo("settings");
            return;
        }

        if (args.InvokedItemContainer?.Tag is not string tag)
            return;

        _nav.NavigateTo(tag);
    }

    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        _nav.GoBack();
    }

    private void OnTrayNavigationRequested(object? sender, TrayNavigationRequest request)
    {
        ShowFromTray();
        _nav.NavigateTo(request.Target, request.ToParameter());
    }

    private void ShowFromTray()
    {
        try
        {
            this.AppWindow.Show();
            this.Activate();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0040);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0040);
        }
        catch { }
    }

    private void BringToFront()
    {
        try
        {
            this.AppWindow.Show();
            this.Activate();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetForegroundWindow(hwnd);
        }
        catch { }
    }

    private void TrayButton_Click(object sender, RoutedEventArgs e)
    {
        SystemTrayService.Current.HideToTray();
    }

    private async Task ShowFirstRunDialogIfNeeded()
    {
        var firstRun = FirstRunService.Current;
        await Task.Delay(500);

        if (firstRun.IsFirstRun)
        {
            await ShowWelcomeDialog();
            firstRun.MarkAsShown();
        }
        else if (firstRun.HasBeenUpdated)
        {
            await ShowWhatsNewDialog();
            firstRun.MarkAsShown();
        }
    }

    private async Task ShowWelcomeDialog()
    {
        try
        {
            var loc = LocalizationService.Current;
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = loc.GetString("FirstRunTitle"),
                Content = loc.GetString("FirstRunContent"),
                PrimaryButtonText = loc.GetString("FirstRunButton"),
                DefaultButton = ContentDialogButton.Primary
            };
            await dialog.ShowAsync();
        }
        catch { }
    }

    private async Task ShowWhatsNewDialog()
    {
        try
        {
            var firstRun = FirstRunService.Current;
            var dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = $"What's New in v{AppInfo.Current.Version}",
                Content = firstRun.GetChangelog(),
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };
            await dialog.ShowAsync();
        }
        catch { }
    }
}
