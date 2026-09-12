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

public sealed class NotificationService
{
    private Panel? _host;
    private readonly Random _random = new();

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
        _host.Children.Add(card);

        // Slide in
        AnimateSlideIn(card);

        // Auto-dismiss
        await Task.Delay(durationMs);
        DismissCard(card);
    }

    public void DismissCard(Border card)
    {
        if (_host is null) return;

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
        Storyboard.SetTarget(slideOut, card);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");
        Storyboard.SetTargetProperty(slideOut, "(Canvas.Left)");

        var sb = new Storyboard();
        sb.Children.Add(fadeOut);
        sb.Children.Add(slideOut);
        sb.Completed += (_, _) => _host.Children.Remove(card);
        sb.Begin();
    }

    private Border CreateCard(string title, string message, NotificationType type)
    {
        var (icon, accentBrush) = type switch
        {
            NotificationType.Success => ("\uE73E", new SolidColorBrush(Colors.SpringGreen)),
            NotificationType.Warning => ("\uE7BA", new SolidColorBrush(Colors.Gold)),
            NotificationType.Error => ("\uEA39", new SolidColorBrush(Colors.Firebrick)),
            _ => ("\uE946", new SolidColorBrush(Colors.DodgerBlue)),
        };

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            TextWrapping = TextWrapping.NoWrap,
            MaxWidth = 280,
        };

        var msgBlock = new TextBlock
        {
            Text = message,
            FontSize = 12,
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(200, 255, 255, 255)),
            TextWrapping = TextWrapping.NoWrap,
            MaxWidth = 280,
            Margin = new Thickness(0, 2, 0, 0),
        };

        var iconBlock = new FontIcon
        {
            Glyph = icon,
            FontSize = 18,
            Foreground = new SolidColorBrush(Colors.White),
        };

        var closeBtn = new Button
        {
            Content = "\u2715",
            FontSize = 12,
            Background = new SolidColorBrush(Colors.Transparent),
            Foreground = new SolidColorBrush(ColorHelper.FromArgb(150, 255, 255, 255)),
            Padding = new Thickness(4),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 0,
            MinHeight = 0,
        };
        closeBtn.Click += (_, _) =>
        {
            var parent = closeBtn.Parent as Border;
            if (parent is not null) DismissCard(parent);
        };

        var contentStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            MaxWidth = 280,
        };
        contentStack.Children.Add(titleBlock);
        contentStack.Children.Add(msgBlock);

        var iconBorder = new Border
        {
            Background = accentBrush,
            CornerRadius = new CornerRadius(16),
            Width = 32,
            Height = 32,
            VerticalAlignment = VerticalAlignment.Center,
            Child = iconBlock,
        };

        var closeBorder = new Border
        {
            Child = closeBtn,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var rootGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto },
            },
        };

        Grid.SetColumn(iconBorder, 0);
        Grid.SetColumn(contentStack, 1);
        Grid.SetColumn(closeBorder, 2);

        rootGrid.Children.Add(iconBorder);
        rootGrid.Children.Add(contentStack);
        rootGrid.Children.Add(closeBorder);

        var card = new Border
        {
            Background = new SolidColorBrush(ColorHelper.FromArgb(230, 32, 32, 32)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 8, 12),
            Margin = new Thickness(0, 0, 16, 8),
            Child = rootGrid,
            Opacity = 0,
        };

        return card;
    }

    private void AnimateSlideIn(Border card)
    {
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
        Storyboard.SetTarget(slideIn, card);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");
        Storyboard.SetTargetProperty(slideIn, "(Canvas.Left)");

        var sb = new Storyboard();
        sb.Children.Add(fadeIn);
        sb.Children.Add(slideIn);
        sb.Begin();
    }
}
