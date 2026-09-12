using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Services;
using Velopack;

namespace DevTemWinUi3;

public partial class App : Application
{
    private SplashScreen? _splash;
    private MainWindow? _mainWindow;

    public App()
    {
        this.InitializeComponent();

        this.UnhandledException += (_, e) =>
        {
            LoggingService.Log.Fatal(e.Exception, "Unhandled UI-thread exception");
            e.Handled = false;
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Show splash screen immediately
        _splash = new SplashScreen();
        _splash.Activate();

        // Initialize services while splash is visible
        await InitializeServicesAsync();

        // Create main window (hidden initially)
        _mainWindow = new MainWindow();
        m_window = _mainWindow;

        // Animate transition: splash fades out, main window fades in
        await TransitionToMainWindow();

        _ = CheckForUpdatesAsync();
    }

    private static async Task InitializeServicesAsync()
    {
        await Task.Run(() =>
        {
            ServiceLocator.Initialize();
            LocalizationService.Current.Initialize();
        });

        var db = ServiceLocator.GetRequiredService<DatabaseService>();
        await db.InitializeAsync();
    }

    private async Task TransitionToMainWindow()
    {
        if (_splash is null || _mainWindow is null) return;

        // Close splash with animation
        var splashCloseTask = _splash.CloseWithAnimation();

        // Small overlap — start activating main window before splash fully closes
        await Task.Delay(100);

        _mainWindow.Activate();
        _splash = null;

        // Wait for splash close animation to finish
        await splashCloseTask;

        // Fade in main window content
        await _mainWindow.PlayEntranceAnimation();
    }

    private async Task CheckForUpdatesAsync()
    {
        var svc = UpdateService.Current;

        if (!svc.IsInstalled)
        {
            LoggingService.Log.Information("Auto-update check skipped: app is not installed");
            return;
        }

        try
        {
            if (!SettingsService.Current.AutoCheck)
            {
                LoggingService.Log.Information("Auto-update check skipped: disabled in settings");
                return;
            }
        }
        catch { }

        try
        {
            var update = await svc.CheckForUpdatesAsync();
            if (update is null)
            {
                LoggingService.Log.Information("Auto-update: already on the latest version");
                return;
            }

            LoggingService.Log.Information(
                "Auto-update: v{Version} available, downloading in the background",
                update.TargetFullRelease.Version);
            await svc.DownloadUpdatesAsync(update);

            if (m_window is null)
                return;
            m_window.DispatcherQueue.TryEnqueue(() => PromptRestart(update));
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Auto-update check failed");
        }
    }

    private async void PromptRestart(UpdateInfo update)
    {
        try
        {
            var window = m_window;
            if (window?.Content is null)
                return;

            var dialog = new ContentDialog
            {
                XamlRoot = window.Content.XamlRoot,
                Title = $"Update v{update.TargetFullRelease.Version} ready",
                Content = "The new version has been downloaded. Restart now to finish the update?",
                PrimaryButtonText = "Restart now",
                CloseButtonText = "Later",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                UpdateService.Current.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Restart prompt failed");
        }
    }

    internal Window? m_window;
}
