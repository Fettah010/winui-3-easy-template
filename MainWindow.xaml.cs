using System;
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
    private readonly WindowStateService _windowState = WindowStateService.Current;

    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "DevTem-WinUI 3";
        this.SystemBackdrop = new MicaBackdrop();

        // Apply localized strings
        var loc = LocalizationService.Current;
        NavHomeItem.Content = loc.GetString("NavHome");
        NavAboutItem.Content = loc.GetString("NavAbout");
        NavSettingsItem.Content = loc.GetString("NavSettings");

        // Native-feeling, theme-aware title bar: the app content extends into the
        // caption area, Mica shows through it, and the caption buttons (min/max/
        // close) stay OS-drawn with automatic light/dark colors.
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        SetTitleBarColors();

        // Restore window state
        RestoreWindowState();

        // Save window state on close
        this.Closed += MainWindow_Closed;

        // Refresh title bar colors when theme changes
        if (Content is FrameworkElement root)
            root.ActualThemeChanged += (_, _) => SetTitleBarColors();

        ContentFrame.Navigate(typeof(HomePage));
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];

        // Initialize system tray
        SystemTrayService.Current.Initialize(this);
        SystemTrayService.Current.NavigationRequested += OnTrayNavigationRequested;

        // Initialize notification service
        NotificationService.Current.Initialize(NotificationHost);

        // Show first-run or what's-new dialog after window is shown
        _ = ShowFirstRunDialogIfNeeded();
    }

    private void SetTitleBarColors()
    {
        var titleBar = this.AppWindow.TitleBar;
        var isDark = (Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark;

        // Active state
        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonForegroundColor = isDark
            ? Microsoft.UI.Colors.White
            : Microsoft.UI.Colors.Black;

        // Hover state
        titleBar.ButtonHoverBackgroundColor = isDark
            ? Microsoft.UI.ColorHelper.FromArgb(40, 255, 255, 255)
            : Microsoft.UI.ColorHelper.FromArgb(30, 0, 0, 0);
        titleBar.ButtonHoverForegroundColor = isDark
            ? Microsoft.UI.Colors.White
            : Microsoft.UI.Colors.Black;

        // Pressed state
        titleBar.ButtonPressedBackgroundColor = isDark
            ? Microsoft.UI.ColorHelper.FromArgb(60, 255, 255, 255)
            : Microsoft.UI.ColorHelper.FromArgb(50, 0, 0, 0);
        titleBar.ButtonPressedForegroundColor = isDark
            ? Microsoft.UI.Colors.White
            : Microsoft.UI.Colors.Black;

        // Inactive state
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveForegroundColor = isDark
            ? Microsoft.UI.ColorHelper.FromArgb(120, 255, 255, 255)
            : Microsoft.UI.ColorHelper.FromArgb(120, 0, 0, 0);
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
        // If tray is active and minimize-to-tray is on, intercept the close
        if (SystemTrayService.Current.HandleWindowClose())
        {
            args.Handled = true;
            return;
        }

        SaveWindowState();
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

    private async Task ShowFirstRunDialogIfNeeded()
    {
        var firstRun = FirstRunService.Current;

        // Wait for UI to be ready
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
                Content = "A ready-to-use template for WinUI 3 desktop apps.\n\n" +
                          "This template includes:\n" +
                          "• Settings with theme selector\n" +
                          "• Auto-updates via GitHub Releases\n" +
                          "• Logging system\n" +
                          "• Desktop shortcut support\n\n" +
                          "Get started by exploring the app!",
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

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is not string tag)
            return;

        switch (tag)
        {
            case "home":
                ContentFrame.Navigate(typeof(HomePage));
                break;
            case "about":
                ContentFrame.Navigate(typeof(AboutPage));
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
        }
    }

    private void TrayButton_Click(object sender, RoutedEventArgs e)
    {
        SystemTrayService.Current.HideToTray();
    }

    private void OnTrayNavigationRequested(object? sender, string target)
    {
        switch (target)
        {
            case "settings":
                ContentFrame.Navigate(typeof(Pages.SettingsPage));
                RootNavigationView.SelectedItem = RootNavigationView.FooterMenuItems[1];
                break;
            case "home":
                ContentFrame.Navigate(typeof(Pages.HomePage));
                RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
                break;
        }
    }
}
