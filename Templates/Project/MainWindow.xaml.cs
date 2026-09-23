using System;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
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

        // Register routes
        // <devtem:routes>
        // Register application-owned routes here; keep the built-in routes
        // and navigation lifecycle below this marker intact.
        // </devtem:routes>
        _nav.RegisterRoute("home", typeof(HomePage));
        _nav.RegisterRoute("about", typeof(AboutPage));
#if (health)
        _nav.RegisterRoute("diagnostics", typeof(DiagnosticsPage));
#else
        CollapseFooterNavItem("diagnostics");
#endif
        _nav.RegisterRoute("settings", typeof(SettingsPage));
#if (setup)
        _nav.RegisterRoute("setupwizard", typeof(SetupWizardPage));
#endif
        _nav.SetFrame(ContentFrame);
        _nav.Navigated += OnNavigated;

        // Rail items come from the single nav contract
        // (Services/NavigationRegistry): no XAML anchor for page wiring to
        // break. Built before the initial navigation so the highlight sync
        // in OnNavigated finds its item.
        BuildNavItems();

        // Navigate to initial page
        _nav.NavigateTo("home");

        // App-wide keyboard support (KeyboardShortcuts owns the reservation
        // list; this is the thin event wiring). Bubbling: focused controls
        // keep their own keys (dropdowns, dialogs, text input); unhandled
        // reserved chords arrive here. Never throws.
        RootGrid.KeyDown += RootGrid_KeyDown;
        RootGrid.PointerPressed += RootGrid_PointerPressed;

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
#if (tray)
                try { SystemTrayService.Current.RefreshThemeIcon(isDark); } catch { }
