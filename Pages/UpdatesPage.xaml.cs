using Microsoft.UI.Xaml.Controls;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class UpdatesPage : Page
{
    public UpdatesPageViewModel ViewModel { get; }

    public UpdatesPage()
    {
        this.InitializeComponent();
        ViewModel = (UpdatesPageViewModel)DataContext;
    }
}
