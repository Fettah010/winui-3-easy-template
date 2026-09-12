using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    [ObservableProperty]
    private string _headline = string.Empty;

    [RelayCommand]
    private void Reset() => Headline = string.Empty;
}
