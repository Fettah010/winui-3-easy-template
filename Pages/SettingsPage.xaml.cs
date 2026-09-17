using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

// Pages are lifetime-managed by Frame (never disposed): the CancellationTokenSource
// is cancelled in OnNavigatedFrom instead of disposed (CA1001).
#pragma warning disable CA1001 // Type owns disposable fields but is not disposable
public sealed partial class SettingsPage : Page, INavigationAware
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        // Resolve BEFORE InitializeComponent so {x:Bind ViewModel.…}
        // bindings evaluate against the real instance on first load.
        ViewModel = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;

        // Static labels and update state bind in XAML (VM-owned); only the
        // packaged-install note and screen-reader names need a refresh on
        // language switch. (Unsubscribed in OnNavigatedFrom: this page is
        // not cached, so a static-event subscription would leak.)
        LocalizationService.Current.LanguageChanged += OnLanguageChanged;
        RefreshDynamicLabels();

        // Populate language combo box. English-only scaffolds hide the
        // picker: a single-item dropdown is noise, not a choice.
        foreach (var lang in LocalizationService.AvailableLanguages)
        {
            LanguageComboBox.Items.Add(lang);
        }
        if (LocalizationService.AvailableLanguages.Count <= 1)
            LanguageCard.Visibility = Visibility.Collapsed;

        SelectCurrentLanguage();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshDynamicLabels();
        RefreshThemeComboBoxDisplay();
        ViewModel.RefreshUpdateLabels();
    }

    private void SelectCurrentLanguage()
    {
        // Select current language
        var currentTag = LocalizationService.Current.CurrentLanguage;
        for (int i = 0; i < LocalizationService.AvailableLanguages.Count; i++)
        {
            if (LocalizationService.AvailableLanguages[i].Tag == currentTag)
            {
                LanguageComboBox.SelectedIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// Re-applies the labels that cannot live in XAML bindings: the
    /// packaged-install note (runtime-constant, set once) and screen-reader
    /// names. Update state (buttons, status card, progress) is VM-owned and
    /// bound — refreshing it here would clobber a running operation.
    /// </summary>
    private void RefreshDynamicLabels()
    {
        var loc = LocalizationService.Current;

        if (!ViewModel.AutoStartAvailable)
            AutoStartCard.Description = loc.GetString("SettingsTrayPackagedNote");

        AutomationProperties.SetName(ThemeComboBox, loc.GetString("SettingsTheme"));
        AutomationProperties.SetName(LanguageComboBox, loc.GetString("SettingsLanguage"));
        AutomationProperties.SetName(ChannelSegment, loc.GetString("SettingsChannelHeader"));
        AutomationProperties.SetName(AutoCheckToggle, loc.GetString("SettingsAutoCheckHeader"));
        AutomationProperties.SetName(AutoInstallToggle, loc.GetString("SettingsAutoInstall"));
        AutomationProperties.SetName(MinimizeToTrayToggle, loc.GetString("SettingsMinimizeToTray"));
        AutomationProperties.SetName(AutoStartToggle, loc.GetString("SettingsAutoStart"));
    }

    /// <summary>
    /// The closed ComboBox caches its display box and does not refresh it when
    /// the selected item's Content changes (i.e. on language switch). A
    /// round-trip through -1 forces a re-render. The ViewModel ignores -1, so
    /// the persisted theme is never touched.
    /// </summary>
    private void RefreshThemeComboBoxDisplay()
    {
        try
        {
            int current = ThemeComboBox.SelectedIndex;
            if (current < 0)
                return;
            ThemeComboBox.SelectedIndex = -1;
            ThemeComboBox.SelectedIndex = current;
        }
        catch { }
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is LanguageInfo lang)
        {
            // The language applies instantly through bindings + the
            // LanguageChanged handler above — no manual refresh needed.
            LocalizationService.Current.SetLanguage(lang.Tag);
        }
    }

    private bool _autoCheckArmed;
    private CancellationTokenSource? _autoCheckCts;

    public void OnNavigatedTo(object? parameter)
    {
        RefreshDynamicLabels();
        if (TrayNavigationRequest.ShouldAutoCheck(parameter))
            _autoCheckArmed = true;
    }

    public void OnNavigatedFrom()
    {
        // The page is gone: drop the static subscription (no cache → leak)
        // and cancel any pending auto-check so it can never touch a
        // detached visual tree.
        LocalizationService.Current.LanguageChanged -= OnLanguageChanged;
        _autoCheckArmed = false;
        try { _autoCheckCts?.Cancel(); } catch { }
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_autoCheckArmed)
            return;
        _autoCheckArmed = false;

        // Smart wait: open the popup on the first actually-rendered frame
        // so the user first sees Settings open smoothly (from Home or tray
        // restore). Falls back after a timeout; cancelled on leave.
        _autoCheckCts?.Cancel();
        _autoCheckCts = new CancellationTokenSource();
        var ct = _autoCheckCts.Token;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
            await WaitForFirstRenderAsync(linked.Token);
            if (!ct.IsCancellationRequested)
                await UpdateDialogService.ShowCheckAsync();
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    /// <summary>
    /// Completes once the compositor draws the next frame — i.e. the page is
    /// genuinely on screen — instead of guessing with a fixed delay.
    /// </summary>
    private static Task WaitForFirstRenderAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        void Handler(object? s, object e)
        {
            CompositionTarget.Rendering -= Handler;
            tcs.TrySetResult();
        }
        CompositionTarget.Rendering += Handler;
        ct.Register(() =>
        {
            CompositionTarget.Rendering -= Handler;
            tcs.TrySetCanceled(ct);
        });
        return tcs.Task;
    }

    private bool _checking;

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        // A manual click wins over any armed auto-check still waiting.
        _autoCheckArmed = false;
        if (_checking)
            return;
        _checking = true;
        // Immediate click feedback on the button itself (disabled +
        // "Checking…"): the result arrives via toast or dialog, which can
        // take a network round-trip — the button must visibly own the
        // in-flight check until then. Restored in finally so it can never
        // stick, whatever the flow below does.
        try
        {
            CheckUpdatesButton.IsEnabled = false;
            CheckUpdatesButton.Content = LocalizationService.Current.GetString("SettingsChecking");
            await UpdateDialogService.ShowCheckAsync();
        }
        catch { }
        finally
        {
            try
            {
                CheckUpdatesButton.Content = LocalizationService.Current.GetString("SettingsCheckNow");
                CheckUpdatesButton.IsEnabled = true;
            }
            catch { }
            _checking = false;
        }
    }

    private void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        // Engine mode hides this button; the external-mode action routes
        // to the update owner (Store / Windows Apps settings).
        _ = ViewModel.OpenExternalUpdateSourceAsync();
    }

    private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ImportSettingsCommand.ExecuteAsync(null);
        // An import may have changed the language behind the combo's back.
        SelectCurrentLanguage();
    }
}
#pragma warning restore CA1001
