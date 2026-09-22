using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.Services;

/// <summary>
/// The template's dialog pattern (pain-log #20): a <see cref="ContentDialog"/>
/// declared <c>Visibility="Collapsed"</c> in XAML silently never shows —
/// <c>ShowAsync()</c> no-ops with no exception and no log. Always show
/// dialogs through here: it uncollapses around <c>ShowAsync</c> (restoring
/// in <c>finally</c>) with a reentrancy guard, so stacked callers serialize
/// instead of throwing "only one dialog at a time".
/// UI-bound by house rule: never throws; headless callers get
/// <c>ContentDialogResult.None</c>.
/// </summary>
public static class DialogHelper
{
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// Shows the dialog safely: restores <see cref="UIElement.Visibility"/>
    /// around the call and serializes concurrent shows. Returns
    /// <see cref="ContentDialogResult.None"/> when there is no UI thread.
    /// </summary>
    public static async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        if (dialog is null)
            return ContentDialogResult.None;

        bool entered = false;
        try
        {
            try
            {
                // Never block the UI thread forever on the gate: a stuck
                // holder degrades to None instead of hanging the app.
                entered = await _gate.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch
            {
                return ContentDialogResult.None;
            }

            if (!entered)
            {
                try { AppLog.Warning("DialogHelper: another dialog is already showing; skipping."); } catch { }
                return ContentDialogResult.None;
            }

            Visibility? previous = null;
            try { previous = dialog.Visibility; } catch { }
            bool restored = false;
            try
            {
                try
                {
                    if (previous == Visibility.Collapsed)
                        dialog.Visibility = Visibility.Visible;
                }
                catch { }

                return await dialog.ShowAsync();
            }
            finally
            {
                try
                {
                    if (previous == Visibility.Collapsed && !restored)
                        dialog.Visibility = Visibility.Collapsed;
                }
                catch { }
            }
        }
        catch
        {
            return ContentDialogResult.None;
        }
        finally
        {
            if (entered)
            {
                try { _gate.Release(); } catch { }
            }
        }
    }
}
