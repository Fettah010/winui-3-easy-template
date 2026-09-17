using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.Services;

/// <summary>
/// First-run welcome and post-update what's-new dialogs. Owned here so
/// <c>MainWindow</c> stays composition. UI-bound (needs a window), so
/// untested headless by house rule — every path is never-throw guarded.
/// </summary>
public static class FirstRunDialogService
{
    /// <summary>
    /// Shows the welcome dialog on first run or the what's-new dialog after
    /// an update, then records the version as shown. No-op otherwise.
    /// P0-1: no fixed settle delay — the caller (MainWindow) already defers
    /// this past the first frame, so waiting again only taxes launch.
    /// The setup wizard never auto-opens: install-time choices (location,
    /// shortcuts, launch) belong to the installer — the MSIX package owns
    /// them on packaged runs and Velopack's setup owns them on portable
    /// runs — so first run lands on Home with a welcome dialog, never a
    /// Next/Back stepper inside the app.
    /// </summary>
    public static async Task ShowIfNeededAsync(Window window)
    {
        var firstRun = FirstRunService.Current;
        // Yield once so a caller that runs inline in window construction
        // still lets the first frame render before the modal appears.
        await Task.Yield();

        if (firstRun.IsFirstRun)
        {
            await ShowWelcomeAsync(window);
            firstRun.MarkAsShown();
        }
        else if (firstRun.HasBeenUpdated)
        {
            await ShowWhatsNewAsync(window);
            firstRun.MarkAsShown();
        }
    }

    internal static bool ShouldShowSetupWizard()
    {
        // Retired: the in-app wizard no longer auto-opens on any track.
        // Kept (returning false) so the setup feature flag still compiles
        // out the page via --setup false without touching callers.
        return false;
    }

    private static async Task ShowWelcomeAsync(Window window)
    {
        try
        {
            var loc = LocalizationService.Current;
            var dialog = new ContentDialog
            {
                XamlRoot = window.Content.XamlRoot,
                Title = loc.GetString("FirstRunTitle"),
                Content = loc.GetString("FirstRunContent"),
                PrimaryButtonText = loc.GetString("FirstRunButton"),
                DefaultButton = ContentDialogButton.Primary
            };
            await dialog.ShowAsync();
        }
        catch { }
    }

    private static async Task ShowWhatsNewAsync(Window window)
    {
        try
        {
            var firstRun = FirstRunService.Current;
            var dialog = new ContentDialog
            {
                XamlRoot = window.Content.XamlRoot,
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
