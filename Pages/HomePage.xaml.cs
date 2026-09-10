using Microsoft.UI.Xaml.Controls;
using MyWinUIApp.Services;

namespace MyWinUIApp.Pages;

public sealed partial class HomePage : Page
{
    public AppInfo AppInfo { get; } = AppInfo.Current;

    public HomePage()
    {
        this.InitializeComponent();
    }
}