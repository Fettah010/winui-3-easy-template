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
    /// </summary>
    public static async Task ShowIfNeededAsync(Window window)
    {
        var firstRun = FirstRunService.Current;
        await Task.Delay(500);

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
