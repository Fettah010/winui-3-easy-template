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

    public SetupWizardPage(SetupWizardViewModel viewModel)
    {
        // Constructor-injected (C1): PageFactory builds this page with the
        // view model from the container — never resolved at click time.
        // Resolve BEFORE InitializeComponent so {x:Bind ViewModel.…}
        // bindings evaluate against the real instance on first evaluation.
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
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
        // Re-render a showing location error in the new language.
        ViewModel.RefreshLocationError();
    }

    private void SetupWizardBackButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.GoBack();

    private void SetupWizardNextButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.GoNext();

    private void SetupWizardCompleteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // False = invalid folder: the VM parked on the location step
            // with the inline error, so there is nowhere to navigate.
            if (ViewModel.Complete())
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
