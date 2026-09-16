using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevTemWinUi3.Services;
using Microsoft.UI.Xaml;

namespace DevTemWinUi3.ViewModels;

/// <summary>
/// Dedicated update page: check → download (live progress) → install, plus
/// the upstream release notes when the backend has any. Null-tolerant by
/// design (the updates feature may be compiled out and tests construct the
/// VM directly): without an engine the page reports its mode instead of
/// failing. Progress callbacks marshal through the captured sync context
/// (direct invoke in tests).
/// </summary>
public sealed partial class UpdateCenterViewModel : ObservableObject
{
    private readonly IUpdateService? _updates;
    private readonly SynchronizationContext? _uiThread;
    private readonly ProgressThrottler _progressThrottler = new();
    private bool _busy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Version line for the card header (current, or pending).</summary>
    [ObservableProperty]
    private string _versionLine = string.Empty;

    [ObservableProperty]
    private string _pendingVersion = string.Empty;

    [ObservableProperty]
    private string _releaseNotes = string.Empty;

    [ObservableProperty]
    private Visibility _notesVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private Visibility _progressVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private bool _canCheck = true;

    [ObservableProperty]
    private bool _canDownload;

    [ObservableProperty]
    private bool _canInstall;

    /// <summary>Installed version, shown in the details card. Never changes.</summary>
    [ObservableProperty]
    private string _currentVersion = string.Empty;

    /// <summary>Last-check line ("Last checked …" or empty before any check).</summary>
    [ObservableProperty]
    private string _lastCheckedText = string.Empty;

    /// <summary>
    /// Whether a check completed this session (success or no-update).
    /// The page checks on first arrival so it never sits stale; failures
    /// leave it false so the next visit retries.
    /// </summary>
    public bool HasCheckedThisSession { get; private set; }

    public UpdateCenterViewModel(IUpdateService? updates = null)
    {
        _updates = updates;
        try { _uiThread = SynchronizationContext.Current; } catch { }
        try { CurrentVersion = AppInfo.Current.Version; } catch { }
        RefreshLabels();
    }

    /// <summary>
    /// Re-reads resting labels (construction + language change). Never
    /// overwrites a running operation's state.
    /// </summary>
    public void RefreshLabels()
    {
        if (_busy)
            return;
        try
        {
            var loc = LocalizationService.Current;
            if (AppFeatures.IsExternallyManaged)
            {
                StatusMessage = ExternalHandlerText(loc);
                VersionLine = AppInfo.Current.VersionDisplay;
                PendingVersion = AppInfo.Current.VersionDisplay;
                ReleaseNotes = string.Empty;
                NotesVisibility = Visibility.Collapsed;
                HasUpdate = false;
                CanCheck = false;
                CanDownload = false;
                CanInstall = false;
                HasCheckedThisSession = true;
                return;
            }
            if (_updates is null)
            {
                StatusMessage = loc.GetString("UpdateCenterNoEngine");
                VersionLine = AppInfo.Current.VersionDisplay;
                PendingVersion = AppInfo.Current.VersionDisplay;
                ReleaseNotes = string.Empty;
                NotesVisibility = Visibility.Collapsed;
                HasUpdate = false;
                CanCheck = false;
                CanDownload = false;
                CanInstall = false;
                HasCheckedThisSession = true;
                return;
            }
            StatusMessage = HasUpdate && !string.IsNullOrWhiteSpace(PendingVersion)
                ? loc.GetString("UpdateAvailableVersion", PendingVersion)
                : loc.GetString("SettingsStatusIdle");
            VersionLine = HasUpdate && !string.IsNullOrWhiteSpace(PendingVersion)
                ? PendingVersion
                : AppInfo.Current.VersionDisplay;
            CanCheck = true;
        }
        catch { }
    }

    /// <summary>Runs the check; publishes version + notes on success.</summary>
    public async Task CheckAsync(CancellationToken ct = default)
    {
        if (_updates is null || _busy || AppFeatures.IsExternallyManaged)
            return;
        var loc = LocalizationService.Current;
        _busy = true;
        IsBusy = true;
        CanCheck = false;
        try
        {
            ct.ThrowIfCancellationRequested();
            StatusMessage = loc.GetString("SettingsChecking");
            var result = await _updates.CheckAsync();
            ct.ThrowIfCancellationRequested();
            StampLastChecked(loc);
            if (!result.HasUpdate)
            {
                HasUpdate = false;
                PendingVersion = string.Empty;
                VersionLine = AppInfo.Current.VersionDisplay;
                ReleaseNotes = string.Empty;
                NotesVisibility = Visibility.Collapsed;
                StatusMessage = loc.GetString("SettingsNoUpdate");
                CanDownload = false;
                CanInstall = false;
                HasCheckedThisSession = true;
                return;
            }
            HasUpdate = true;
            PendingVersion = result.Version ?? string.Empty;
            VersionLine = PendingVersion;
            if (!string.IsNullOrWhiteSpace(result.ReleaseNotes))
            {
                ReleaseNotes = result.ReleaseNotes.Trim();
                NotesVisibility = Visibility.Visible;
            }
            else
            {
                ReleaseNotes = string.Empty;
                NotesVisibility = Visibility.Collapsed;
            }
            StatusMessage = loc.GetString("UpdateAvailableVersion", PendingVersion);
            CanDownload = true;
            CanInstall = false;
            HasCheckedThisSession = true;
            AppLog.Information("UpdateCenter: v{Version} available", PendingVersion);
        }
        catch (OperationCanceledException)
        {
            RefreshLabels();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{loc.GetString("SettingsCheckFailed")}: {ex.Message}";
            AppLog.Error(ex, "UpdateCenter check failed");
        }
        finally
        {
            _busy = false;
            IsBusy = false;
            if (!AppFeatures.IsExternallyManaged && _updates is not null)
                CanCheck = true;
        }
    }

