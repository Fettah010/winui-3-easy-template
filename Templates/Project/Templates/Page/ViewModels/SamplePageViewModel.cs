using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace DevTemWinUi3.ViewModels;

/// <summary>
/// ViewModel for SamplePage. Transient: register with
/// services.AddTransient&lt;SamplePageViewModel&gt;() in
/// ServiceLocator.Initialize() and resolve it from the container in the
/// page constructor (never new). Keep the constructor side-effect free so
/// unit tests can construct the VM directly.
/// </summary>
public partial class SamplePageViewModel : ObservableObject
{
    public const string Route = "sample";

    /// <summary>
    /// The page's nav icon (chosen at scaffold time via --icon). Used for
    /// the NavigationViewItem's SymbolIcon; data-bound nav hosts can bind it.
    /// </summary>
    public Symbol NavSymbol => Symbol.TemplateIcon;

    [ObservableProperty]
    private string _headline = string.Empty;

    [RelayCommand]
    private void Reset() => Headline = string.Empty;
}
