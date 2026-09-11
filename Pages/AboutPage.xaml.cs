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
    }
}
