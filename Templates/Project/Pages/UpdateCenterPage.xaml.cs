using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

// Pages are lifetime-managed by Frame (never disposed): no disposable fields.
public sealed partial class UpdateCenterPage : Page, INavigationAware
{
    public UpdateCenterViewModel ViewModel { get; }

    public UpdateCenterPage()
    {
        // Resolve BEFORE InitializeComponent so {x:Bind ViewModel.…}
        // bindings evaluate against the real instance on first load.
        ViewModel = ServiceLocator.GetRequiredService<UpdateCenterViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;

        LocalizationService.Current.LanguageChanged += OnLanguageChanged;
    }

    public void OnNavigatedTo(object? parameter)
    {
        // Background update flow routes here with "check" to run a check on
        // arrival (the ViewModel guards re-entrancy and missing engines).
        if (parameter is string s && s.Equals("check", StringComparison.OrdinalIgnoreCase))
            _ = ViewModel.CheckAsync();
    }

    public void OnNavigatedFrom()
    {
        // The page is gone: drop the static subscription (no cache → leak).
        LocalizationService.Current.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) =>
        ViewModel.RefreshLabels();

    private async void UpdateCheckButton_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.CheckAsync();

    private async void UpdateDownloadButton_Click(object sender, RoutedEventArgs e) =>
        await ViewModel.DownloadAsync();

    private void UpdateInstallButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.ApplyAndRestart();
}
