using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using DevTemWinUi3.Controls;
using Serilog;

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

    public static NotificationService Current { get; } = new();

    private NotificationService() { }

    public void Initialize(Panel host)
    {
        _host = host;
        Log.Information("Notification service initialized");
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
    /// </summary>
    public async Task Show(string title, string message, NotificationType type, int durationMs = 4000)
    {
        if (_host is null) return;

        var card = new NotificationCard { Notification = new NotificationItem(title, message, type) };
        card.DismissRequested += (_, _) => DismissCard(card);
        // Newest on top: the bottom-anchored stack grows upward.
        _host.Children.Insert(0, card);

        // Slide in
        AnimateSlideIn(card);

        // Auto-dismiss
        await Task.Delay(durationMs);
        DismissCard(card);
    }

    public void DismissCard(FrameworkElement card)
    {
        if (_host is null || !_host.Children.Contains(card)) return;

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
        sb.Completed += (_, _) => _host.Children.Remove(card);
        sb.Begin();
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
