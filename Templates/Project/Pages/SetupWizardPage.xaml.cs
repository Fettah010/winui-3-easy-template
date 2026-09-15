using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

// Pages are lifetime-managed by Frame (never disposed): no disposable fields.
public sealed partial class SetupWizardPage : Page, INavigationAware
{
    public SetupWizardViewModel ViewModel { get; }

    public SetupWizardPage()
    {
        // Resolve BEFORE InitializeComponent so {x:Bind ViewModel.…}
        // bindings evaluate against the real instance on first load.
        ViewModel = ServiceLocator.GetRequiredService<SetupWizardViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;

        LocalizationService.Current.LanguageChanged += OnLanguageChanged;
    }

    public void OnNavigatedTo(object? parameter)
    {
    }

    public void OnNavigatedFrom()
    {
        // The page is gone: drop the static subscription (no cache → leak).
        LocalizationService.Current.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
    }

    private void SetupWizardBackButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.GoBack();

    private void SetupWizardNextButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.GoNext();

    private void SetupWizardCompleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.Complete();
            NavigationService.Current.NavigateTo("home");
        }
        catch { }
    }

    private async void SetupBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            if (App.Current is App app && app.MainWindowInstance is not null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindowInstance);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }
            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
                ViewModel.DataFolder = folder.Path;
        }
        catch { }
    }
}
