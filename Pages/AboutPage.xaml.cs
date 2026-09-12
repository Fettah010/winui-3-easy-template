using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.Services;

namespace DevTemWinUi3.Pages;

public sealed partial class AboutPage : Page
{
    public string AppVersion { get; }

    public AboutPage()
    {
        this.InitializeComponent();
        AppVersion = AppInfo.Current.Version;

        var loc = LocalizationService.Current;
        AboutTitleText.Text = loc.GetString("AboutTitle");
        AboutAppInfoText.Text = loc.GetString("AboutAppInfo");
        AboutTechnologyText.Text = loc.GetString("AboutTechnology");
        AboutLicenseText.Text = loc.GetString("AboutLicense");
        AboutLinksText.Text = loc.GetString("AboutLinks");
    }
}
