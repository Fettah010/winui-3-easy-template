using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Controls;

namespace DevTemWinUi3.Services;

public enum NotificationType { Info, Success, Warning, Error }

/// <summary>
/// One toast: immutable data. Layout lives in <c>Controls/NotificationCard</c>.
/// </summary>
public sealed class NotificationItem
{
    public NotificationItem(string title, string message, NotificationType type)
    {
        Title = title;
        Message = message;
        Type = type;
    }

    public string Title { get; }
    public string Message { get; }
    public NotificationType Type { get; }
}

/// <summary>
/// Small animated in-app toasts (bottom-right card host), styled like modern
/// WinUI 3 / CommunityToolkit toasts: theme-aware card, tinted icon disc,
/// title + wrapping message, dismiss button, slide-and-fade motion. Card
/// layout lives in <see cref="NotificationCard"/>; this class owns host
/// lifetime and motion. For events the user must see while the window is
/// hidden, use <see cref="DesktopToastService"/> (OS Action Center) instead.
/// </summary>
public sealed class NotificationService
{
    private Panel? _host;
    private readonly Queue<(NotificationItem Item, int DurationMs)> _pending = new();
    private readonly Dictionary<FrameworkElement, CancellationTokenSource> _lifetimes = new();

    public static NotificationService Current { get; } = new();

    private NotificationService() { }

    public void Initialize(Panel host)
    {
        _host = host;
        AppLog.Information("Notification service initialized");
    }

    public void Info(string title, string message, int durationMs = 4000)
        => _ = Show(title, message, NotificationType.Info, durationMs);

    public void Success(string title, string message, int durationMs = 3000)
        => _ = Show(title, message, NotificationType.Success, durationMs);

    public void Warning(string title, string message, int durationMs = 5000)
        => _ = Show(title, message, NotificationType.Warning, durationMs);

    public void Error(string title, string message, int durationMs = 6000)
        => _ = Show(title, message, NotificationType.Error, durationMs);

    /// <summary>
    /// Shows a toast card. Returns a Task (fire-and-forget via the helpers
    /// above) so a faulted toast can never crash the process — <c>async
    /// void</c> would rethrow on the sync context. Unobserved failures are
    /// logged by the handler in <c>Program</c>.
    /// P1-2: bursts are bounded — at most <c>ToastPolicy.MaxVisible</c>
    /// cards show at once, the rest queue (oldest dropped past the cap).
    /// Must be called on the UI thread (host children are thread-affine).
    /// </summary>
    public Task Show(string title, string message, NotificationType type, int durationMs = 4000)
    {
        var host = _host;
        if (host is null)
        {
            // Silent drops are undebuggable (the update flow reports every
            // state through here): leave a breadcrumb naming the cause.
            try { AppLog.Warning("Toast dropped (host not initialized): {Title}", title); } catch { }
            return Task.CompletedTask;
        }

        // Host children are thread-affine: background callers (update
        // checks, downloads) are marshalled instead of dying with
        // RPC_E_WRONG_THREAD in a fire-and-forget task nobody observes.
        try
        {
            var queue = host.DispatcherQueue;
            if (queue is not null && !queue.HasThreadAccess)
            {
                queue.TryEnqueue(() =>
                {
                    try { _ = Show(title, message, type, durationMs); } catch { }
                });
                return Task.CompletedTask;
            }
        }
        catch { }

        var item = new NotificationItem(title, message, type);
        if (!ToastPolicy.ShouldShowNow(host.Children.Count))
        {
            if (!ToastPolicy.ShouldQueue(_pending.Count))
                _pending.Dequeue();
            _pending.Enqueue((item, durationMs));
            return Task.CompletedTask;
        }
        return ShowCardAsync(item, durationMs);
    }

    private async Task ShowCardAsync(NotificationItem item, int durationMs)
    {
        if (_host is null) return;

        try
        {
            await ShowCardCoreAsync(item, durationMs);
        }
        catch (Exception ex)
        {
            // A fault here used to vanish into an unobserved fire-and-forget
            // task (the helpers discard the Task): no toast, no trace. Log
            // it so a broken toast pipeline names itself in Logs/.
            try { AppLog.Error(ex, "Toast failed to show: {Title}", item.Title); } catch { }
        }
    }

    private async Task ShowCardCoreAsync(NotificationItem item, int durationMs)
    {
        if (_host is null) return;
        try { AppLog.Debug("Toast showing: {Title}", item.Title); } catch { }

        var card = new NotificationCard { Notification = item };
        var lifetime = new CancellationTokenSource();
        card.DismissRequested += (_, _) =>
        {
            try { lifetime.Cancel(); } catch { }
            DismissCard(card);
        };
        _lifetimes[card] = lifetime;
        // Newest on top: the bottom-anchored stack grows upward.
        _host.Children.Insert(0, card);

        // Slide in
        AnimateSlideIn(card);

        // Auto-dismiss; manual dismissal cancels this wait (P1-2) so a
        // dismissed card never lingers in the lifetime table.
        try
        {
            await Task.Delay(durationMs, lifetime.Token);
        }
        catch (OperationCanceledException) { }
        catch { }
        try
        {
            _lifetimes.Remove(card);
            lifetime.Dispose();
        }
        catch { }
        DismissCard(card);
    }

    public void DismissCard(FrameworkElement card)
    {
        var host = _host;
        if (host is null || !host.Children.Contains(card)) return;

        try
        {
            if (_lifetimes.TryGetValue(card, out var lifetime))
            {
                _lifetimes.Remove(card);
                try { lifetime.Cancel(); } catch { }
                try { lifetime.Dispose(); } catch { }
            }
        }
        catch { }

        // Slide via RenderTransform (panel-agnostic): the old Canvas.Left
        // animation assumed a Canvas host.
        var transform = card.RenderTransform as TranslateTransform ?? new TranslateTransform();
        card.RenderTransform = transform;

        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(250)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        var slideOut = new DoubleAnimation
        {
            From = 0,
            To = 400,
            Duration = new Duration(TimeSpan.FromMilliseconds(250)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        Storyboard.SetTarget(fadeOut, card);
        Storyboard.SetTarget(slideOut, transform);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        Storyboard.SetTargetProperty(slideOut, "X");

        var sb = new Storyboard();
        sb.Children.Add(fadeOut);
        sb.Children.Add(slideOut);
        sb.Completed += (_, _) =>
        {
            try { host.Children.Remove(card); } catch { }
            PumpQueue();
        };
        sb.Begin();
    }

    /// <summary>
    /// Shows the next queued toast when a visible slot freed up. Must run
    /// on the UI thread (called from card teardown). Never throws.
    /// </summary>
    private void PumpQueue()
    {
        try
        {
            var host = _host;
            if (host is null || _pending.Count == 0)
                return;
            if (!ToastPolicy.ShouldShowNow(host.Children.Count))
                return;
            var (item, durationMs) = _pending.Dequeue();
            _ = ShowCardAsync(item, durationMs);
        }
        catch { }
    }

    private void AnimateSlideIn(FrameworkElement card)
    {
        var transform = new TranslateTransform();
        card.RenderTransform = transform;

        var fadeIn = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slideIn = new DoubleAnimation
        {
            From = 80,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(fadeIn, card);
        Storyboard.SetTarget(slideIn, transform);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        Storyboard.SetTargetProperty(slideIn, "X");

        var sb = new Storyboard();
        sb.Children.Add(fadeIn);
        sb.Children.Add(slideIn);
        sb.Begin();
    }
}
