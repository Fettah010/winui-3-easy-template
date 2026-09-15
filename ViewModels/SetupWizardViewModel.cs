using CommunityToolkit.Mvvm.ComponentModel;
using DevTemWinUi3.Services;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.ViewModels;

/// <summary>
/// First-run setup wizard state: welcome → location → shortcuts → launch →
/// done. Pure preferences + step math (headless-testable); OS touches
/// (shortcuts, autostart, folder creation) live in
/// <see cref="SetupWizardService"/> and run only on Complete. Step content
/// binds to the per-step <see cref="Visibility"/> props (no converters).
/// </summary>
public sealed partial class SetupWizardViewModel : ObservableObject
{
    private const string KeyCompleted = "SetupWizardCompleted";
    private const string KeyDataFolder = "SetupWizardDataFolder";
    private const string KeyDesktopShortcut = "SetupWizardDesktopShortcut";
    private const string KeyStartShortcut = "SetupWizardStartShortcut";
    private const string KeyLaunchAtLogin = "SetupWizardLaunchAtLogin";

    [ObservableProperty]
    private int _selectedStepIndex;

    [ObservableProperty]
    private string _dataFolder = string.Empty;

    [ObservableProperty]
    private bool _createDesktopShortcut = true;

    [ObservableProperty]
    private bool _createStartShortcut = true;

    [ObservableProperty]
    private bool _launchAtLogin;

    [ObservableProperty]
    private Visibility _welcomeStepVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _locationStepVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _shortcutsStepVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _launchStepVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _doneStepVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _nextButtonVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _completeButtonVisibility = Visibility.Collapsed;

    public int StepCount => 5;

    public bool CanGoBack => SelectedStepIndex > 0;

    public bool CanGoNext => SelectedStepIndex < StepCount - 1;

    public bool IsLastStep => SelectedStepIndex >= StepCount - 1;

    public static bool IsCompleted
    {
        get
        {
            try { return LocalSettingsStore.Shared.Get(KeyCompleted, false); }
            catch { return false; }
        }
    }

    public SetupWizardViewModel()
    {
        try
        {
            var store = LocalSettingsStore.Shared;
            var saved = store.Get(KeyDataFolder, string.Empty);
            DataFolder = string.IsNullOrWhiteSpace(saved)
                ? DefaultDataFolder()
                : saved;
            CreateDesktopShortcut = store.Get(KeyDesktopShortcut, true);
            CreateStartShortcut = store.Get(KeyStartShortcut, true);
            LaunchAtLogin = store.Get(KeyLaunchAtLogin, false);
        }
        catch
        {
            DataFolder = DefaultDataFolder();
        }
        UpdateStepVisibility();
    }

    public void GoNext()
    {
        if (SelectedStepIndex < StepCount - 1)
            SelectedStepIndex++;
    }

    public void GoBack()
    {
        if (SelectedStepIndex > 0)
            SelectedStepIndex--;
    }

    /// <summary>
    /// Persists the choices and applies the OS side effects (best-effort,
    /// never throws). Marks first-run shown so the welcome dialog never
    /// appears behind the wizard.
    /// </summary>
    public void Complete()
    {
        try
        {
            var store = LocalSettingsStore.Shared;
            store.Set(KeyDataFolder, DataFolder);
            store.Set(KeyDesktopShortcut, CreateDesktopShortcut);
            store.Set(KeyStartShortcut, CreateStartShortcut);
            store.Set(KeyLaunchAtLogin, LaunchAtLogin);
            store.Set(KeyCompleted, true);
        }
        catch { }
        try { SetupWizardService.Current.ApplyChoices(DataFolder, CreateDesktopShortcut, CreateStartShortcut, LaunchAtLogin); } catch { }
        try { FirstRunService.Current.MarkAsShown(); } catch { }
        try { AppLog.Information("Setup wizard completed"); } catch { }
    }

    public void Skip()
    {
        try { LocalSettingsStore.Shared.Set(KeyCompleted, true); } catch { }
        try { FirstRunService.Current.MarkAsShown(); } catch { }
    }

    partial void OnSelectedStepIndexChanged(int value)
    {
        if (value < 0)
            SelectedStepIndex = 0;
        else if (value >= StepCount)
            SelectedStepIndex = StepCount - 1;
        UpdateStepVisibility();
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(IsLastStep));
    }

    private void UpdateStepVisibility()
    {
        WelcomeStepVisibility = SelectedStepIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        LocationStepVisibility = SelectedStepIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        ShortcutsStepVisibility = SelectedStepIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        LaunchStepVisibility = SelectedStepIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
        DoneStepVisibility = SelectedStepIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
        bool last = SelectedStepIndex >= StepCount - 1;
        NextButtonVisibility = last ? Visibility.Collapsed : Visibility.Visible;
        CompleteButtonVisibility = last ? Visibility.Visible : Visibility.Collapsed;
    }

    internal static string DefaultDataFolder()
    {
        try { return AppPaths.DataFolder; }
        catch { return string.Empty; }
    }
}
