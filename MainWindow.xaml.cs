using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using MyWinUIApp.Pages;

namespace MyWinUIApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.SystemBackdrop = new MicaBackdrop();

        ContentFrame.Navigate(typeof(HomePage));
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.Tag is not string tag)
            return;

        switch (tag)
        {
            case "home":
                ContentFrame.Navigate(typeof(HomePage));
                break;
            case "updates":
                ContentFrame.Navigate(typeof(UpdatesPage));
                break;
        }
    }
}
