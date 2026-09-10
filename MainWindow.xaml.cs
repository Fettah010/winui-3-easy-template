using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DevTemWinUi3.Pages;

namespace DevTemWinUi3;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "DevTem-WinUI 3";
        this.SystemBackdrop = new MicaBackdrop();

        // Native-feeling, theme-aware title bar: the app content extends into the
        // caption area, Mica shows through it, and the caption buttons (min/max/
        // close) stay OS-drawn with automatic light/dark colors.
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        this.AppWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        this.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;

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
