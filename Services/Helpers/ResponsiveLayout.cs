using System;

namespace DevTemWinUi3.Services;

/// <summary>
/// Single source of truth for window sizing and the page-level responsive
/// breakpoint. The nav pane is an overlay (LeftCompact) and never resizes the
/// content, so no breakpoint tracks it.
/// </summary>
public static class ResponsiveLayout
{
    /// <summary>Minimum window width in effective (DIP) pixels.</summary>
    public const int MinWindowWidth = 720;

    /// <summary>Minimum window height in effective (DIP) pixels.</summary>
    public const int MinWindowHeight = 540;

    /// <summary>
    /// Page (frame) widths below this threshold use the single-column layout.
    /// A frame this narrow cannot fit two comfortable columns plus padding,
    /// so pages stack instead of squeezing (which is what used to clip
    /// longer translations off the right edge).
    /// </summary>
    public const double NarrowPageThreshold = 700;

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
