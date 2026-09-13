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
            CrashReportingService.Current.CaptureException(e.Exception, "ui-thread");
            e.Handled = false;
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Show splash screen immediately (it reports real init phases below)
        _splash = new SplashScreen();
        _splash.Activate();
        LoggingService.Log.Information(
            "Splash shown after {ElapsedMs}ms", Program.StartupStopwatch.ElapsedMilliseconds);

        // Initialize services while splash is visible
        await InitializeServicesAsync();

        // Create main window (hidden initially)
        _mainWindow = new MainWindow();
        m_window = _mainWindow;
        _splash?.ReportProgress(0.85, LocalizationService.Current.GetString("SplashPreparingWindow"));

        // Animate transition: splash fades out, main window fades in
        await TransitionToMainWindow();
        LoggingService.Log.Information(
            "Main window shown after {ElapsedMs}ms", Program.StartupStopwatch.ElapsedMilliseconds);

        // Deep link that started this process (if any) wins over the home page.
        HandlePendingProtocolUri();

        // Heavy work deferred past the first frame so the window appears ASAP:
        // database init and the update check run while the user already sees UI.
        // The periodic loop keeps trayed (long-running) apps current too.
        _ = InitializeDatabaseAsync();
        _ = CheckForUpdatesAsync();
        _ = RunPeriodicChecksAsync();
    }

    private async Task InitializeServicesAsync()
    {
        await Task.Run(() =>
        {
            ServiceLocator.Initialize();
            LocalizationService.Current.Initialize();
            // Unpackaged installs have no manifest: claim our deep-link
            // scheme per-user (HKCU, no admin); MSIX covers itself.
            ProtocolService.EnsureRegistered();
        });

        // One-time channel migration (beta builds holding a stale channel).
        SettingsService.Current.EnsureChannelForCurrentBuild();

        var loc = LocalizationService.Current;
        _splash?.ReportProgress(0.4, loc.GetString("SplashLoadingServices"));
        LoggingService.Log.Information(
            "Services ready after {ElapsedMs}ms", Program.StartupStopwatch.ElapsedMilliseconds);
    }

    private static async Task InitializeDatabaseAsync()
    {
        try
        {
            var db = ServiceLocator.GetRequiredService<DatabaseService>();
            await db.InitializeAsync();
        }
        catch (Exception ex)
        {
            // DatabaseService.InitializeAsync is idempotent; a page that needs
            // the DB earlier triggers init itself, so this is best-effort.
            LoggingService.Log.Error(ex, "Deferred database init failed");
        }
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

    /// <summary>
    /// Navigates to the deep link that started this process, if any.
    /// Runs after the main window is visible so the nav frame exists.
    /// </summary>
    private void HandlePendingProtocolUri()
    {
        try
        {
            var pending = Program.PendingProtocolUri;
            if (string.IsNullOrWhiteSpace(pending))
                return;
            if (NavigationService.Current.TryNavigateByUri(pending, out var tag))
                LoggingService.Log.Information("Deep link handled on launch: {Tag}", tag);
            else
                LoggingService.Log.Warning("Deep link had no route: {Uri}", pending);
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Deep link handling on launch failed");
        }
    }

    /// <summary>
    /// Re-checks for updates on <see cref="UpdateService.PeriodicCheckInterval"/>
    /// while the app stays running. <see cref="CheckForUpdatesAsync"/> itself
    /// honors the auto-check setting and the installed-app guard, so this loop
    /// is a no-op for opted-out or unpackaged runs (one log line per tick).
    /// </summary>
    private async Task RunPeriodicChecksAsync()
    {
        try
        {
            LoggingService.Log.Information(
                "Periodic update checks scheduled every {Interval}", UpdateService.PeriodicCheckInterval);
            using var timer = new PeriodicTimer(UpdateService.PeriodicCheckInterval);
            while (await timer.WaitForNextTickAsync())
            {
                await CheckForUpdatesAsync();
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log.Error(ex, "Periodic update check loop ended");
        }
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
