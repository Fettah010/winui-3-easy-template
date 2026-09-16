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

    private readonly object _gate = new();
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private UpdateManager? _manager;
    private string _channel = ChannelResolver.Stable;

    private UpdateService()
    {
        // Start on the persisted (or build-default) channel so background
        // checks use the right feed even if the user never opens Settings.
        try { _channel = SettingsService.Current.Channel; } catch { }
    }

    /// <summary>
    /// Whether the Velopack manager has been created. False before the
    /// first <see cref="EnsureInitializedAsync"/> (or check/download):
    /// every synchronous property below degrades to a safe default until
    /// then instead of blocking the dispatcher (P0-3).
    /// </summary>
    public bool IsInitialized
    {
        get
        {
            try
            {
                lock (_gate)
                {
                    return _manager is not null;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Creates the Velopack manager off the calling thread (never blocks
    /// it): construction + disk probes run on the thread pool behind an
    /// async gate with double-checked init. Null when creation fails
    /// (callers degrade to no-update). Never throws.
    /// </summary>
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await GetManagerAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Whether the app was installed through the Velopack installer.
    /// Running straight from the build output or debugger, updates are unavailable.
    /// Never blocks: false until <see cref="EnsureInitializedAsync"/> runs.
    /// </summary>
    public bool IsInstalled
    {
        get
        {
            try
            {
                var manager = TryGetManager();
                if (manager is null)
                    return false;
                return manager.IsInstalled;
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
                var manager = TryGetManager();
                if (manager is not null)
                    return manager.CurrentVersion?.ToString() ?? AppInfo.Current.Version;
                return AppInfo.Current.Version;
            }
            catch
            {
                return AppInfo.Current.Version;
            }
        }
    }

    /// <summary>Fast-path read of an already-created manager. Never creates one.</summary>
    private UpdateManager? TryGetManager()
    {
        lock (_gate)
        {
            return _manager;
        }
    }

    private async Task<UpdateManager?> GetManagerAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_manager is not null)
                return _manager;
        }

        await _initGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_gate)
            {
                if (_manager is not null)
                    return _manager;
            }

            string channel;
            lock (_gate)
            {
                channel = _channel;
            }

            var created = await Task.Run(
                () => CreateManager(channel), cancellationToken).ConfigureAwait(false);
            if (created is null)
                return null;
            lock (_gate)
            {
                _manager ??= created;
                return _manager;
            }
        }
        finally
        {
            try { _initGate.Release(); } catch { }
        }
    }

    private static UpdateManager? CreateManager(string channel)
    {
        try
        {
            var options = new UpdateOptions();
            if (channel is not "stable")
                options.ExplicitChannel = channel;

            var token = ResolveToken();
            AppLog.Information(
                "Update source: {RepoUrl} (channel {Channel}, token {HasToken})",
                GitHubRepoUrl, channel, !string.IsNullOrWhiteSpace(token));

            // accessToken is optional for public repos (unauthenticated
            // GitHub is rate-limited to 60 requests/hour/IP). Set your PAT
            // via GITHUB_TOKEN to avoid the limit.
            var source = new GithubSource(
                GitHubRepoUrl,
                string.IsNullOrWhiteSpace(token) ? null : token,
                prerelease: false);

            return new UpdateManager(source, options, null);
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Update manager creation failed");
            return null;
        }
    }

    private static string ResolveToken() =>
        Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;

    public void SetChannel(string channel)
    {
        channel = ChannelResolver.Normalize(channel);
        lock (_gate)
        {
            if (_channel == channel)
                return;
            AppLog.Information("Update channel set to {Channel}", channel);
            _channel = channel;
            // Dropped (not disposed: UpdateManager is not disposable);
            // rebuilt lazily with the new channel.
            _manager = null;
        }
    }

    private UpdateInfo? _pendingUpdate;

    public bool HasPendingUpdate
    {
        get
        {
            if (_pendingUpdate is not null)
                return true;
            try
            {
                var manager = TryGetManager();
                if (manager is null)
                    return false;
                return manager.UpdatePendingRestart is not null;
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Version waiting from a previous session (downloaded, unapplied).
    /// Null when nothing waits or the backend is unreachable. Never throws.
    /// </summary>
    public string? PendingRestartVersion
    {
        get
        {
            try
            {
                var manager = TryGetManager();
                if (manager is null)
                    return null;
                return manager.UpdatePendingRestart?.Version?.ToString();
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// Checks the feed and stashes any update as the pending one (replacing
    /// any previous pending update). The result carries only the version +
    /// upstream notes — no Velopack types leak to consumers. Initializes
    /// the manager first (off-thread); null manager degrades to no-update.
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync()
    {
        var manager = await GetManagerAsync(CancellationToken.None).ConfigureAwait(false);
        if (manager is null)
            return new UpdateCheckResult(false, null);
        var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
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
    public async Task DownloadPendingUpdateAsync(Action<int>? progress = null)
    {
        var pending = _pendingUpdate;
        if (pending is null)
            return;
        var manager = await GetManagerAsync(CancellationToken.None).ConfigureAwait(false);
        if (manager is null)
            return;
        await manager.DownloadUpdatesAsync(pending, progress, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the pending update and restarts the app. WinUI apps must
    /// terminate the current process so the updater can swap files safely.
    /// Prefers the session pending update, falls back to a previous
    /// session's prepared update. No-op when nothing is pending.
    /// </summary>
    public void ApplyPendingUpdateAndRestart()
    {
        Velopack.VelopackAsset? pending = _pendingUpdate is null
            ? TryGetDiskPending()
            : (Velopack.VelopackAsset) _pendingUpdate;
        if (pending is null)
            return;
        var manager = TryGetManager();
        if (manager is null)
            return;
        manager.ApplyUpdatesAndRestart(pending);
        Environment.Exit(0);
    }

    private Velopack.VelopackAsset? TryGetDiskPending()
    {
        try
        {
            var manager = TryGetManager();
            if (manager is null)
                return null;
            return manager.UpdatePendingRestart;
        }
        catch { return null; }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            // UpdateManager is not disposable; dropping the reference is
            // the whole teardown.
            _manager = null;
        }
        _initGate.Dispose();
        GC.SuppressFinalize(this);
    }
}