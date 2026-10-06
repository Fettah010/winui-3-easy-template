using System;
using System.Collections.Generic;
using System.Linq;

namespace DevTemWinUi3.Services.Data;

/// <summary>
/// One sample catalog card. The content-grid and data-grid item templates seed
/// from <see cref="SampleDataService"/> shapes like this; replace with your entity.
/// </summary>
public sealed record SampleCard(string Title, string Detail);

/// <summary>
/// Sample-data service (pages + data plan P1): deterministic seed + pure
/// search/sort/page helpers for the grid and content-grid templates. Ships
/// unconditionally (like <see cref="IRepository{T}"/> + InMemoryRepository) so
/// <c>!database</c> scaffolds compile it; it never touches
/// <see cref="DatabaseService"/> - point it at your store when a real table
/// arrives, or delete it with <c>remove-sample-content.ps1</c>.
/// All members are pure and headless-tested; never throws on null input.
/// </summary>
public static class SampleDataService
{
    /// <summary>Deterministic 12-card seed (mirrors the content-grid template).</summary>
    public static IReadOnlyList<SampleCard> GetSampleCards() => new List<SampleCard>
    {
        new("Alpine notebook", "Ruled pages, lays flat."),
        new("Basalt mug", "Keeps coffee hot."),
        new("Cedar pencil set", "Twelve soft leads."),
        new("Dune desk lamp", "Warm 2700K light."),
        new("Ember keycaps", "PBT, muted legends."),
        new("Fern mouse pad", "Stitched edges."),
        new("Granite stand", "Holds a laptop high."),
        new("Harbor backpack", "Waxed canvas."),
        new("Ion cable kit", "Braided USB-C set."),
        new("Juniper plant pot", "Glazed stoneware."),
        new("Kite webcam cover", "Slides shut."),
        new("Lagoon water bottle", "Insulated steel."),
    };

    /// <summary>
    /// Narrows <paramref name="cards"/> to those containing <paramref name="query"/>
    /// in the title or detail (case-insensitive, trimmed; null/empty returns all).
    /// </summary>
    public static IReadOnlyList<SampleCard> Search(IEnumerable<SampleCard>? cards, string? query)
    {
        var list = (cards ?? Enumerable.Empty<SampleCard>()).ToList();
        var q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
            return list;
        return list.Where(card =>
            card.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            card.Detail.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Orders <paramref name="cards"/> by Title or Detail. Unknown properties throw
    /// (a stale sort Tag fails loudly instead of silently not sorting - same rule
    /// as the grid templates' ApplySort).
    /// </summary>
    public static IReadOnlyList<SampleCard> Sort(IEnumerable<SampleCard>? cards, string propertyName, bool ascending)
    {
        var property = typeof(SampleCard).GetProperty(propertyName);
        if (property is null)
            throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "No such SampleCard property.");
        var list = (cards ?? Enumerable.Empty<SampleCard>()).ToList();
        return ascending
            ? list.OrderBy(card => property.GetValue(card)).ToList()
            : list.OrderByDescending(card => property.GetValue(card)).ToList();
    }

    /// <summary>
    /// Takes one <paramref name="pageSize"/> page at <paramref name="pageIndex"/>.
    /// Out-of-range indexes clamp to the nearest valid page (never throws);
    /// non-positive page sizes return the whole list.
    /// </summary>
    public static IReadOnlyList<SampleCard> Page(IReadOnlyList<SampleCard>? cards, int pageIndex, int pageSize)
    {
        var list = cards ?? (IReadOnlyList<SampleCard>)Array.Empty<SampleCard>();
        if (pageSize <= 0 || list.Count == 0)
            return list;
        var pageCount = Math.Max(1, (list.Count + pageSize - 1) / pageSize);
        var clamped = Math.Clamp(pageIndex, 0, pageCount - 1);
        return list.Skip(clamped * pageSize).Take(pageSize).ToList();
    }

    /// <summary>Number of <paramref name="pageSize"/> pages for <paramref name="count"/> rows.</summary>
    public static int PageCount(int count, int pageSize) =>
        pageSize <= 0 ? 1 : Math.Max(1, (Math.Max(0, count) + pageSize - 1) / pageSize);
}
