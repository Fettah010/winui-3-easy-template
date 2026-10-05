using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class SamplePage : Page, INavigationAware
{
    public SamplePageViewModel ViewModel { get; }

    // Window resize drags fire SizeChanged on every tick: the guard below makes
    // same-breakpoint ticks a no-op (no Grid churn mid-resize). Complex pages
    // additionally coalesce ticks through LayoutDebouncer (see HomePage).
    private bool? _isNarrow;
    private bool _tabsWired;

    public SamplePage(SamplePageViewModel viewModel)
    {
        // Constructor-injected (C1): add-page.ps1 registers this route's
        // factory in ServiceLocator, so navigation builds the page with
        // the view model — never new, never resolved at click time.
        // Wire-up step 1: services.AddTransient<SamplePageViewModel>() +
        // PageFactory.Register("sample", () => new SamplePage(...)).
        ViewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
        this.InitializeComponent();
        DataContext = ViewModel;
    }

    public void OnNavigatedTo(object? parameter)
    {
        // Labels bind live ({loc:Loc} in the XAML) — nothing to refresh.
        // Wire-up step 2: paste SamplePage.strings.md into
        // Services/Localization/{En,Es,Fr}Strings.cs, then delete the
        // snippet. The generated test stub fails until you do (or run
        // Scripts/add-page.ps1, which does every step for you).
        UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(ActualWidth));
        EnsureTabsWired();
        SyncTabs();
    }

    public void OnNavigatedFrom()
    {
        // The page is gone: drop the collection + property subscriptions (no
        // cache → leak) so a detached visual tree is never touched again.
        try { ViewModel.Tabs.CollectionChanged -= OnTabsChanged; } catch { }
        try { ViewModel.PropertyChanged -= OnViewModelPropertyChanged; } catch { }
        _tabsWired = false;
    }

    private void SamplePage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(ActualWidth));
        EnsureTabsWired();
        SyncTabs();
    }

    private void SamplePage_SizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(e.NewSize.Width));

    /// <summary>
    /// Thin TabView event wiring (the MainWindow pattern): the ViewModel owns
    /// add/close/selection state, the page only forwards the control events
    /// and mirrors collection changes into TabItems. Never throws.
    /// </summary>
    private void SampleTabView_AddTabButtonClick(TabView sender, object args)
    {
        try
        {
            ViewModel.AddTabCommand.Execute(null);
            SyncTabs();
        }
        catch { }
    }

    private void SampleTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        try
        {
            int index = sender.TabItems.IndexOf(args.Tab);
            ViewModel.CloseTabAt(index);
            SyncTabs();
        }
        catch { }
    }

    private void EnsureTabsWired()
    {
        if (_tabsWired)
            return;
        _tabsWired = true;
        try { ViewModel.Tabs.CollectionChanged += OnTabsChanged; } catch { }
        try { ViewModel.PropertyChanged += OnViewModelPropertyChanged; } catch { }
    }

    private void OnTabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try { SyncTabs(); } catch { }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Closability flips with the tab count (single tab locks): refresh the
        // flags without rebuilding the items.
        if (string.Equals(e.PropertyName, nameof(SamplePageViewModel.CanCloseTabs), StringComparison.Ordinal))
        {
            try { RefreshClosability(); } catch { }
        }
    }

    /// <summary>
    /// Rebuilds TabItems from ViewModel.Tabs (TabView has no ItemsSource).
    /// Tab records are immutable, so content is set once — no per-item
    /// bindings to leak. Selection is restored from the ViewModel after the
    /// rebuild. Never throws.
    /// </summary>
    private void SyncTabs()
    {
        try
        {
            SampleTabView.TabItems.Clear();
            foreach (var tab in ViewModel.Tabs)
            {
                var body = new StackPanel { Spacing = 4, Padding = new Thickness(16) };
                body.Children.Add(new TextBlock
                {
                    Text = tab.Detail,
                    Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                    TextWrapping = TextWrapping.Wrap,
                });
                SampleTabView.TabItems.Add(new TabViewItem
                {
                    Header = tab.Title,
                    Content = body,
                    IsClosable = ViewModel.CanCloseTabs,
                });
            }
            int selected = ViewModel.SelectedIndex;
            if (selected < 0)
                selected = 0;
            if (selected >= SampleTabView.TabItems.Count)
                selected = SampleTabView.TabItems.Count - 1;
            if (selected >= 0)
                SampleTabView.SelectedIndex = selected;
        }
        catch { }
    }

    private void RefreshClosability()
    {
        bool closable = ViewModel.CanCloseTabs;
        foreach (var item in SampleTabView.TabItems)
        {
            if (item is TabViewItem tabItem)
                tabItem.IsClosable = closable;
        }
    }

    /// <summary>
    /// Page-level responsive switch (padding here; two-column pages also
    /// restack their grids). Driven by the page's own width so the compact
    /// nav pane is accounted for. Idempotent: same breakpoint returns without
    /// touching the visual tree. Never depends on language.
    /// </summary>
    private void UpdateResponsiveLayout(bool narrow)
    {
        if (_isNarrow.HasValue && _isNarrow.Value == narrow)
            return;
        _isNarrow = narrow;

        ContentPanel.Padding = narrow
            ? new Thickness(16, 16, 16, 24)
            : new Thickness(32, 24, 32, 32);
    }
}
