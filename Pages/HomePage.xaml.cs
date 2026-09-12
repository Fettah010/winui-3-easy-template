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
}
