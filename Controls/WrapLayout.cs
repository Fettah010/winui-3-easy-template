using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace DevTemWinUi3.Controls;

/// <summary>
/// Pure row-wrapping layout math behind <see cref="WrapPanel"/>.
/// Kept UI-free so the algorithm is unit-testable: given child sizes and the
/// available width, it returns each child's final rect plus the total extent.
/// </summary>
public static class WrapLayout
{
    /// <summary>
    /// Lays out <paramref name="childSizes"/> left-to-right, wrapping onto a
    /// new row whenever the next child would exceed <paramref name="maxWidth"/>.
    /// Children are vertically centered within their row.
    /// </summary>
    /// <param name="maxWidth">Available width, or +infinity for a single row.</param>
    /// <returns>One rect per child, in the same order.</returns>
    public static IReadOnlyList<Rect> Arrange(
        IReadOnlyList<Size> childSizes,
        double maxWidth,
        double horizontalSpacing,
        double verticalSpacing,
        out Size extent)
    {
        if (childSizes is null)
            throw new ArgumentNullException(nameof(childSizes));
        if (double.IsNaN(maxWidth) || maxWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        if (double.IsNaN(horizontalSpacing) || horizontalSpacing < 0)
            throw new ArgumentOutOfRangeException(nameof(horizontalSpacing));
        if (double.IsNaN(verticalSpacing) || verticalSpacing < 0)
            throw new ArgumentOutOfRangeException(nameof(verticalSpacing));

        var rects = new Rect[childSizes.Count];
        if (childSizes.Count == 0)
        {
            extent = new Size(0, 0);
            return rects;
        }

        bool bounded = !double.IsPositiveInfinity(maxWidth);
        double y = 0;
        double rowHeight = 0;
        double rowStartX = 0;
        int rowStart = 0;
        double extentWidth = 0;

        for (int i = 0; i < childSizes.Count; i++)
        {
            var size = childSizes[i];
            double x = rowStartX;

            // Wrap before this child if it would overflow (never wrap the
            // first child of a row, even if it alone is wider than maxWidth).
            if (bounded && i > rowStart && x + size.Width > maxWidth)
            {
                extentWidth = Math.Max(extentWidth, x - horizontalSpacing);
                y += rowHeight + verticalSpacing;
                rowStart = i;
                rowStartX = 0;
                rowHeight = 0;
                x = 0;
            }

            rects[i] = new Rect(x, y, size.Width, size.Height);
            rowStartX = x + size.Width + horizontalSpacing;
            rowHeight = Math.Max(rowHeight, size.Height);
        }

        extentWidth = Math.Max(extentWidth, rowStartX - horizontalSpacing);
        double extentHeight = y + rowHeight;
        extent = new Size(bounded ? Math.Min(extentWidth, maxWidth) : extentWidth, extentHeight);

        // Vertically center children within their row.
        CenterRows(rects, childSizes);
        return rects;
    }

    private static void CenterRows(Rect[] rects, IReadOnlyList<Size> sizes)
    {
        int i = 0;
        while (i < rects.Length)
        {
            int j = i;
            double rowY = rects[i].Y;
            double rowH = 0;
            while (j < rects.Length && rects[j].Y == rowY)
            {
                rowH = Math.Max(rowH, sizes[j].Height);
                j++;
            }
            for (int k = i; k < j; k++)
            {
                var r = rects[k];
                rects[k] = new Rect(r.X, rowY + (rowH - sizes[k].Height) / 2, r.Width, r.Height);
            }
            i = j;
        }
    }
}
