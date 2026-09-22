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
        // Scripts/add-page.ps1 -Kind list, which does every step for you).
        UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(ActualWidth));
    }

    public void OnNavigatedFrom()
    {
        // Nothing to tear down on leave.
    }

    private void SamplePage_Loaded(object sender, RoutedEventArgs e) =>
        UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(ActualWidth));

    private void SamplePage_SizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(e.NewSize.Width));

    /// <summary>
    /// Page-level responsive switch: wide shows list + details side by side,
    /// narrow stacks them (details below the list). Stacking zeroes the
    /// unused column and reuses the other one as Star (never ColumnSpan:
    /// spanned children join Auto sizing with unbounded measure and blow
    /// the grid past the card). Driven by the page's own width so the
    /// compact nav pane is accounted for. Idempotent: same breakpoint
    /// returns without touching the visual tree. Never depends on language.
    /// </summary>
    private void UpdateResponsiveLayout(bool narrow)
    {
        if (_isNarrow.HasValue && _isNarrow.Value == narrow)
            return;
        _isNarrow = narrow;

        ContentPanel.Padding = narrow
            ? new Thickness(16, 16, 16, 24)
            : new Thickness(32, 24, 32, 32);

        ListCol.Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(320);
        DetailsCol.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(SampleDetailsCard, narrow ? 1 : 0);
        Grid.SetColumn(SampleDetailsCard, narrow ? 0 : 1);
    }
}
