using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// Single source of truth for window sizing and responsive-layout breakpoints.
/// The XAML <c>AdaptiveTrigger</c> thresholds and the code-behind pane-switching
/// both derive from <see cref="CompactLayoutThreshold"/> so they stay in sync.
/// </summary>
public static class ResponsiveLayout
{
    /// <summary>Minimum window width in effective (DIP) pixels.</summary>
    public const int MinWindowWidth = 720;

    /// <summary>Minimum window height in effective (DIP) pixels.</summary>
    public const int MinWindowHeight = 540;

    /// <summary>
    /// Window widths below this threshold use the compact navigation pane and
    /// the single-column page layouts. Must match the page code-behind usage
    /// of <see cref="NarrowPageThreshold"/> so pane and pages switch together.
    /// </summary>
    public const double CompactLayoutThreshold = 860;

    /// <summary>
    /// Page (frame) widths below this threshold use the single-column layout.
    /// A frame this narrow cannot fit two comfortable columns plus padding,
    /// so pages stack instead of squeezing (which is what used to clip
    /// longer translations off the right edge).
    /// </summary>
    public const double NarrowPageThreshold = 700;

    /// <summary>
    /// Whether the given window width should use the compact navigation pane
    /// (and, by convention, the narrow single-column page layouts).
    /// </summary>
    public static bool ShouldUseCompactPane(double windowWidth) =>
        !double.IsNaN(windowWidth) && windowWidth > 0 && windowWidth < CompactLayoutThreshold;

    /// <summary>
    /// Whether the given page width should use the stacked single-column layout.
    /// Driven by the page's own width (not the window's), so it stays correct
    /// regardless of pane mode.
    /// </summary>
    public static bool ShouldUseNarrowPage(double pageWidth) =>
        !double.IsNaN(pageWidth) && pageWidth > 0 && pageWidth < NarrowPageThreshold;

    /// <summary>
    /// Converts effective pixels (DIPs) to physical pixels for the given DPI.
    /// Used for <c>WM_GETMINMAXINFO</c> track sizes, which are in physical pixels.
    /// </summary>
    public static int ScaleLogicalToPhysical(int logicalPixels, uint dpi)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(logicalPixels);
        ArgumentOutOfRangeException.ThrowIfZero(dpi);
        return (int)Math.Round(logicalPixels * dpi / 96.0);
    }
}