#endif
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
    }

    /// <summary>
    /// Post-first-frame init: tray icon/window, notifications, OS toasts,
    /// update dialogs, and the first-run/what's-new flow. Runs on the UI
    /// thread via a low-priority dispatcher item (see ctor). Never throws.
    /// Each service initializes in its own guard: one failing service must
    /// never abort the rest (a single shared try used to leave the app
    /// silently half-wired — e.g. no update surface and no first-run
    /// dialog — with only one log line).
    /// </summary>
    private void InitializeDeferredServices()
    {
        // P0-4: second-instance listener spawns its thread here, past the
        // first frame — the ctor keeps chrome + navigation only.
        try { Program.StartActivationListener(this.DispatcherQueue, BringToFront); } catch { }

#if (tray)
        try
        {
            if (Content is FrameworkElement themedRootForIcon)
            {
                bool initialDark = themedRootForIcon.ActualTheme == ElementTheme.Dark;
                try { SystemTrayService.Current.RefreshThemeIcon(initialDark); } catch { }
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deferred window icon init failed");
        }

        try
        {
            // Initialize system tray
            SystemTrayService.Current.Initialize(this);
            SystemTrayService.Current.NavigationRequested += OnTrayNavigationRequested;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deferred tray init failed");
        }
#endif

        try
        {
            // Initialize notification services (in-app cards + OS toasts)
            NotificationService.Current.Initialize(NotificationHost);
#if (tray)
            DesktopToastService.Current.Initialize();
            DesktopToastService.Current.ActivationRequested += (_, _) =>
            {
                try { DispatcherQueue.TryEnqueue(() => WindowActivator.ShowAndActivate(this)); } catch { }
            };
#endif
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deferred notification init failed");
        }

        try
        {
            // The single update surface (toasts + native dialogs).
            UpdateDialogService.Initialize(this, UpdatePopupControl);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deferred update dialog init failed");
        }

        try
        {
            // Show first-run or what's-new dialog after window is shown
            _ = FirstRunDialogService.ShowIfNeededAsync(this);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deferred first-run init failed");
        }
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
#if (tray)
        if (SystemTrayService.Current.HandleWindowClose())
        {
            args.Handled = true;
            return;
        }
#endif

        SaveWindowState();

        // Flush any coalesced settings write (P0-2) before teardown.
        try { LocalSettingsStore.FlushShared(); } catch { }

        // Flush any queued crash reports on the way out.
        CrashReportingService.Current.Shutdown();

#if (tray)
        // Real exit: tear down the tray icon or Windows keeps a ghost
        // icon after the process dies.
        SystemTrayService.Current.Shutdown();
        DesktopToastService.Current.Shutdown();
#endif
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
    /// otherwise keeps the OS language label) and re-reads every registry
    /// item label — declared nav items bind to NavEntry.Label, not XAML, so
    /// they refresh here: at startup, once the pane materializes (Loaded),
    /// and on every language change — alongside stable AutomationIds for
    /// the FlaUI smoke tests (UI/): names localize, Ids don't.
    /// </summary>
    private void ApplyNavLocalization()
    {
        try
        {
            try { NavigationRegistry.RefreshLabels(); } catch { }
            if (RootNavigationView.SettingsItem is NavigationViewItem settingsItem)
            {
                settingsItem.Content = LocalizationService.Current.GetString("NavSettings");
                AutomationProperties.SetAutomationId(settingsItem, "NavSettingsItem");
                // Native selection stays enabled (default True) so the
                // settings pill shows like every other item.
            }
        }
        catch { }
    }

    /// <summary>
    /// Hides a footer nav item whose page was dropped at scaffold time
    /// (XAML carries no engine markers, so this runs in code). Never throws.
    /// </summary>
    private void CollapseFooterNavItem(string tag)
    {
        try
        {
            foreach (var item in RootNavigationView.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem &&
                    navItem.Tag is string t &&
                    string.Equals(t, tag, StringComparison.Ordinal))
                {
                    navItem.Visibility = Visibility.Collapsed;
                    return;
                }
            }
        }
        catch { }
    }

    private void OnNavigated(object? sender, string tag)
    {
        // Native selection is enabled, so clicks already selected the item
        // before this runs. Only re-sync when the navigation did NOT come
        // from a click (back, tray, deep link, initial navigate): setting
        // the same item again is a harmless no-op, setting a different one
        // moves the pill. Never clear the selection — a cleared pill reads
        // as "no current page".
        // The built-in settings item lives outside MenuItems/FooterMenuItems,
        // so sync it explicitly — otherwise the previous item stays highlighted.
        if (string.Equals(tag, "settings", StringComparison.OrdinalIgnoreCase))
        {
            if (!ReferenceEquals(RootNavigationView.SelectedItem, RootNavigationView.SettingsItem))
                RootNavigationView.SelectedItem = RootNavigationView.SettingsItem;
            return;
        }

        // Sync NavigationView selection to the navigated page
        foreach (var item in RootNavigationView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string t && t == tag)
            {
                if (!ReferenceEquals(RootNavigationView.SelectedItem, item))
                    RootNavigationView.SelectedItem = item;
                return;
            }
        }

        // Check footer items (if any)
        foreach (var item in RootNavigationView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag is string t && t == tag)
            {
                if (!ReferenceEquals(RootNavigationView.SelectedItem, item))
                    RootNavigationView.SelectedItem = item;
                return;
            }
        }
    }

    /// <summary>
    /// Builds the rail + footer items from <see cref="NavigationRegistry"/>
    /// (the single nav contract). Idempotent: clears first, so a second
    /// call (tests, re-init) never duplicates items. Never throws.
    /// </summary>
    private void BuildNavItems()
    {
        try
        {
            RootNavigationView.MenuItems.Clear();
            foreach (var entry in NavigationRegistry.MenuEntries)
            {
                try
                {
                    var item = entry.CreateItem();
                    if (item is not null)
                        RootNavigationView.MenuItems.Add(item);
                }
                catch { }
            }
            RootNavigationView.FooterMenuItems.Clear();
            foreach (var entry in NavigationRegistry.FooterEntries)
            {
                try
                {
                    var item = entry.CreateItem();
                    if (item is not null)
                        RootNavigationView.FooterMenuItems.Add(item);
                }
                catch { }
            }
        }
        catch { }
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        // Native selection (SelectsOnInvoked=True): the click selects the
        // item and this handler navigates. The overlay pane auto-closes on
        // selection (standard drawer behavior) — no decoupling needed.
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

    /// <summary>
    /// Reserved-chord handler: Alt+Left/Right (back/forward), Ctrl+,
    /// (Settings), Escape (dismiss the update flow). Fires only when no
    /// focused control handled the key; marks handled only when the action
    /// actually ran (GoBack with an empty stack stays unhandled).
    /// </summary>
    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            bool ctrl = IsKeyDown(VirtualKey.Control);
            bool alt = IsKeyDown(VirtualKey.Menu);
            switch (KeyboardShortcuts.Resolve(e.Key, ctrl, alt))
            {
                case KeyboardAction.GoBack:
                    if (_nav.GoBack())
                        e.Handled = true;
                    break;
                case KeyboardAction.GoForward:
                    if (_nav.GoForward())
                        e.Handled = true;
                    break;
                case KeyboardAction.OpenSettings:
                    _nav.NavigateTo("settings");
                    e.Handled = true;
                    break;
                case KeyboardAction.DismissUpdateFlow:
                    _ = UpdateDialogService.DismissAsync();
                    e.Handled = true;
                    break;
            }
        }
        catch { }
    }

    private static bool IsKeyDown(VirtualKey key)
    {
        try
        {
            return InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Mouse back/forward (X1/X2) anywhere in the window, mirroring
    /// Alt+Left/Right. Thin wiring; the stack guard lives in the service.
    /// </summary>
    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            var props = e.GetCurrentPoint(null).Properties;
            if (props.IsXButton1Pressed && _nav.GoBack())
                e.Handled = true;
            else if (props.IsXButton2Pressed && _nav.GoForward())
                e.Handled = true;
        }
        catch { }
    }

    private void OnTrayNavigationRequested(object? sender, TrayNavigationRequest request)
    {
        WindowActivator.ShowAndActivate(this);
        // Single trigger: the explicit check below is the whole flow.
        // (Passing the check parameter too would arm the Settings page's
        // Loaded auto-check as a SECOND concurrent flow; the superseded
        // loser used to surface a bogus "check failed" toast.)
        _nav.NavigateTo(request.Target);
        // The tray "check updates" entry runs the update flow over the
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
#if (tray)
        SystemTrayService.Current.HideToTray();
#endif
    }
}
