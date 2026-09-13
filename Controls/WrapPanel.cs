using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DevTemWinUi3.Controls;

/// <summary>
/// A minimal horizontal wrap panel: children flow left-to-right and wrap onto
/// new rows instead of overflowing (and getting clipped). Used for button rows
/// so longer translations can never push content off-screen.
/// </summary>
public sealed class WrapPanel : Panel
{
    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(WrapPanel),
            new PropertyMetadata(0.0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(WrapPanel),
            new PropertyMetadata(0.0, OnLayoutPropertyChanged));

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WrapPanel panel)
            panel.InvalidateMeasure();
    }

    private IReadOnlyList<Rect>? _lastRects;

    protected override Size MeasureOverride(Size availableSize)
    {
        var sizes = new Size[Children.Count];
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            sizes[i] = Children[i].DesiredSize;
        }

        double maxWidth = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : availableSize.Width;

        _lastRects = WrapLayout.Arrange(sizes, maxWidth, HorizontalSpacing, VerticalSpacing, out var extent);

        double width = double.IsInfinity(availableSize.Width) ? extent.Width : availableSize.Width;
        return new Size(width, extent.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rects = _lastRects;
        if (rects is null || rects.Count != Children.Count)
        {
            var sizes = new Size[Children.Count];
            for (int i = 0; i < Children.Count; i++)
                sizes[i] = Children[i].DesiredSize;
            rects = WrapLayout.Arrange(sizes, finalSize.Width, HorizontalSpacing, VerticalSpacing, out _);
        }

        for (int i = 0; i < Children.Count; i++)
            Children[i].Arrange(rects[i]);

        return finalSize;
    }
}
