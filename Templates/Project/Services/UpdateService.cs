using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace DevTemWinUi3.Services;

/// <summary>
/// Thin wrapper around the Velopack <see cref="UpdateManager"/> so pages
/// stay simple and the update source can be swapped/reused from one place.
/// Implements <see cref="IUpdateService"/> (the ViewModel seam): the pending
/// update is held internally, so no Velopack type leaks to consumers.
/// Process-lifetime singleton (never disposed in practice); implements
/// <see cref="IDisposable"/> to own its gate correctly (CA1001).
/// </summary>
public sealed class UpdateService : IUpdateService, IDisposable
{
    // GitHub Releases feed that hosts this app's updates. Releases are
    // created with Scripts/build-and-release.ps1 (vpk upload github).
    // Single source of truth: Services/Helpers/AppMetadata.cs (rewritten per app).
    public static string GitHubRepoUrl => AppMetadata.RepoUrl;

    public const string NotInstalledMessage =
        "Updates are only available for installed apps. Run setup.exe once, then check again.";

    public const string NoUpdateMessage =
        "You are running the latest version published on the selected release feed.";

    /// <summary>
    /// How often a long-running app re-checks for updates after the startup
    /// check (trayed apps live for days). Honored only when the auto-check
    /// setting is on and the app is installed; the feed poll itself is cheap.
    /// </summary>
    public static readonly TimeSpan PeriodicCheckInterval = TimeSpan.FromHours(6);

    public static UpdateService Current { get; } = new();

    private readonly SemaphoreSlim _managerLock = new(1, 1);
    private UpdateManager? _manager;
    private string _channel = ChannelResolver.Stable;

    private UpdateService()
    {
        // Start on the persisted (or build-default) channel so background
        // checks use the right feed even if the user never opens Settings.
        try { _channel = SettingsService.Current.Channel; } catch { }
    }

    /// <summary>
    /// Whether the app was installed through the Velopack installer.
    /// Running straight from the build output or debugger, updates are unavailable.
    /// </summary>
    public bool IsInstalled
    {
        get
        {
            try
            {
                return Manager.IsInstalled;
            }
            catch
            {
                return false;
            }
        }
    }

    public string CurrentVersion
    {
        get
        {
            try
            {
                return Manager.CurrentVersion?.ToString() ?? AppInfo.Current.Version;
            }
            catch
            {
                return AppInfo.Current.Version;
            }
        }
    }

    private UpdateManager Manager
    {
        get
        {
            _managerLock.Wait();
            try
            {
                if (_manager is not null)
                    return _manager;

                var options = new UpdateOptions();
                if (_channel is not "stable")
                    options.ExplicitChannel = _channel;

                var token = ResolveToken();
                AppLog.Information(
                    "Update source: {RepoUrl} (channel {Channel}, token {HasToken})",
                    GitHubRepoUrl, _channel, !string.IsNullOrWhiteSpace(token));

                // accessToken is optional for public repos (unauthenticated
                // GitHub is rate-limited to 60 requests/hour/IP). Set your PAT
                // via GITHUB_TOKEN to avoid the limit.
                var source = new GithubSource(
                    GitHubRepoUrl,
                    string.IsNullOrWhiteSpace(token) ? null : token,
                    prerelease: false);

                _manager = new UpdateManager(source, options, null);
                return _manager;
            }
            finally
            {
                _managerLock.Release();
            }
        }
    }

    private static string ResolveToken() =>
        Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;

    public void SetChannel(string channel)
    {
        channel = ChannelResolver.Normalize(channel);
        if (_channel == channel)
            return;

        _managerLock.Wait();
        try
        {
            AppLog.Information("Update channel set to {Channel}", channel);
            _channel = channel;
            _manager = null; // rebuilt lazily with the new channel
        }
        finally
        {
            _managerLock.Release();
        }
    }

    private UpdateInfo? _pendingUpdate;

    public bool HasPendingUpdate => _pendingUpdate is not null;

    /// <summary>
    /// Checks the feed and stashes any update as the pending one (replacing
    /// any previous pending update). The result carries only the version +
    /// upstream notes — no Velopack types leak to consumers.
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync()
    {
        var update = await Manager.CheckForUpdatesAsync();
        _pendingUpdate = update;
        if (update is null)
            return new UpdateCheckResult(false, null);
        string? notes = null;
        try { notes = update.TargetFullRelease.NotesMarkdown; } catch { }
        if (string.IsNullOrWhiteSpace(notes))
            notes = null;
        return new UpdateCheckResult(true, update.TargetFullRelease.Version?.ToString(), notes);
    }

    /// <summary>
    /// Downloads the pending update (from the last <see cref="CheckAsync"/>).
    /// No-op when nothing is pending.
    /// </summary>
    public Task DownloadPendingUpdateAsync(Action<int>? progress = null)
    {
        var pending = _pendingUpdate;
        if (pending is null)
            return Task.CompletedTask;
        return Manager.DownloadUpdatesAsync(pending, progress, CancellationToken.None);
    }

    /// <summary>
    /// Applies the pending update and restarts the app. WinUI apps must
    /// terminate the current process so the updater can swap files safely.
    /// No-op when nothing is pending.
    /// </summary>
    public void ApplyPendingUpdateAndRestart()
    {
        var pending = _pendingUpdate;
        if (pending is null)
            return;
        Manager.ApplyUpdatesAndRestart(pending);
        Environment.Exit(0);
    }

    public void Dispose()
    {
        _managerLock.Dispose();
        GC.SuppressFinalize(this);
    }
}