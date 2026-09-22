using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace DevTemWinUi3.Services;

/// <summary>
/// One navigation destination in the rail/drawer.
/// </summary>
public sealed class NavEntry : INotifyPropertyChanged
{
    public NavEntry(string route, string locKey, Symbol? symbol, string? glyph, bool isFooter, string automationId)
    {
        Route = route;
        LocKey = locKey;
        IconSymbol = symbol;
        IconGlyph = glyph;
        IsFooter = isFooter;
        AutomationId = automationId;
        _label = locKey;
    }

    public string Route { get; }
    public string LocKey { get; }
    public Symbol? IconSymbol { get; }
    public string? IconGlyph { get; }
    public bool IsFooter { get; }
    public string AutomationId { get; }

    private string _label;

    /// <summary>Display text, refreshed on language switch.</summary>
    public string Label
    {
        get => _label;
        private set
        {
            if (!string.Equals(_label, value, StringComparison.Ordinal))
            {
                _label = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void RefreshLabel()
    {
        try { Label = LocalizationService.Current.GetString(LocKey); }
        catch { }
    }

    /// <summary>
    /// Builds the rail item: tag = route (selection sync + invoke match on
    /// it), content bound live to <see cref="Label"/>, Symbol or glyph icon.
    /// UI thread only. Never throws (null on failure).
    /// </summary>
    internal NavigationViewItem? CreateItem()
    {
        try
        {
            var item = new NavigationViewItem();
            try { AutomationProperties.SetAutomationId(item, AutomationId); } catch { }
            item.Tag = Route;
            try
            {
                item.SetBinding(
                    ContentControl.ContentProperty,
                    new Binding { Path = new PropertyPath(nameof(Label)), Source = this, Mode = BindingMode.OneWay });
            }
            catch { item.Content = Label; }
            try
            {
                if (IconSymbol.HasValue)
                    item.Icon = new SymbolIcon(IconSymbol.Value);
                else if (!string.IsNullOrEmpty(IconGlyph))
                    item.Icon = new FontIcon { Glyph = IconGlyph };
            }
            catch { }
            return item;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// The single nav contract (Advancement C2): one list of
/// (route, loc-key, icon, footer) that the shell builds its items from and
/// <c>add-page.ps1</c> appends to. Restyles cannot break it — there is no
/// XAML anchor, only the <c>// &lt;devtem:nav-entries&gt;</c> marker below
/// (C# moves, never a styling casualty). Order = display order within each
/// section. The built-in Settings item stays outside the registry (the
/// NavigationView owns it; <c>MainWindow</c> keeps its explicit sync
/// branch). Labels refresh on language switch via
/// <see cref="RefreshLabels"/>. Never throws.
/// </summary>
public static class NavigationRegistry
{
    private static readonly List<NavEntry> _entries = new();
    private static readonly List<NavEntry> _menu = new();
    private static readonly List<NavEntry> _footer = new();
    private static readonly object _gate = new();

    static NavigationRegistry()
    {
        try
        {
            _entries.Add(new NavEntry("home", "NavHome", Symbol.Home, null, false, "NavHomeItem"));
            _entries.Add(new NavEntry("about", "NavAbout", null, "\uE946", true, "NavAboutItem"));
#if (health)
            _entries.Add(new NavEntry("diagnostics", "NavDiagnostics", Symbol.Repair, null, true, "NavDiagnosticsItem"));
#endif
            // <devtem:nav-entries>
            // Application-owned entries are appended here by
            // Scripts/add-page.ps1 (one line per page, order = display
            // order). Keep this marker exactly once.
            // </devtem:nav-entries>
            RebuildSections();
            RefreshLabels();
        }
        catch { }
    }

    /// <summary>Rail entries, in display order.</summary>
    public static IReadOnlyList<NavEntry> MenuEntries
    {
        get { lock (_gate) { return _menu.ToArray(); } }
    }

    /// <summary>Footer entries, in display order.</summary>
    public static IReadOnlyList<NavEntry> FooterEntries
    {
        get { lock (_gate) { return _footer.ToArray(); } }
    }

    /// <summary>Finds an entry by route (selection sync). Never throws.</summary>
    public static NavEntry? FindByRoute(string? route)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(route))
                return null;
            lock (_gate)
            {
                foreach (var entry in _entries)
                {
                    if (string.Equals(entry.Route, route, StringComparison.OrdinalIgnoreCase))
                        return entry;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Re-reads every label in the current language. Never throws.</summary>
    public static void RefreshLabels()
    {
        try
        {
            lock (_gate)
            {
                foreach (var entry in _entries)
                    entry.RefreshLabel();
            }
        }
        catch { }
    }

    private static void RebuildSections()
    {
        _menu.Clear();
        _footer.Clear();
        foreach (var entry in _entries)
        {
            if (entry.IsFooter)
                _footer.Add(entry);
            else
                _menu.Add(entry);
        }
    }
}
