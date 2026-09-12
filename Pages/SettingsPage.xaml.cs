using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using DevTemWinUi3.Services;
using DevTemWinUi3.ViewModels;

namespace DevTemWinUi3.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = (SettingsPageViewModel)DataContext;

        ApplyLocalization();

        // Populate language combo box
        foreach (var lang in LocalizationService.AvailableLanguages)
        {
            LanguageComboBox.Items.Add(lang);
        }

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

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Current;
        SettingsTitleText.Text = loc.GetString("SettingsTitle");
        SettingsDescText.Text = loc.GetString("SettingsDescription");
        SettingsAppearanceText.Text = loc.GetString("SettingsAppearance");
        SettingsLanguageText.Text = loc.GetString("SettingsLanguage");
        SettingsUpdatesText.Text = loc.GetString("SettingsUpdates");
        SettingsSystemTrayText.Text = loc.GetString("SettingsSystemTray");
        SettingsAboutText.Text = loc.GetString("SettingsAbout");

        ThemeCard.Header = loc.GetString("SettingsTheme");
        ThemeCard.Description = loc.GetString("SettingsThemeDesc");
        ThemeSystemItem.Content = loc.GetString("SettingsThemeSystem");
        ThemeLightItem.Content = loc.GetString("SettingsThemeLight");
        ThemeDarkItem.Content = loc.GetString("SettingsThemeDark");
        RefreshThemeComboBoxDisplay();

        LanguageCard.Header = loc.GetString("SettingsLanguage");
        LanguageCard.Description = loc.GetString("SettingsLanguageDesc");

        ChannelCard.Header = loc.GetString("SettingsChannelHeader");
        ChannelCard.Description = loc.GetString("SettingsChannelDesc");

        CheckUpdatesCard.Header = loc.GetString("SettingsCheckHeader");
        CheckUpdatesCard.Description = loc.GetString("SettingsCheckDesc");
        if (CheckUpdatesButton.IsEnabled)
            CheckUpdatesButton.Content = loc.GetString("SettingsCheckNow");

        AutoCheckCard.Header = loc.GetString("SettingsAutoCheckHeader");
        AutoCheckCard.Description = loc.GetString("SettingsAutoCheckDesc");

        UpdateStatusCard.Header = loc.GetString("SettingsCheckHeader");
        if (UpdateStatusCard.Visibility == Visibility.Collapsed)
            UpdateStatusCard.Description = loc.GetString("SettingsStatusIdle");

        MinimizeCard.Header = loc.GetString("SettingsMinimizeToTray");
        MinimizeCard.Description = loc.GetString("SettingsTrayMinimizeDesc");
        AutoStartCard.Header = loc.GetString("SettingsAutoStart");
        AutoStartCard.Description = loc.GetString("SettingsTrayAutoStartDesc");

        AppVersionCard.Header = loc.GetString("SettingsAppVersion");
        RepoCard.Header = loc.GetString("SettingsRepoHeader");

        if (InstallUpdateButton.Visibility == Visibility.Visible && InstallUpdateButton.IsEnabled)
            InstallUpdateButton.Content = loc.GetString("SettingsInstall");
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
            LocalizationService.Current.SetLanguage(lang.Tag);
            // The language applies instantly: refresh our own strings right away.
            // Other open pages refresh when navigated to; the nav pane listens
            // to LanguageChanged directly.
            ApplyLocalization();
        }
    }

    private Velopack.UpdateInfo? _pendingUpdate;
    private bool _autoCheckArmed;
    private CancellationTokenSource? _autoCheckCts;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ApplyLocalization();
        if (TrayNavigationRequest.ShouldAutoCheck(e.Parameter))
            _autoCheckArmed = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        // The page is gone: cancel any pending auto-check so it can never
        // touch a detached visual tree.
        _autoCheckArmed = false;
        try { _autoCheckCts?.Cancel(); } catch { }
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_autoCheckArmed)
            return;
        _autoCheckArmed = false;

        // Smart wait: run the check on the first actually-rendered frame so the
        // user first sees Settings open smoothly (from Home or tray restore),
        // then a short beat so the eye registers the page before the button
        // flips to "Checking…". Falls back after a timeout; cancelled on leave.
        _autoCheckCts?.Cancel();
        _autoCheckCts = new CancellationTokenSource();
        var ct = _autoCheckCts.Token;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
            await WaitForFirstRenderAsync(linked.Token);
            await Task.Delay(150, ct);
            if (!ct.IsCancellationRequested)
                await RunUpdateCheckAsync();
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

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        // A manual click wins over any armed auto-check still waiting.
        _autoCheckArmed = false;
        await RunUpdateCheckAsync();
    }

    /// <summary>
    /// Runs the Velopack update check and updates the status card.
    /// Public so the tray "Check for updates" menu item can trigger it
    /// right after navigating here.
    /// </summary>
    public async Task RunUpdateCheckAsync()
    {
        // The page may be navigated via tray before the XAML names are
        // wired in a test/host scenario — bail out gracefully.
        if (CheckUpdatesButton is null)
            return;

        var loc = LocalizationService.Current;
        var svc = UpdateService.Current;

        if (!svc.IsInstalled)
        {
            // Prefer the desktop toast over the inline card for this message;
            // only fall back to the card when OS toasts are unavailable.
            if (DesktopToastService.Current.TryShowNotInstalled())
                return;
            ShowUpdateStatus(UpdateService.NotInstalledMessage, false);
            NotificationService.Current.Info(loc.GetString("NotifUpdates"), loc.GetString("SettingsNotInstalled"));
            return;
        }

        CheckUpdatesButton.IsEnabled = false;
        CheckUpdatesButton.Content = loc.GetString("SettingsChecking");

        try
        {
            var update = await svc.CheckForUpdatesAsync();
            if (update is null)
            {
                ShowUpdateStatus(loc.GetString("SettingsNoUpdate"), false);
                NotificationService.Current.Success(loc.GetString("NotifUpdates"), loc.GetString("SettingsNoUpdate"));
            }
            else
            {
                _pendingUpdate = update;
                var version = update.TargetFullRelease.Version;
                ShowUpdateStatus($"v{version} available", true);
                NotificationService.Current.Info(loc.GetString("NotifUpdates"), $"Version {version} is available. Click Install to update.");
            }
        }
        catch (Exception ex)
        {
            ShowUpdateStatus($"{loc.GetString("SettingsCheckFailed")}: {ex.Message}", false);
            NotificationService.Current.Error(loc.GetString("SettingsCheckFailed"), ex.Message);
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
            CheckUpdatesButton.Content = loc.GetString("SettingsCheckNow");
        }
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null) return;

        var loc = LocalizationService.Current;
        InstallUpdateButton.IsEnabled = false;
        InstallUpdateButton.Content = loc.GetString("SettingsDownloading");

        try
        {
            await UpdateService.Current.DownloadUpdatesAsync(_pendingUpdate);
            UpdateStatusText.Text = loc.GetString("SettingsInstalling");
            InstallUpdateButton.Visibility = Visibility.Collapsed;
            NotificationService.Current.Success(loc.GetString("NotifUpdates"), "Update downloaded. Restarting…");

            await Task.Delay(300);
            UpdateService.Current.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Install failed: {ex.Message}";
            InstallUpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = loc.GetString("SettingsInstall");
            NotificationService.Current.Error(loc.GetString("SettingsCheckFailed"), ex.Message);
        }
    }

    private void ShowUpdateStatus(string message, bool showInstall)
    {
        var loc = LocalizationService.Current;
        UpdateStatusCard.Visibility = Visibility.Visible;
        UpdateStatusCard.Description = loc.GetString("SettingsCheckHeader");
        UpdateStatusText.Text = message;
        InstallUpdateButton.Visibility = showInstall ? Visibility.Visible : Visibility.Collapsed;
        if (showInstall)
        {
            InstallUpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = loc.GetString("SettingsInstall");
        }
    }
}
