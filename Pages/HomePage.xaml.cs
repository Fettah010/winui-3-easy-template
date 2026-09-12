using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class HomePage : Page
{
    public AppInfo AppInfo { get; } = AppInfo.Current;

    public HomePage()
    {
        this.InitializeComponent();

        var loc = LocalizationService.Current;
        HomeTitleText.Text = loc.GetString("HomeTitle");
        HomeDescText.Text = loc.GetString("HomeDescription");
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
