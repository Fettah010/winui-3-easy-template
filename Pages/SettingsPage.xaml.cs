using Microsoft.UI.Xaml.Controls;
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
}