    /// <summary>Downloads the pending update with live progress.</summary>
    public async Task DownloadAsync(CancellationToken ct = default)
    {
        if (_updates is null || _busy || !HasUpdate)
            return;
        var loc = LocalizationService.Current;
        _busy = true;
        IsBusy = true;
        CanCheck = false;
        CanDownload = false;
        try
        {
            ProgressVisibility = Visibility.Visible;
            DownloadProgress = 0;
            StatusMessage = loc.GetString("SettingsDownloadingProgress", 0);
            // P1-2: coalesce the callback burst before marshalling — the
            // bar cannot render hundreds of updates per second anyway.
            _progressThrottler.Reset();
            await _updates.DownloadPendingUpdateAsync(percent =>
            {
                if (!_progressThrottler.ShouldReport(percent, DateTimeOffset.UtcNow))
                    return;
                SetOnUiThread(() =>
                {
                    DownloadProgress = percent;
                    StatusMessage = loc.GetString("SettingsDownloadingProgress", percent);
                });
            });
            ct.ThrowIfCancellationRequested();
            ProgressVisibility = Visibility.Collapsed;
            StatusMessage = loc.GetString("SettingsInstalling");
            CanInstall = true;
            AppLog.Information("UpdateCenter: download complete");
        }
        catch (OperationCanceledException)
        {
            ProgressVisibility = Visibility.Collapsed;
            RefreshLabels();
        }
        catch (Exception ex)
        {
            ProgressVisibility = Visibility.Collapsed;
            StatusMessage = loc.GetString("InstallFailedDetail", ex.Message);
            AppLog.Error(ex, "UpdateCenter download failed");
        }
        finally
        {
            _busy = false;
            IsBusy = false;
            CanCheck = true;
        }
    }

    /// <summary>Applies the pending update and restarts. No-op when idle.</summary>
    public void ApplyAndRestart()
    {
        if (_updates is null || _busy || !HasUpdate)
            return;
        try
        {
            if (!_updates.HasPendingUpdate)
                return;
            _updates.ApplyPendingUpdateAndRestart();
        }
        catch (Exception ex)
        {
            StatusMessage = GetInstallFailedText(ex.Message);
            AppLog.Error(ex, "UpdateCenter apply failed");
        }
    }

    private static string GetInstallFailedText(string detail)
    {
        try { return LocalizationService.Current.GetString("InstallFailedDetail", detail); }
        catch { return detail; }
    }

    /// <summary>
    /// Checks when nothing completed yet this session (page arrivals
    /// without an explicit "check" parameter). Failures leave the flag
    /// clear so the next visit retries; the button always can too.
    /// </summary>
    public Task EnsureCheckedAsync(CancellationToken ct = default)
    {
        if (HasCheckedThisSession)
            return Task.CompletedTask;
        return CheckAsync(ct);
    }

    private void StampLastChecked(LocalizationService loc)
    {
        try
        {
            LastCheckedText = loc.GetString(
                "UpdateCenterLastChecked",
                DateTime.Now.ToString("g", System.Globalization.CultureInfo.CurrentCulture));
        }
        catch { }
    }

    /// <summary>
    /// Handler line for the slim status surface (mirrors the Settings VM:
    /// Store text in store mode, packaged-dual text for engine scaffolds
    /// running packaged, feed text otherwise).
    /// </summary>
    internal static string ExternalHandlerText(LocalizationService loc)
    {
        if (AppFeatures.UpdateMode == "store")
            return loc.GetString("SettingsUpdatesExternalStore");
        if (AppInfo.IsPackaged)
            return loc.GetString("SettingsUpdatesExternalPackaged");
        return loc.GetString("SettingsUpdatesExternalAppInstaller");
    }

    private void SetOnUiThread(Action update)
    {
        try
        {
            if (_uiThread is null)
                update();
            else
                _uiThread.Post(_ => update(), null);
        }
        catch { }
    }
}
