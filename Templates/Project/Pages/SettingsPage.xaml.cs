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
        this.InitializeComponent();
        // ViewModel comes from the container (transient per page), never new.
        ViewModel = ServiceLocator.GetRequiredService<SettingsPageViewModel>();
        DataContext = ViewModel;

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

        // Feature sections collapse at runtime when their flag was
        // scaffolded off (XAML keeps no conditionals by design).
        UpdatesSection.Visibility = AppFeatures.Updates ? Visibility.Visible : Visibility.Collapsed;
        TraySection.Visibility = AppFeatures.Tray ? Visibility.Visible : Visibility.Collapsed;

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
        AutomationProperties.SetName(ThemeComboBox, loc.GetString("SettingsTheme"));
        RefreshThemeComboBoxDisplay();

        LanguageCard.Header = loc.GetString("SettingsLanguage");
        LanguageCard.Description = loc.GetString("SettingsLanguageDesc");
        AutomationProperties.SetName(LanguageComboBox, loc.GetString("SettingsLanguage"));

        ChannelCard.Header = loc.GetString("SettingsChannelHeader");
        ChannelCard.Description = loc.GetString("SettingsChannelDesc");
        AutomationProperties.SetName(ChannelSegment, loc.GetString("SettingsChannelHeader"));

        CheckUpdatesCard.Header = loc.GetString("SettingsCheckHeader");
        CheckUpdatesCard.Description = loc.GetString("SettingsCheckDesc");
        if (CheckUpdatesButton.IsEnabled)
            CheckUpdatesButton.Content = loc.GetString("SettingsCheckNow");

        AutoCheckCard.Header = loc.GetString("SettingsAutoCheckHeader");
        AutoCheckCard.Description = loc.GetString("SettingsAutoCheckDesc");
        AutomationProperties.SetName(AutoCheckToggle, loc.GetString("SettingsAutoCheckHeader"));

        UpdateStatusCard.Header = loc.GetString("SettingsCheckHeader");
        if (UpdateStatusCard.Visibility == Visibility.Collapsed)
            UpdateStatusCard.Description = loc.GetString("SettingsStatusIdle");

        MinimizeCard.Header = loc.GetString("SettingsMinimizeToTray");
        MinimizeCard.Description = loc.GetString("SettingsTrayMinimizeDesc");
        AutomationProperties.SetName(MinimizeToTrayToggle, loc.GetString("SettingsMinimizeToTray"));
        AutoStartCard.Header = loc.GetString("SettingsAutoStart");
        AutoStartCard.Description = loc.GetString("SettingsTrayAutoStartDesc");
        AutomationProperties.SetName(AutoStartToggle, loc.GetString("SettingsAutoStart"));
        if (!ViewModel.AutoStartAvailable)
            AutoStartCard.Description = loc.GetString("SettingsTrayPackagedNote");

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

#if (updates)
    private Velopack.UpdateInfo? _pendingUpdate;
#endif
    private bool _autoCheckArmed;
    private CancellationTokenSource? _autoCheckCts;

    public void OnNavigatedTo(object? parameter)
    {
        ApplyLocalization();
        if (TrayNavigationRequest.ShouldAutoCheck(parameter))
            _autoCheckArmed = true;
    }

    public void OnNavigatedFrom()
    {
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
    /// Without the updates feature this is an inert stub (the whole section
    /// is collapsed, so it is unreachable — the shell only satisfies XAML).
    /// </summary>
#if (updates)
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
            // Unpackaged run: explain via the animated in-app toast (modern
            // WinUI style) instead of the inline status card.
            LoggingService.Log.Information("Update check: app is not installed, showing toast");
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
#else
    public Task RunUpdateCheckAsync() => Task.CompletedTask;
#endif

#if (updates)
    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pendingUpdate is null) return;

        var loc = LocalizationService.Current;
        InstallUpdateButton.IsEnabled = false;
        InstallUpdateButton.Content = loc.GetString("SettingsDownloading");
        CheckUpdatesButton.IsEnabled = false;

        // Smooth determinate progress while Velopack downloads the delta/full
        // package; the callback runs on a background thread, so marshal in.
        DownloadProgressBar.Visibility = Visibility.Visible;
        DownloadProgressBar.Value = 0;
        UpdateStatusText.Text = loc.GetString("SettingsDownloadingProgress", 0);

        try
        {
            await UpdateService.Current.DownloadUpdatesAsync(_pendingUpdate, percent =>
            {
                try
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        DownloadProgressBar.Value = percent;
                        UpdateStatusText.Text = loc.GetString("SettingsDownloadingProgress", percent);
                    });
                }
                catch { }
            });

            UpdateStatusText.Text = loc.GetString("SettingsInstalling");
            DownloadProgressBar.Visibility = Visibility.Collapsed;
            InstallUpdateButton.Visibility = Visibility.Collapsed;
            NotificationService.Current.Success(loc.GetString("NotifUpdates"), "Update downloaded. Restarting…");

            await Task.Delay(300);
            UpdateService.Current.ApplyUpdatesAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Install failed: {ex.Message}";
            DownloadProgressBar.Visibility = Visibility.Collapsed;
            InstallUpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = loc.GetString("SettingsInstall");
            CheckUpdatesButton.IsEnabled = true;
            NotificationService.Current.Error(loc.GetString("SettingsCheckFailed"), ex.Message);
        }
    }
#else
    private void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
    }
#endif

    private void ShowUpdateStatus(string message, bool showInstall)
    {
        var loc = LocalizationService.Current;
        UpdateStatusCard.Visibility = Visibility.Visible;
        UpdateStatusCard.Description = loc.GetString("SettingsCheckHeader");
        DownloadProgressBar.Visibility = Visibility.Collapsed;
        DownloadProgressBar.Value = 0;
        UpdateStatusText.Text = message;
        InstallUpdateButton.Visibility = showInstall ? Visibility.Visible : Visibility.Collapsed;
        if (showInstall)
        {
            InstallUpdateButton.IsEnabled = true;
            InstallUpdateButton.Content = loc.GetString("SettingsInstall");
        }
    }
}
#pragma warning restore CA1001
