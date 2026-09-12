using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Serilog;

namespace DevTemWinUi3.Services;

public enum NotificationType { Info, Success, Warning, Error }

/// <summary>
/// Small animated in-app toasts (bottom-right card host), styled like modern
/// WinUI 3 / CommunityToolkit toasts: theme-aware card brushes, tinted icon
/// disc, title + wrapping message, dismiss button, slide-and-fade motion.
/// For events the user must see while the window is hidden, use
/// <see cref="DesktopToastService"/> (OS Action Center) instead.
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
        => Show(title, message, NotificationType.Info, durationMs);

    public void Success(string title, string message, int durationMs = 3000)
        => Show(title, message, NotificationType.Success, durationMs);

    public void Warning(string title, string message, int durationMs = 5000)
        => Show(title, message, NotificationType.Warning, durationMs);

    public void Error(string title, string message, int durationMs = 6000)
        => Show(title, message, NotificationType.Error, durationMs);

    public async void Show(string title, string message, NotificationType type, int durationMs = 4000)
    {
        if (_host is null) return;

        var card = CreateCard(title, message, type);
        // Newest on top: the bottom-anchored stack grows upward.
        _host.Children.Insert(0, card);

        // Slide in
        AnimateSlideIn(card);

        // Auto-dismiss
        await Task.Delay(durationMs);
        DismissCard(card);
    }

    public void DismissCard(Border card)
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

    private static Brush ThemeBrush(string key, Brush fallback)
    {
        try
        {
            if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
                return brush;
        }
        catch { }
        return fallback;
    }

    private Border CreateCard(string title, string message, NotificationType type)
    {
        var (icon, accentKey, accentFallback) = type switch
        {
            NotificationType.Success => ("\uE73E", "SystemFillColorSuccessBrush", (Brush)new SolidColorBrush(Colors.MediumSeaGreen)),
            NotificationType.Warning => ("\uE7BA", "SystemFillColorCautionBrush", (Brush)new SolidColorBrush(Colors.Goldenrod)),
            NotificationType.Error => ("\uEA39", "SystemFillColorCriticalBrush", (Brush)new SolidColorBrush(Colors.Firebrick)),
            _ => ("\uE946", "AccentFillColorDefaultBrush", (Brush)new SolidColorBrush(Colors.DodgerBlue)),
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = ThemeBrush("TextFillColorPrimaryBrush", new SolidColorBrush(Colors.Black)),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 300,
        };

        var msgBlock = new TextBlock
        {
            Text = message,
            FontSize = 12,
            Foreground = ThemeBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Colors.DimGray)),
            TextWrapping = TextWrapping.WrapWholeWords,
            MaxWidth = 300,
            Margin = new Thickness(0, 2, 0, 0),
        };

        var iconBlock = new FontIcon
        {
            Glyph = icon,
            FontSize = 14,
            Foreground = ThemeBrush("TextOnAccentFillColorPrimaryBrush", new SolidColorBrush(Colors.White)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var iconDisc = new Border
        {
            Background = ThemeBrush(accentKey, accentFallback),
            CornerRadius = new CornerRadius(15),
            Width = 30,
            Height = 30,
            VerticalAlignment = VerticalAlignment.Center,
            Child = iconBlock,
        };

        var closeBtn = new Button
        {
            Content = "\u2715",
            FontSize = 12,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            Foreground = ThemeBrush("TextFillColorSecondaryBrush", new SolidColorBrush(Colors.Gray)),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 0,
            MinHeight = 0,
        };
        Border? cardRef = null;
        closeBtn.Click += (_, _) => { if (cardRef is not null) DismissCard(cardRef); };

        var contentStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            MaxWidth = 300,
        };
        contentStack.Children.Add(titleBlock);
        contentStack.Children.Add(msgBlock);

        var rootGrid = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
        };

        Grid.SetColumn(iconDisc, 0);
        Grid.SetColumn(contentStack, 1);
        Grid.SetColumn(closeBtn, 2);

        rootGrid.Children.Add(iconDisc);
        rootGrid.Children.Add(contentStack);
        rootGrid.Children.Add(closeBtn);

        var card = new Border
        {
            Background = ThemeBrush("CardBackgroundFillColorDefaultBrush", new SolidColorBrush(ColorHelper.FromArgb(250, 250, 250, 250))),
            BorderBrush = ThemeBrush("CardStrokeColorDefaultBrush", new SolidColorBrush(ColorHelper.FromArgb(255, 220, 220, 220))),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 10, 12),
            Margin = new Thickness(0, 0, 16, 8),
            // Fixed width: the Canvas host measures with unbounded width, where
            // Star columns collapse and the card can measure to zero. A fixed
            // width also matches real toolkit toasts (uniform, predictable).
            Width = 360,
            Child = rootGrid,
            Opacity = 0,
        };
        cardRef = card;

        return card;
    }

    private void AnimateSlideIn(Border card)
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
