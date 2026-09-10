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
    // ── PLACEHOLDER CREDENTIALS ────────────────────────────────────────────
    // Replace these with your real values before publishing a release.
    //   - Repository must have releases created via Scripts/publish-release.ps1
    //   - Access token needs 'repo' scope for GitHub Releases. Generate at
    //     https://github.com/settings/tokens  (classic or fine-grained PAT that
    //     can read repo releases).
    public const string GitHubRepoUrl = "https://github.com/YOUR_USERNAME/YOUR_REPOSITORY";
    private const string GitHubAuthToken = "YOUR_GITHUB_TOKEN";
    // ────────────────────────────────────────────────────────────────────────

    public const string NoUpdateMessage =
        "You are running the latest version published on the selected release feed.";

    public static UpdateService Current { get; } = new();

    private readonly SemaphoreSlim _managerLock = new(1, 1);
    private UpdateManager? _manager;
    private string _channel = "stable";

    private UpdateService()
    {
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

                var source = new GithubSource(GitHubRepoUrl, GitHubAuthToken, prerelease: false);
                _manager = new UpdateManager(source, options, null);
                return _manager;
            }
            finally
            {
                _managerLock.Release();
            }
        }
    }

    public void SetChannel(string channel)
    {
        if (_channel == channel)
            return;

        _managerLock.Wait();
        try
        {
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

    public void ApplyUpdatesAndRestart(UpdateInfo update) => Manager.ApplyUpdatesAndRestart(update);
}