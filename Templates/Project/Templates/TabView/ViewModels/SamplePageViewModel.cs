using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.ViewModels;

/// <summary>
/// One document tab. Top-level (not nested in the ViewModel) so
/// compiled XAML bindings can name it via <c>x:DataType</c> — mistyped tab
/// paths then fail the build instead of rendering blank at runtime.
/// </summary>
public sealed record SampleTabItem(string Title, string Detail);

/// <summary>
/// ViewModel for SamplePage (tabbed). Transient: register with
/// services.AddTransient&lt;SamplePageViewModel&gt;() in
/// ServiceLocator.Initialize() and resolve it from the container in the
/// page constructor (never new). Keep the constructor side-effect free so
/// unit tests can construct the VM directly. The seeded tabs are
/// placeholders — replace <see cref="LoadSampleTabs"/> with your data
/// source and delete the seed. Tabs are in-page document tabs: the rail
/// still owns navigation, so this page registers exactly one route.
/// </summary>
public partial class SamplePageViewModel : ObservableObject
{
    public const string Route = "sample";

    /// <summary>
    /// The page's nav icon (chosen at scaffold time via --icon). Used for
    /// the NavigationViewItem's SymbolIcon; data-bound nav hosts can bind it.
    /// </summary>
    public Symbol NavSymbol => Symbol.TemplateIcon;

    /// <summary>Open document tabs. The last tab cannot close.</summary>
    public ObservableCollection<SampleTabItem> Tabs { get; } = new();

    [ObservableProperty]
    private int _selectedIndex;

    /// <summary>Whether any tab may close (false with a single tab).</summary>
    public bool CanCloseTabs => Tabs.Count > 1;

    public SamplePageViewModel()
    {
        LoadSampleTabs();
        Tabs.CollectionChanged += OnTabsChanged;
        SelectedIndex = 0;
    }

    /// <summary>Placeholder tabs. Replace with your data source.</summary>
    private void LoadSampleTabs()
    {
        Tabs.Add(new SampleTabItem("First tab", "Details for the first tab."));
        Tabs.Add(new SampleTabItem("Second tab", "Details for the second tab."));
        Tabs.Add(new SampleTabItem("Third tab", "Details for the third tab."));
    }

    /// <summary>Opens a new tab and selects it.</summary>
    [RelayCommand]
    private void AddTab()
    {
        Tabs.Add(new SampleTabItem("New tab " + (Tabs.Count + 1), "Replace this placeholder with your tab content."));
        SelectedIndex = Tabs.Count - 1;
    }

    /// <summary>
    /// Closes a tab and clamps the selection. Unknown tabs and the last
    /// remaining tab are refused (the surface never goes blank). Never throws.
    /// </summary>
    public void CloseTab(SampleTabItem tab)
    {
        try
        {
            CloseTabAt(Tabs.IndexOf(tab));
        }
        catch { }
    }

    /// <summary>
    /// Index-based close for the TabView event wiring
    /// (<c>TabItems.IndexOf</c>). Out-of-range indexes and the last remaining
    /// tab are refused. Never throws.
    /// </summary>
    public void CloseTabAt(int index)
    {
        try
        {
            if (index < 0 || index >= Tabs.Count || Tabs.Count <= 1)
                return;
            Tabs.RemoveAt(index);
            if (SelectedIndex >= Tabs.Count)
                SelectedIndex = Tabs.Count - 1;
        }
        catch { }
    }

    private void OnTabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanCloseTabs));
    }
}
