using System;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace DevTemWinUi3.Services;

/// <summary>
/// OS-level (Action Center) toast notifications via the Windows App SDK
/// <c>AppNotificationManager</c> — the native Windows 11 path for unpackaged
/// apps. Used for events the user must see even when the window is hidden:
/// minimize-to-tray and the not-installed update notice.
/// Everything is guarded: if registration or display fails, the calls simply
/// return false and callers fall back to in-app UI.
/// </summary>
public sealed class DesktopToastService
{
    /// <summary>Activation argument marking "reopen the app".</summary>
    public const string OpenAction = "open";

    public static DesktopToastService Current { get; } = new();

    private DesktopToastService() { }

    /// <summary>Raised when the user clicks a toast while it is visible.</summary>
    public event EventHandler? ActivationRequested;

    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Registers with the OS notification platform. Call once on the UI thread
    /// at startup. Never throws.
    /// </summary>
    public void Initialize()
    {
        try
        {
            var manager = AppNotificationManager.Default;
            manager.NotificationInvoked += OnNotificationInvoked;
            manager.Register();
            IsAvailable = true;
            AppLog.Information("Desktop toasts registered");
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            AppLog.Warning(ex, "Desktop toasts unavailable, using in-app fallback");
        }
    }

    /// <summary>Unregisters from the OS notification platform. Never throws.</summary>
    public void Shutdown()
    {
        try
        {
            AppNotificationManager.Default.Unregister();
        }
        catch { }
        IsAvailable = false;
    }

    /// <summary>
    /// Shows the "minimized to tray — click to reopen" toast. Returns whether
    /// the OS accepted it.
    /// </summary>
    public bool TryShowMinimized()
    {
        var loc = LocalizationService.Current;
        return TryShow("tray", loc.GetString("TrayMinTitle"), loc.GetString("TrayMinBody"));
    }

    private bool TryShow(string tag, string title, string body)
    {
        if (!IsAvailable)
            return false;
        try
        {
            AppNotificationManager.Default.Show(BuildNotification(tag, title, body));
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "Desktop toast '{Tag}' failed, using in-app fallback", tag);
            return false;
        }
    }

    /// <summary>
    /// Builds the notification payload (pure construction, no OS calls):
    /// two text lines plus the reopen action. Same tag replaces previous
    /// toasts instead of stacking them.
    /// </summary>
    public static AppNotification BuildNotification(string tag, string title, string body) =>
        new AppNotificationBuilder()
            .AddText(title)
            .AddText(body)
            .AddArgument("action", OpenAction)
            .SetTag(tag)
            .BuildNotification();

    private void OnNotificationInvoked(object sender, object args)
    {
        try
        {
            ActivationRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Toast activation handling failed");
        }
    }
}
