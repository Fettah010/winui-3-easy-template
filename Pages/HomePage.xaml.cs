using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class HomePage : Page
{
    public AppInfo AppInfo { get; } = AppInfo.Current;

    public HomePage()
    {
        this.InitializeComponent();
        ApplyLocalization();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // The page is cached: refresh strings in case the language changed
        // while the user was on another page.
        ApplyLocalization();
        UpdateResponsiveLayout();
    }

    private void HomePage_Loaded(object sender, RoutedEventArgs e) => UpdateResponsiveLayout();

    private void HomePage_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateResponsiveLayout();

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

        HeroLogoCol.Width = narrow ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        HeroTextCol.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(HeroText, narrow ? 1 : 0);
        Grid.SetColumn(HeroText, narrow ? 0 : 1);
        Grid.SetColumnSpan(HeroText, 1);

        StatusCol1.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(QuickCard, narrow ? 1 : 0);
        Grid.SetColumn(QuickCard, narrow ? 0 : 1);

        FeatureCol1.Width = narrow ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        Grid.SetRow(FeatureCard2, narrow ? 1 : 0);
        Grid.SetColumn(FeatureCard2, narrow ? 0 : 1);
        Grid.SetRow(FeatureCard3, narrow ? 2 : 1);
        Grid.SetColumn(FeatureCard3, 0);
        Grid.SetRow(FeatureCard4, narrow ? 3 : 1);
        Grid.SetColumn(FeatureCard4, narrow ? 0 : 1);
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Current;
        HomeTitleText.Text = loc.GetString("HomeTitle");
        HomeDescText.Text = loc.GetString("HomeDescription");
        CheckUpdatesQuickButton.Content = loc.GetString("HomeCheckUpdates");
        OpenSettingsQuickButton.Content = loc.GetString("HomeOpenSettings");
        ReleasesLinkButton.Content = loc.GetString("HomeReleasesLink");
        StatusTitleText.Text = loc.GetString("HomeStatusOk");
        QuickActionsText.Text = loc.GetString("HomeQuickActions");
        QuickSettingsButton.Content = loc.GetString("NavSettings");
        QuickAboutButton.Content = loc.GetString("NavAbout");
        IncludedText.Text = loc.GetString("HomeIncluded");
        FeatUpdatesTitle.Text = loc.GetString("HomeFeatUpdatesTitle");
        FeatUpdatesDesc.Text = loc.GetString("HomeFeatUpdatesDesc");
        FeatSettingsTitle.Text = loc.GetString("HomeFeatSettingsTitle");
        FeatSettingsDesc.Text = loc.GetString("HomeFeatSettingsDesc");
        FeatDiagTitle.Text = loc.GetString("HomeFeatDiagnosticsTitle");
        FeatDiagDesc.Text = loc.GetString("HomeFeatDiagnosticsDesc");
        FeatTrayTitle.Text = loc.GetString("HomeFeatTrayTitle");
        FeatTrayDesc.Text = loc.GetString("HomeFeatTrayDesc");
        ShipTitleText.Text = loc.GetString("HomeShipTitle");
        ShipBodyText.Text = loc.GetString("HomeShipBody");
        ShipNoteText.Text = loc.GetString("HomeShipNote");
    }

    private void CheckUpdatesQuickButton_Click(object sender, RoutedEventArgs e)
    {
        // Jump to Settings and auto-run the update check (same as the tray menu).
        NavigationService.Current.NavigateTo("settings", TrayNavigationRequest.CheckUpdatesParameter);
    }

    private void OpenSettingsQuickButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Current.NavigateTo("settings");
    }

    private void OpenAboutQuickButton_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.Current.NavigateTo("about");
    }
}
