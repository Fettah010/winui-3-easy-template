using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

/// <summary>
/// Advancement C2: the single nav contract. Item construction
/// (<c>NavEntry.CreateItem</c>) is UI-bound and stays covered by the build +
/// the scaffold matrix; these tests pin the registry itself — partition,
/// uniqueness, and label coverage — headlessly.
/// </summary>
[TestClass]
public class NavigationRegistryTests
{
    [TestMethod]
    public void Registry_HasMenuAndFooterSections()
    {
        // Relational across scaffold modes: flag-dropped entries vanish
        // with their pages (matrix runs every combo).
        Assert.IsGreaterThanOrEqualTo(1, NavigationRegistry.MenuEntries.Count);
        int expectedFooter = AppFeatures.Health ? 2 : 1;
        Assert.HasCount(expectedFooter, NavigationRegistry.FooterEntries);
        Assert.AreEqual("home", NavigationRegistry.MenuEntries[0].Route);
        Assert.AreEqual(AppFeatures.Health, NavigationRegistry.FindByRoute("diagnostics") is not null);
    }

    [TestMethod]
    public void Registry_RoutesAreUnique()
    {
        var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in NavigationRegistry.MenuEntries)
            Assert.IsTrue(seen.Add(entry.Route), "Duplicate menu route: " + entry.Route);
        foreach (var entry in NavigationRegistry.FooterEntries)
            Assert.IsTrue(seen.Add(entry.Route), "Duplicate footer route: " + entry.Route);
    }

    [TestMethod]
    public void Registry_StockEntriesHaveIconsAndIds()
    {
        foreach (var entry in NavigationRegistry.MenuEntries)
            AssertStockEntry(entry);
        foreach (var entry in NavigationRegistry.FooterEntries)
            AssertStockEntry(entry);
    }

    [TestMethod]
    public void FindByRoute_MatchesCaseInsensitively()
    {
        var home = NavigationRegistry.FindByRoute("HOME");
        Assert.IsNotNull(home);
        Assert.AreEqual("home", home!.Route);
        Assert.IsFalse(home.IsFooter);
        Assert.IsNull(NavigationRegistry.FindByRoute("nope"));
        Assert.IsNull(NavigationRegistry.FindByRoute(null));
    }

    [TestMethod]
    public void Registry_LabelsResolveInEveryLanguage()
    {
        var loc = LocalizationService.Current;
        foreach (string lang in new[] { "en-US", "es-ES", "fr-FR" })
        {
            loc.SetLanguage(lang);
            NavigationRegistry.RefreshLabels();
            foreach (var entry in NavigationRegistry.MenuEntries)
                AssertLabel(entry, lang);
            foreach (var entry in NavigationRegistry.FooterEntries)
                AssertLabel(entry, lang);
        }
        loc.SetLanguage("en-US");
    }

    private static void AssertStockEntry(NavEntry entry)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Route), "Entry with blank route.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(entry.LocKey), entry.Route + ": blank loc key.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(entry.AutomationId), entry.Route + ": blank automation id.");
        Assert.AreNotEqual(!entry.IconSymbol.HasValue, string.IsNullOrEmpty(entry.IconGlyph),
            entry.Route + ": exactly one of Symbol/Glyph must be set.");
    }

    private static void AssertLabel(NavEntry entry, string lang)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Label), lang + ": " + entry.Route + " label blank.");
        Assert.AreNotEqual(entry.LocKey, entry.Label, lang + ": " + entry.Route + " label untranslated.");
    }
}
