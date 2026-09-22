using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class HomePage : Page, INavigationAware
{
    public AppInfo AppInfo { get; } = AppInfo.Current;

    // Window resize drags fire SizeChanged on every tick. Ticks coalesce in
    // LayoutDebouncer and only a settled breakpoint FLIP reflows; same-breakpoint
    // ticks are a no-op. (Pane toggles never resize the content: overlay.)
    private readonly LayoutDebouncer _layoutDebouncer;
    private bool? _isNarrow;

    public HomePage()
    {
        this.InitializeComponent();
        _layoutDebouncer = new LayoutDebouncer(DispatcherQueue);
        // P2-3 caching policy: lightweight pages stay cached (no XAML
        // re-parse per nav); heavy pages (Diagnostics log tails) stay
        // transient — the default. See docs/DECISIONS.md.
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
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

    private void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyProductLinks();
        PaintLayout();
    }

    /// <summary>
    /// Resolves the releases link from <see cref="AppMetadata"/> at runtime
    /// (see AboutPage.ApplyProductLinks): XAML carries no hardcoded repo.
    /// </summary>
    private void ApplyProductLinks()
    {
        try
        {
            if (ReleasesLinkButton is not null)
            {
                string repo = AppMetadata.RepoUrl.TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(repo))
                    ReleasesLinkButton.NavigateUri = new System.Uri(repo + "/releases", System.UriKind.Absolute);
            }
        }
        catch { }
    }

    private void HomePage_SizeChanged(object sender, SizeChangedEventArgs e) =>
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
    /// breakpoint returns without touching the visual tree, so settled pane
    /// slides that never cross the threshold cost zero layout. Layout depends
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
