using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = (SettingsPageViewModel)DataContext;

        var loc = LocalizationService.Current;
        SettingsTitleText.Text = loc.GetString("SettingsTitle");
        SettingsDescText.Text = "Customize the appearance and behavior of the app.";
        SettingsAppearanceText.Text = loc.GetString("SettingsAppearance");
        SettingsLanguageText.Text = loc.GetString("SettingsLanguage");
        SettingsUpdatesText.Text = loc.GetString("SettingsUpdates");
        SettingsSystemTrayText.Text = loc.GetString("SettingsSystemTray");
        SettingsAboutText.Text = loc.GetString("SettingsAbout");

        // Populate language combo box
        foreach (var lang in LocalizationService.AvailableLanguages)
        {
            LanguageComboBox.Items.Add(lang);
        }

        // Select current language
        var currentTag = loc.CurrentLanguage;
        for (int i = 0; i < LocalizationService.AvailableLanguages.Count; i++)
        {
            if (LocalizationService.AvailableLanguages[i].Tag == currentTag)
            {
                LanguageComboBox.SelectedIndex = i;
                break;
            }
        }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is LanguageInfo lang)
        {
            LocalizationService.Current.SetLanguage(lang.Tag);
        }
    }

    private Velopack.UpdateInfo? _pendingUpdate;
    private bool _autoCheckArmed;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (TrayNavigationRequest.ShouldAutoCheck(e.Parameter))
            _autoCheckArmed = true;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_autoCheckArmed)
            return;
        _autoCheckArmed = false;
        // Let the page finish rendering before kicking off the check,
        // so the user sees Settings open and the button state change.
        await Task.Delay(350);
        await RunUpdateCheckAsync();
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        await RunUpdateCheckAsync();
    }

    /// <summary>
    /// Runs the Velopack update check and updates the status card.
    /// Public so the tray "Check for updates" menu item can trigger it
    /// right after navigating here.
    /// </summary>
    public async Task RunUpdateCheckAsync()
    {
        // The page may be navigated via tray before the XAML names are
        // wired in a test/host scenario — bail out gracefully.
        if (CheckUpdatesButton is null)
            return;

        var svc = UpdateService.Current;
        if (!svc.IsInstalled)
        {
            ShowUpdateStatus(UpdateService.NotInstalledMessage, false);
            NotificationService.Current.Info("Updates", UpdateService.NotInstalledMessage);
            return;
        }

        CheckUpdatesButton.IsEnabled = false;
        CheckUpdatesButton.Content = "Checking\u2026";

        try
        {
            var update = await svc.CheckForUpdatesAsync();
            if (update is null)
            {
                ShowUpdateStatus(UpdateService.NoUpdateMessage, false);
                NotificationService.Current.Success("Updates", UpdateService.NoUpdateMessage);
            }
            else
            {
                _pendingUpdate = update;
                var version = update.TargetFullRelease.Version;
                ShowUpdateStatus($"v{version} available", true);
                NotificationService.Current.Info("Updates", $"Version {version} is available. Click Install to update.");
            }
        }
        catch (Exception ex)
        {
            ShowUpdateStatus($"Check failed: {ex.Message}", false);
            NotificationService.Current.Error("Update check failed", ex.Message);
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
            CheckUpdatesButton.Content = "Check now";
        }
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null) return;

        InstallUpdateButton.IsEnabled = false;
        InstallUpdateButton.Content = "Downloading\u2026";

        try
        {
            await UpdateService.Current.DownloadUpdatesAsync(_pendingUpdate);
            UpdateStatusText.Text = "Installing\u2026";
            InstallUpdateButton.Visibility = Visibility.Collapsed;
            NotificationService.Current.Success("Updates", "Update downloaded. Restarting\u2026");

            await Task.Delay(300);
            UpdateService.Current.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Install failed: {ex.Message}";
            InstallUpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = "Install";
            NotificationService.Current.Error("Update failed", ex.Message);
        }
    }

    private void ShowUpdateStatus(string message, bool showInstall)
    {
        UpdateStatusCard.Visibility = Visibility.Visible;
        UpdateStatusText.Text = message;
        InstallUpdateButton.Visibility = showInstall ? Visibility.Visible : Visibility.Collapsed;
        if (showInstall)
        {
            InstallUpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = "Install";
        }
    }
}
