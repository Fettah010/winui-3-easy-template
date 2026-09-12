using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class AboutPage : Page
{
    public string AppVersion { get; }

    public AboutPage()
    {
        this.InitializeComponent();
        AppVersion = AppInfo.Current.Version;
        ApplyLocalization();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // The page is cached: refresh strings in case the language changed
        // while the user was on another page.
        ApplyLocalization();
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
