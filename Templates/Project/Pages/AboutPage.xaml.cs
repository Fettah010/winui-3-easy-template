using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class AboutPage : Page, INavigationAware
{
    public string AppVersion { get; }

    // Window resize drags fire SizeChanged on every tick. Ticks coalesce in
    // LayoutDebouncer and only a settled breakpoint FLIP reflows; same-breakpoint
    // ticks are a no-op. (Pane toggles never resize the content: overlay.)
    private readonly LayoutDebouncer _layoutDebouncer;
    private bool? _isNarrow;

    public AboutPage()
    {
        // Assign BEFORE InitializeComponent so {x:Bind AppVersion} binds
        // against the real value on first evaluation. Carries the build
        // commit when known so About names the exact binary (#18).
        AppVersion = AppInfo.Current.VersionWithCommit;
        this.InitializeComponent();
        _layoutDebouncer = new LayoutDebouncer(DispatcherQueue);
    }

    public void OnNavigatedTo(object? parameter)
    {
        // The page is cached but every label is a live loc binding now —
        // nothing to refresh on return.
        ApplyProductLinks();
        PaintLayout();
    }

    public void OnNavigatedFrom()
    {
        // Nothing to tear down on leave.
    }

    private void AboutPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyProductLinks();
        PaintLayout();
    }

    /// <summary>
    /// Resolves repo/license links from <see cref="AppMetadata"/> (backed by
    /// <c>ProductConfiguration</c>) at runtime, so XAML carries no hardcoded
    /// repo identity and one rebrand (init-template or scaffold params)
    /// covers every surface. Never throws.
    /// </summary>
    private void ApplyProductLinks()
    {
        try
        {
            string repo = AppMetadata.RepoUrl.TrimEnd('/');
            SetUri(GithubButton, repo);
            SetUri(SourceButton, repo);
            SetUri(ReportIssueButton, repo + "/issues");
            SetUri(OpenIssueButton, repo + "/issues");
            SetUri(ViewReleasesButton, repo + "/releases");
            SetUri(ViewLicenseButton, AppMetadata.LicenseUrl);
        }
        catch { }
    }

    private static void SetUri(Microsoft.UI.Xaml.Controls.HyperlinkButton? button, string url)
    {
        try
        {
            if (button is null || string.IsNullOrWhiteSpace(url))
                return;
            button.NavigateUri = new System.Uri(url, System.UriKind.Absolute);
        }
        catch { }
    }

    /// <summary>
    /// "What's new" recall: re-shows the current version's notes on demand.
    /// Recall never marks the version (it is already this version's) and
    /// never throws.
    /// </summary>
    private async void ViewWhatsNewButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await FirstRunDialogService.ShowWhatsNewRecallAsync(this.XamlRoot);
        }
        catch { }
    }

    private void AboutPage_SizeChanged(object sender, SizeChangedEventArgs e) =>
        _layoutDebouncer.RequestSwap(
            ContentPanel,
            () => ResponsiveLayout.ShouldUseNarrowPage(e.NewSize.Width),
            () => UpdateResponsiveLayout(ResponsiveLayout.ShouldUseNarrowPage(e.NewSize.Width)));

    /// <summary>Resting paint (no-op when the breakpoint is unchanged).</summary>
    private void PaintLayout()
    {
        bool narrow = ResponsiveLayout.ShouldUseNarrowPage(ActualWidth);
        _layoutDebouncer.PaintInitial(narrow, () => UpdateResponsiveLayout(narrow));
    }

    /// <summary>
    /// Switches between two-column and stacked layouts based on the page's own
    /// width (not the window's, so the compact nav pane is accounted for).
    /// Done in code because VisualState setters cannot reliably retarget Grid
    /// columns/rows. Stacking zeroes the unused column and reuses the other
    /// one as Star (never ColumnSpan: spanned children join Auto sizing with
    /// unbounded measure and blow the grid past the card). Idempotent: same
    /// breakpoint returns without touching the visual tree. Layout depends
    /// only on width — never on language — so longer translations wrap
    /// instead of shifting content.
    /// </summary>
    private void UpdateResponsiveLayout(bool narrow)
    {
        if (_isNarrow.HasValue && _isNarrow.Value == narrow)
            return;
        _isNarrow = narrow;

        ContentPanel.Padding = narrow
            ? new Thickness(16, 16, 16, 24)
            : new Thickness(32, 24, 32, 32);

        HeroMainCol.Width = new GridLength(1, GridUnitType.Star);

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
