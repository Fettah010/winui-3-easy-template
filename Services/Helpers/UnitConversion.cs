namespace DevTemWinUi3.Services.Helpers;

/// <summary>
/// Named unit-conversion seam for overlay math (pain-log #23): PDF engines
/// disagree (Windows.Data.Pdf reports 96-DPI pixels, PdfPig word boxes live
/// in points), and mapping one into the other without a named seam shrinks
/// everything to 75% with zero errors. Convert once at the boundary through
/// here; unit tests pin A4 in both systems so regressions fail the build
/// instead of landing on empty lines.
/// </summary>
public static class UnitConversion
{
    /// <summary>Points per inch (typographic point).</summary>
    public const double PointsPerInch = 72.0;

    /// <summary>Device-independent pixels per inch (WinUI/PDF raster).</summary>
    public const double PixelsPerInch = 96.0;

    /// <summary>Points to device-independent pixels (×96/72).</summary>
    public static double PointsToPixels(double points) => points * PixelsPerInch / PointsPerInch;

    /// <summary>Device-independent pixels to points (×72/96).</summary>
    public static double PixelsToPoints(double pixels) => pixels * PointsPerInch / PixelsPerInch;

    /// <summary>Scale factor mapping a source rect onto a destination rect.</summary>
    public static double ScaleToFit(double sourceWidth, double sourceHeight, double destWidth, double destHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
            return 1.0;
        double sx = destWidth / sourceWidth;
        double sy = destHeight / sourceHeight;
        double s = sx < sy ? sx : sy;
        return double.IsFinite(s) && s > 0 ? s : 1.0;
    }
}
