using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using Velopack;

namespace DevTemWinUi3;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();

        // XAML/UI-thread exceptions: log them, but keep default handling.
        // (Setting e.Handled = true would suppress the crash; a template should
        //  not hide bugs, so we re-throw through the default path.)
        this.UnhandledException += (_, e) =>
        {
            LoggingService.Log.Fatal(e.Exception, "Unhandled UI-thread exception");
            e.Handled = false;
        };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Initialize DI container
        ServiceLocator.Initialize();

        // Initialize localization
        LocalizationService.Current.Initialize();

        // Initialize database
        var db = ServiceLocator.GetRequiredService<DatabaseService>();
        await db.InitializeAsync();

        m_window = new MainWindow();
        m_window.Activate();

        // Fire-and-forget: the update check runs off the UI thread after the
        // window is up, so startup is never blocked by the network.
        _ = CheckForUpdatesAsync();
    }

    /// <summary>
    /// Silent background auto-update flow: check the release feed, download in
    /// the background when an update exists, and only involve the user once it
    /// is ready, asking them to restart.
    /// </summary>
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
