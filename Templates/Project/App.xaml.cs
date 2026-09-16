using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Services;

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
            AppLog.Fatal(e.Exception, "Unhandled UI-thread exception");
            CrashReportingService.Current.CaptureException(e.Exception, "ui-thread");
            e.Handled = false;
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Startup trace (spans are no-ops without a listener) + phase
        // timings for the diagnostics page (work with no listener at all).
        using var launchSpan = DevTemWinUi3.Services.Diagnostics.AppTrace.StartStartup();

        // Show splash screen immediately (it reports real init phases below)
        _splash = new SplashScreen();
        _splash.Activate();
        AppLog.Information(
            "Splash shown after {ElapsedMs}ms", Program.StartupStopwatch.ElapsedMilliseconds);
        DevTemWinUi3.Services.Diagnostics.AppMetrics.RecordStartupPhase(
            "splash", Program.StartupStopwatch.ElapsedMilliseconds);

        // Initialize services while splash is visible
        using (DevTemWinUi3.Services.Diagnostics.AppTrace.StartPhase("services"))
        {
            await InitializeServicesAsync();
        }
        DevTemWinUi3.Services.Diagnostics.AppMetrics.RecordStartupPhase(
            "services", Program.StartupStopwatch.ElapsedMilliseconds);

        // Create main window (hidden initially)
        _mainWindow = new MainWindow();
        MainWindowInstance = _mainWindow;
        _splash?.ReportProgress(0.85, LocalizationService.Current.GetString("SplashPreparingWindow"));

        // Animate transition: splash fades out, main window fades in
        using (DevTemWinUi3.Services.Diagnostics.AppTrace.StartPhase("window"))
        {
            await TransitionToMainWindow();
        }
        AppLog.Information(
            "Main window shown after {ElapsedMs}ms", Program.StartupStopwatch.ElapsedMilliseconds);
        DevTemWinUi3.Services.Diagnostics.AppMetrics.RecordStartupPhase(
            "window", Program.StartupStopwatch.ElapsedMilliseconds);

        // Deep link that started this process (if any) wins over the home page.
        // Unpackaged: command line (Program.PendingProtocolUri, already
        // activation-resolved in Program for packaged runs).
        HandlePendingProtocolUri(Program.PendingProtocolUri ?? ProtocolService.GetPackagedProtocolUri());

        // Heavy work deferred past the first frame so the window appears ASAP:
        // database init and the update check run while the user already sees UI.
        // The periodic loop keeps trayed (long-running) apps current too.
        // Owned by BackgroundUpdateService; App stays launch orchestration.
#if (database)
        _ = DatabaseInitializer.InitializeAsync();
#endif
#if (updates == 'velopack' || updates == 'basic')
        _ = BackgroundUpdateService.Current.CheckForUpdatesAsync(MainWindowInstance);
        _ = BackgroundUpdateService.Current.RunPeriodicChecksAsync(MainWindowInstance);
#endif
    }

    private async Task InitializeServicesAsync()
    {
        await Task.Run(() =>
        {
            ServiceLocator.Initialize();
            // Warm the settings cache off the UI thread so later Gets are
            // memory-only (P0-2); the first load is the only disk touch.
            LocalSettingsStore.PreloadShared();
            LocalizationService.Current.Initialize();
            // Unpackaged installs have no manifest: claim our deep-link
            // scheme per-user (HKCU, no admin); MSIX covers itself.
            ProtocolService.EnsureRegistered();
        });

        // One-time channel migration (beta builds holding a stale channel).
        SettingsService.Current.EnsureChannelForCurrentBuild();

        var loc = LocalizationService.Current;
        _splash?.ReportProgress(0.4, loc.GetString("SplashLoadingServices"));
        AppLog.Information(
            "Services ready after {ElapsedMs}ms", Program.StartupStopwatch.ElapsedMilliseconds);
    }

    private Task TransitionToMainWindow()
    {
        if (_splash is null || _mainWindow is null) return Task.CompletedTask;

        // P0-1: no fixed sleeps on the critical path. The splash close
        // animation runs while the main window activates underneath it;
        // time-to-interactive is the Activate, not the fade. The entrance
        // animation is fire-and-forget for the same reason.
        var splash = _splash;
        _splash = null;

        _mainWindow.Activate();

        // Splash teardown (its close animation, then Close) finishes in the
        // background; a fault there must never take down the launch.
        _ = splash.CloseWithAnimation().ContinueWith(
            t =>
            {
                if (t.IsFaulted)
                {
                    try { AppLog.Error(t.Exception, "Splash close failed"); } catch { }
                }
            },
            TaskScheduler.Default);

        try
        {
            _ = WindowChromeService.Current.PlayEntranceAnimation(_mainWindow.ContentRoot);
        }
        catch { }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navigates to the deep link that started this process, if any.
    /// Runs after the main window is visible so the nav frame exists.
    /// </summary>
    private void HandlePendingProtocolUri(string? pending)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pending))
                return;
            if (NavigationService.Current.TryNavigateByUri(pending, out var tag))
                AppLog.Information("Deep link handled on launch: {Tag}", tag);
            else
                AppLog.Warning("Deep link had no route: {Uri}", pending);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Deep link handling on launch failed");
        }
    }

    internal Window? MainWindowInstance;
}
