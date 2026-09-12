using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class SamplePage : Page, INavigationAware
{
    public SamplePageViewModel ViewModel { get; }

    public SamplePage()
    {
        this.InitializeComponent();
        // ViewModel comes from the container (transient per page), never new.
        // Wire-up step 1: register it in ServiceLocator.Initialize():
        //     services.AddTransient<SamplePageViewModel>();
        ViewModel = ServiceLocator.GetRequiredService<SamplePageViewModel>();
        DataContext = ViewModel;
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

    private void SamplePage_Loaded(object sender, RoutedEventArgs e) => UpdateResponsiveLayout();

    private void SamplePage_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateResponsiveLayout();

    /// <summary>
    /// Page-level responsive switch (padding here; two-column pages also
    /// restack their grids). Driven by the page's own width so the compact
    /// nav pane is accounted for. Never depends on language.
    /// </summary>
    private void UpdateResponsiveLayout()
    {
        bool narrow = ResponsiveLayout.ShouldUseNarrowPage(ActualWidth);

        ContentPanel.Padding = narrow
            ? new Thickness(16, 16, 16, 24)
            : new Thickness(32, 24, 32, 32);
    }

    private void ApplyLocalization()
    {
        // Wire-up step 2: add SampleTitle/SampleDescription to all three
        // dictionaries in LocalizationService (en-US/es-ES/fr-FR).
        // The generated test stub fails until you do.
        var loc = LocalizationService.Current;
        TitleText.Text = loc.GetString("SampleTitle");
        DescriptionText.Text = loc.GetString("SampleDescription");
    }
}
