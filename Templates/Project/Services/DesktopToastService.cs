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

    /// <summary>
    /// Raised when the user clicks a toast that carries a deep-link route:
    /// the tag parsed from the toast's <c>route</c> argument (e.g. a settings
    /// toast lands on Settings). Toast-click and <c>devtem://</c> launches
    /// land identically through this event.
    /// </summary>
    public event EventHandler<string>? RouteActivationRequested;

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

    /// <summary>
    /// Shows a toast that deep-links on click: <paramref name="route"/> is a
    /// navigation tag (or full <c>devtem://</c> URI) delivered back through
    /// <see cref="RouteActivationRequested"/>. Returns whether the OS
    /// accepted it. Never throws.
    /// </summary>
    public bool TryShowRouted(string tag, string title, string body, string? route)
    {
        try
        {
            return TryShow(tag, title, body, route);
        }
        catch
        {
            return false;
        }
    }

    private bool TryShow(string tag, string title, string body, string? route = null)
    {
        if (!IsAvailable)
            return false;
        try
        {
            AppNotificationManager.Default.Show(BuildNotification(tag, title, body, route));
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
    /// toasts instead of stacking them. <paramref name="route"/> (a nav tag
    /// or deep-link URI) rides as an argument and comes back through
    /// <see cref="RouteActivationRequested"/> on click.
    /// </summary>
    public static AppNotification BuildNotification(string tag, string title, string body, string? route = null)
    {
        var builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(body)
            .AddArgument("action", OpenAction)
            .SetTag(tag);
        if (!string.IsNullOrWhiteSpace(route))
            builder.AddArgument("route", route);
        return builder.BuildNotification();
    }

    /// <summary>
    /// Extracts the navigation tag from toast activation arguments (pure,
    /// headless-testable): a <c>route</c> entry (full URI or plain tag)
    /// parses through <see cref="ProtocolService.TryParseRoute"/>.
    /// Returns false when there is no route. Never throws.
    /// </summary>
    internal static bool TryExtractRoute(object? argument, out string tag)
    {
        tag = string.Empty;
        try
        {
            if (argument is System.Collections.Generic.IDictionary<string, object> args &&
                args.TryGetValue("route", out object? value) &&
                value is string route &&
                ProtocolService.TryParseRoute(route, out tag))
            {
                return true;
            }
            if (argument is string direct &&
                ProtocolService.TryParseRoute(direct, out tag))
            {
                return true;
            }
            return false;
        }
        catch
        {
            tag = string.Empty;
            return false;
        }
    }

    private void OnNotificationInvoked(object sender, object args)
    {
        try
        {
            // Routed toast: same landing as a deep-link launch, plus the
            // window comes forward (the plain event only shows it).
            try
            {
                if (ResolveActivation(args, out string tag) &&
                    !string.IsNullOrWhiteSpace(tag))
                {
                    RouteActivationRequested?.Invoke(this, tag);
                    return;
                }
            }
            catch { }
            ActivationRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Toast activation handling failed");
        }
    }

    /// <summary>
    /// Decides which activation a toast click delivers (pure,
    /// headless-testable): a <c>route</c> argument wins (routed activation),
    /// anything else falls back to the plain event. Never throws.
    /// </summary>
    internal static bool ResolveActivation(object? argument, out string tag)
    {
        tag = string.Empty;
        try
        {
            object? payload = argument is AppNotificationActivatedEventArgs activated
                ? activated.Argument
                : argument;
            return TryExtractRoute(payload, out tag);
        }
        catch
        {
            tag = string.Empty;
            return false;
        }
    }
}
