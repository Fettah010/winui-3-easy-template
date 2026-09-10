using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace DevTemWinUi3.Services;

/// <summary>
/// Thin wrapper around the Velopack <see cref="UpdateManager"/> so pages
/// stay simple and the update source can be swapped/reused from one place.
/// </summary>
public sealed class UpdateService
{
    // GitHub Releases feed that hosts this app's updates. Releases are
    // created with Scripts/build-and-release.ps1 (vpk upload github).
    public const string GitHubRepoUrl = "https://github.com/Fettah010/winui-3-easy-template";

    public const string NotInstalledMessage =
        "Updates are only available for installed apps. Run setup.exe once, then check again.";

    public const string NoUpdateMessage =
        "You are running the latest version published on the selected release feed.";

    public static UpdateService Current { get; } = new();

    private readonly SemaphoreSlim _managerLock = new(1, 1);
    private UpdateManager? _manager;
    private string _channel = "stable";

    private UpdateService()
    {
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
                LoggingService.Log.Information(
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
        if (_channel == channel)
            return;

        _managerLock.Wait();
        try
        {
            LoggingService.Log.Information("Update channel set to {Channel}", channel);
            _channel = channel;
            _manager = null; // rebuilt lazily with the new channel
        }
        finally
        {
            _managerLock.Release();
        }
    }

    public Task<UpdateInfo?> CheckForUpdatesAsync() => Manager.CheckForUpdatesAsync();

    public Task DownloadUpdatesAsync(UpdateInfo update, Action<int>? progress = null) =>
        Manager.DownloadUpdatesAsync(update, progress, CancellationToken.None);

    /// <summary>
    /// Applies the update and restarts the app. WinUI apps must terminate the
    /// current process so the updater can swap files safely.
    /// </summary>
    public void ApplyUpdatesAndRestart(UpdateInfo update)
    {
        Manager.ApplyUpdatesAndRestart(update);
        Environment.Exit(0);
    }
}