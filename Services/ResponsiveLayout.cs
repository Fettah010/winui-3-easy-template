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
    /// the single-column page layouts. Must match the <c>AdaptiveTrigger
    /// MinWindowWidth</c> values in Home/About/Settings pages.
    /// </summary>
    public const double CompactLayoutThreshold = 860;

    /// <summary>
    /// Whether the given window width should use the compact navigation pane
    /// (and, by convention, the narrow single-column page layouts).
    /// </summary>
    public static bool ShouldUseCompactPane(double windowWidth) =>
        !double.IsNaN(windowWidth) && windowWidth > 0 && windowWidth < CompactLayoutThreshold;

    /// <summary>
    /// Converts effective pixels (DIPs) to physical pixels for the given DPI.
    /// Used for <c>WM_GETMINMAXINFO</c> track sizes, which are in physical pixels.
    /// </summary>
    public static int ScaleLogicalToPhysical(int logicalPixels, uint dpi)
    {
        if (logicalPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(logicalPixels));
        if (dpi == 0)
            throw new ArgumentOutOfRangeException(nameof(dpi));
        return (int)Math.Round(logicalPixels * dpi / 96.0);
    }
}
