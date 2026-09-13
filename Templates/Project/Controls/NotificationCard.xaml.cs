using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Controls;

/// <summary>
/// Toast card visuals for <see cref="NotificationService"/>. Layout lives
/// here (XAML); the service owns lifetime and motion. Title/message bind
/// once (toasts are immutable); the type-driven icon disc is applied in
/// code when <see cref="Notification"/> is set.
/// </summary>
public sealed partial class NotificationCard : UserControl
{
    public static readonly DependencyProperty NotificationProperty =
        DependencyProperty.Register(
            nameof(Notification),
            typeof(NotificationItem),
            typeof(NotificationCard),
            new PropertyMetadata(null, OnNotificationChanged));

    public NotificationItem? Notification
    {
        get => (NotificationItem?)GetValue(NotificationProperty);
        set => SetValue(NotificationProperty, value);
    }

    /// <summary>Raised when the dismiss (×) button is clicked.</summary>
    public event EventHandler? DismissRequested;

    public NotificationCard()
    {
        this.InitializeComponent();
    }

    private static void OnNotificationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NotificationCard card && e.NewValue is NotificationItem item)
            card.ApplyTypeVisuals(item.Type);
    }

    private void ApplyTypeVisuals(NotificationType type)
    {
        var (glyph, accentKey, accentFallback) = type switch
        {
            NotificationType.Success => ("\uE73E", "SystemFillColorSuccessBrush", Colors.MediumSeaGreen),
            NotificationType.Warning => ("\uE7BA", "SystemFillColorCautionBrush", Colors.Goldenrod),
            NotificationType.Error => ("\uEA39", "SystemFillColorCriticalBrush", Colors.Firebrick),
            _ => ("\uE946", "AccentFillColorDefaultBrush", Colors.DodgerBlue),
        };

        IconBlock.Glyph = glyph;
        IconDisc.Background = ThemeBrush(accentKey, new SolidColorBrush(accentFallback));
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

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        DismissRequested?.Invoke(this, EventArgs.Empty);
}
