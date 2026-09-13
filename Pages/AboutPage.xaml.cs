using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class AboutPage : Page, INavigationAware
{
    public string AppVersion { get; }

    public AboutPage()
    {
        this.InitializeComponent();
        AppVersion = AppInfo.Current.Version;
    }

    public void OnNavigatedTo(object? parameter)
    {
        // The page is cached but every label is a live loc binding now —
        // nothing to refresh on return.
        UpdateResponsiveLayout();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to tear down on leave.
    }

    private void AboutPage_Loaded(object sender, RoutedEventArgs e) => UpdateResponsiveLayout();

    private void AboutPage_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateResponsiveLayout();

    /// <summary>
    /// Switches between two-column and stacked layouts based on the page's own
    /// width (not the window's, so the compact nav pane is accounted for).
    /// Done in code because VisualState setters cannot reliably retarget Grid
    /// columns/rows. Stacking zeroes the unused column and reuses the other
    /// one as Star (never ColumnSpan: spanned children join Auto sizing with
    /// unbounded measure and blow the grid past the card). Layout depends only
    /// on width — never on language — so longer translations wrap instead of
    /// shifting content.
    /// </summary>
    private void UpdateResponsiveLayout()
    {
        bool narrow = ResponsiveLayout.ShouldUseNarrowPage(ActualWidth);

        ContentPanel.Padding = narrow
            ? new Thickness(16, 16, 16, 24)
            : new Thickness(32, 24, 32, 32);

        HeroMainCol.Width = narrow ? new GridLength(1, GridUnitType.Star) : new GridLength(2, GridUnitType.Star);
        HeroSideCol.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(GlanceCard, narrow ? 1 : 0);
        Grid.SetColumn(GlanceCard, narrow ? 0 : 1);

        AppCol.Width = new GridLength(1, GridUnitType.Star);
        TechCol.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(TechPanel, narrow ? 1 : 0);
        Grid.SetColumn(TechPanel, narrow ? 0 : 1);

        LicenseCol.Width = new GridLength(1, GridUnitType.Star);
        LinksCol.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(LinksPanel, narrow ? 1 : 0);
        Grid.SetColumn(LinksPanel, narrow ? 0 : 1);
    }
}
