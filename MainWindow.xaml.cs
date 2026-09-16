using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Pages;
using DevTemWinUi3.Services;
using DevTemWinUi3.Services.Native;

namespace DevTemWinUi3;

public sealed partial class MainWindow : Window
{
    private readonly WindowStateService _windowState = WindowStateService.Current;
    private readonly NavigationService _nav = NavigationService.Current;

    /// <summary>
    /// Root content element (entrance-animation target for App).
    /// </summary>
    public FrameworkElement ContentRoot => RootGrid;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = AppMetadata.AppName;

        // Apply the persisted theme on launch (single source: ThemeService).
        if (Content is FrameworkElement themedRoot)
            ThemeService.Current.ApplyTo(themedRoot);

        // Window dressing (Mica, title bar, icon, min size, colors).
        // P0-4: no icon disk I/O here — the initial theme icon resolves in
        // InitializeDeferredServices, past the first frame.
        WindowChromeService.Current.Initialize(this, AppTitleBar);

        // The built-in Settings item keeps the OS label, so set it
        // explicitly; declared nav items bind in XAML.
        ApplyNavLocalization();
        RootNavigationView.Loaded += (_, _) => ApplyNavLocalization();
        LocalizationService.Current.LanguageChanged += (_, _) => ApplyNavLocalization();

        // Responsive navigation pane (page-level breakpoints live in XAML).
        this.SizeChanged += MainWindow_SizeChanged;
        UpdateNavigationPaneMode(this.AppWindow.Size.Width);

        // Register routes
        // <devtem:routes>
        // Register application-owned routes here; keep the built-in routes
        // and navigation lifecycle below this marker intact.
        // </devtem:routes>
        _nav.RegisterRoute("home", typeof(HomePage));
        _nav.RegisterRoute("about", typeof(AboutPage));
        _nav.RegisterRoute("diagnostics", typeof(DiagnosticsPage));
        _nav.RegisterRoute("settings", typeof(SettingsPage));
        _nav.RegisterRoute("setupwizard", typeof(SetupWizardPage));
        _nav.SetFrame(ContentFrame);
        _nav.Navigated += OnNavigated;

        // Navigate to initial page
        _nav.NavigateTo("home");

        // Restore window state
        RestoreWindowState();

        // Save window state on close
        this.Closed += MainWindow_Closed;

        // Refresh title bar colors + theme icons when theme changes
        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, _) =>
            {
                bool isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;
                WindowChromeService.Current.RefreshForThemeChange(this, isDark);
                try { SystemTrayService.Current.RefreshThemeIcon(isDark); } catch { }
            };

        // P0-4: everything below is deferred past the first frame so the
        // ctor only builds chrome + navigation. The low-priority enqueue
        // runs after first render; a refusal falls back to inline init.
        bool deferred = false;
        try { deferred = DispatcherQueue.TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            () => InitializeDeferredServices()); } catch { }
        if (!deferred)
        {
            try { InitializeDeferredServices(); } catch { }
        }

        // Listen for second-instance activation signals (single-instance enforcement)
        Program.StartActivationListener(this.DispatcherQueue, BringToFront);
    }

    /// <summary>
    /// Post-first-frame init: tray icon/window, notifications, OS toasts,
    /// and the first-run/what's-new flow. Runs on the UI thread via a
    /// low-priority dispatcher item (see ctor). Never throws.
    /// </summary>
    private void InitializeDeferredServices()
    {
        try
        {
            if (Content is FrameworkElement themedRootForIcon)
            {
                bool initialDark = themedRootForIcon.ActualTheme == ElementTheme.Dark;
                try { SystemTrayService.Current.RefreshThemeIcon(initialDark); } catch { }
            }

            // Initialize system tray
            SystemTrayService.Current.Initialize(this);
            SystemTrayService.Current.NavigationRequested += OnTrayNavigationRequested;

            // Initialize notification services (in-app cards + OS toasts)
            NotificationService.Current.Initialize(NotificationHost);
            DesktopToastService.Current.Initialize();
            DesktopToastService.Current.ActivationRequested += (_, _) =>
            {
                try { DispatcherQueue.TryEnqueue(() => WindowActivator.ShowAndActivate(this)); } catch { }
            };

            // The single update surface (animated popup, not a page).
            UpdateDialogService.Initialize(this, UpdatePopupControl);

            // Show first-run or what's-new dialog after window is shown
            _ = FirstRunDialogService.ShowIfNeededAsync(this);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deferred window services init failed");
        }
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

        // Flush any coalesced settings write (P0-2) before teardown.
        try { LocalSettingsStore.FlushShared(); } catch { }

        // Flush any queued crash reports on the way out.
        CrashReportingService.Current.Shutdown();

        // Real exit: tear down the tray icon or Windows keeps a ghost
        // icon after the process dies.
        SystemTrayService.Current.Shutdown();
        DesktopToastService.Current.Shutdown();
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

    /// <summary>
    /// Applies the localized label to the built-in settings item (which
    /// otherwise keeps the OS language label). Declared nav items bind in
    /// XAML; this one has no XAML declaration, so it is set here — at
    /// startup, once the pane materializes (Loaded), and on every language
    /// change — alongside its stable AutomationId for the FlaUI smoke tests
    /// (UI/): names localize, Ids don't.
    /// </summary>
    private void ApplyNavLocalization()
    {
        try
        {
            if (RootNavigationView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Content = LocalizationService.Current.GetString("NavSettings");
                AutomationProperties.SetAutomationId(settingsItem, "NavSettingsItem");
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
        WindowActivator.ShowAndActivate(this);
        _nav.NavigateTo(request.Target, request.ToParameter());
        // The tray "check updates" entry opens the update popup over the
        // target page (the Update Center page is retired).
        if (request.AutoCheckUpdates)
        {
            try { _ = UpdateDialogService.ShowCheckAsync(); } catch { }
        }
    }

    private void BringToFront()
    {
        try
        {
            WindowActivator.ShowAndActivate(this);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowChromeNative.SetForegroundWindow(hwnd);

            // A second launch (e.g. a deep link) stashes its URI where we
            // look on every activation — navigate to it when present.
            if (ProtocolService.TryReadAndClearPendingUri(out var pending) &&
                !string.IsNullOrWhiteSpace(pending))
            {
                _nav.TryNavigateByUri(pending, out _);
            }
        }
        catch { }
    }

    private void TrayButton_Click(object sender, RoutedEventArgs e)
    {
        SystemTrayService.Current.HideToTray();
    }
}
