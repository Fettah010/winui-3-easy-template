using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class HomePage : Page, INavigationAware
{
    public AppInfo AppInfo { get; } = AppInfo.Current;

    public HomePage()
    {
        this.InitializeComponent();
        ApplyLocalization();
        ApplyFeatureVisibility();
    }

    public void OnNavigatedTo(object? parameter)
    {
        // The page is cached: refresh strings in case the language changed
        // while the user was on another page.
        ApplyLocalization();
        UpdateResponsiveLayout();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to tear down on leave.
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

        ApplyFeatureVisibility();
        LayoutFeatureCards(narrow);
    }

    /// <summary>
    /// Collapses UI for features scaffolded off (AppFeatures). Runs pre-render
    /// (ctor) and on every layout pass so cached pages stay correct.
    /// XAML keeps no conditionals by design: the template engine skips
    /// markers in markup files.
    /// </summary>
    private void ApplyFeatureVisibility()
    {
        var updates = AppFeatures.Updates ? Visibility.Visible : Visibility.Collapsed;
        var tray = AppFeatures.Tray ? Visibility.Visible : Visibility.Collapsed;

        CheckUpdatesQuickButton.Visibility = updates;
        ReleasesLinkButton.Visibility = updates;
        FeatureCard1.Visibility = updates;
        FeatureCard4.Visibility = tray;
        ShipSection.Visibility = updates;
    }

    /// <summary>
    /// Fills the feature grid row-major from the visible cards (card1..4
    /// order, skipping collapsed features) in wide mode, stacks rows in
    /// narrow mode. Identical to the static layout when all features are on.
    /// </summary>
    private void LayoutFeatureCards(bool narrow)
    {
        var cards = new FrameworkElement[] { FeatureCard1, FeatureCard2, FeatureCard3, FeatureCard4 };
        int slot = 0;
        foreach (var card in cards)
        {
            if (card.Visibility != Visibility.Visible)
                continue;
            if (narrow)
            {
                Grid.SetRow(card, slot);
                Grid.SetColumn(card, 0);
            }
            else
            {
                Grid.SetRow(card, slot / 2);
                Grid.SetColumn(card, slot % 2);
            }
            slot++;
        }
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
