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
        ApplyLocalization();
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

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Current;
        AboutTitleText.Text = loc.GetString("AboutTitle");
        AboutSubtitleText.Text = loc.GetString("AboutSubtitle");
        AboutDescriptionText.Text = loc.GetString("AboutDescription");
        GithubLink.Content = loc.GetString("AboutGithub");
        ReportIssueLink.Content = loc.GetString("AboutReportIssue");
        GlanceTitleText.Text = loc.GetString("AboutGlance");
        GlanceVersionLabel.Text = loc.GetString("AboutVersion");
        GlanceFrameworkLabel.Text = loc.GetString("AboutFramework");
        GlancePlatformLabel.Text = loc.GetString("AboutPlatform");
        AboutAppInfoText.Text = loc.GetString("AboutAppInfo");
        AboutTechnologyText.Text = loc.GetString("AboutTechnology");
        AboutLicenseText.Text = loc.GetString("AboutLicense");
        AboutLinksText.Text = loc.GetString("AboutLinks");

        NameCard.Header = loc.GetString("AboutCardName");
        NameCard.Description = loc.GetString("AboutCardNameDesc");
        VersionCard.Header = loc.GetString("AboutCardVersion");
        VersionCard.Description = loc.GetString("AboutCardVersionDesc");
        FrameworkCard.Header = loc.GetString("AboutCardFramework");
        FrameworkCard.Description = loc.GetString("AboutCardFrameworkDesc");
        PlatformCard.Header = loc.GetString("AboutCardPlatform");
        PlatformCard.Description = loc.GetString("AboutCardPlatformDesc");

        UpdatesCard.Header = loc.GetString("AboutCardUpdates");
        UpdatesCard.Description = loc.GetString("AboutCardUpdatesDesc");
        LoggingCard.Header = loc.GetString("AboutCardLogging");
        LoggingCard.Description = loc.GetString("AboutCardLoggingDesc");
        MvvmCard.Header = loc.GetString("AboutCardMvvm");
        MvvmCard.Description = loc.GetString("AboutCardMvvmDesc");
        UiCard.Header = loc.GetString("AboutCardUi");
        UiCard.Description = loc.GetString("AboutCardUiDesc");

        LicenseCard.Header = loc.GetString("AboutCardLicense");
        LicenseCard.Description = loc.GetString("AboutCardLicenseDesc");
        ViewLicenseButton.Content = loc.GetString("AboutViewLicense");

        SourceCard.Header = loc.GetString("AboutCardSource");
        SourceCard.Description = loc.GetString("AboutCardSourceDesc");
        GithubButton.Content = loc.GetString("AboutGithubShort");
        ReleasesCard.Header = loc.GetString("AboutCardReleases");
        ReleasesCard.Description = loc.GetString("AboutCardReleasesDesc");
        ReleasesButton.Content = loc.GetString("AboutViewReleases");
        IssuesCard.Header = loc.GetString("AboutCardIssues");
        IssuesCard.Description = loc.GetString("AboutCardIssuesDesc");
        IssuesButton.Content = loc.GetString("AboutOpenIssue");
    }
}
