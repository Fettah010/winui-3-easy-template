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
