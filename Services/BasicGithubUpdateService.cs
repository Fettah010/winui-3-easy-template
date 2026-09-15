using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// Zero-dependency GitHub-releases update checker behind
/// <see cref="IUpdateService"/>: polls the releases API, picks the newest
/// qualifying tag, downloads the attached Setup <c>.exe</c>, and launches
/// it. No update SDK, no installer framework, no background service —
// one HTTP call per check. Channel mapping: <c>stable</c> ignores
/// prereleases, <c>beta</c> includes them. Process-lifetime singleton
/// (never disposed in practice); implements <see cref="IDisposable"/>
/// to own its <see cref="HttpClient"/> correctly (CA1001).
/// <para/>
/// Release convention (documented in
/// <c>docs/feature-guides/updates-basic.md</c>): attach the installer as a
/// <c>.exe</c> asset to the GitHub release; tags look like
/// <c>v0.0.4</c> or <c>v0.0.4-beta</c>.
/// </summary>
public sealed class BasicGithubUpdateService : IUpdateService, IDisposable
{
    /// <summary>
    /// How often a long-running app re-checks (same cadence as Velopack;
    /// one cheap API call per tick, well under rate limits).
    /// </summary>
    public static readonly TimeSpan PeriodicCheckInterval = TimeSpan.FromHours(6);

    public static BasicGithubUpdateService Current { get; } = new();

    private readonly HttpMessageHandler? _testHandler;
    private readonly object _lock = new();
    private HttpClient? _http;
    private string _channel = ChannelResolver.Stable;
    private PendingRelease? _pending;
    private string? _downloadedPath;

    /// <summary>
    /// The optional handler is a test seam (stub the releases API without
    /// the network). Production uses the default handler.
    /// </summary>
    public BasicGithubUpdateService(HttpMessageHandler? handler = null)
    {
        _testHandler = handler;
        try { _channel = SettingsService.Current.Channel; } catch { }
    }

    /// <summary>
    /// Basic cannot detect installer state, so checks run everywhere —
    /// including dev runs, which makes the flow testable without setup.
    /// Failures are caught and logged by the callers.
    /// </summary>
    public bool IsInstalled => true;

    public bool HasPendingUpdate => _pending is not null;

    /// <summary>
    /// Always null: the basic backend has no cross-session prepared
    /// state (nothing survives a restart to prompt for).
    /// </summary>
    public string? PendingRestartVersion => null;

    private HttpClient Http
    {
        get
        {
            lock (_lock)
            {
                if (_http is not null)
                    return _http;
                _http = _testHandler is null
                    ? new HttpClient()
                    : new HttpClient(_testHandler, disposeHandler: false);
                _http.DefaultRequestHeaders.UserAgent.ParseAdd(AppMetadata.UserAgent);
                _http.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                string token = ResolveToken();
                if (!string.IsNullOrWhiteSpace(token))
                    _http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                return _http;
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
        lock (_lock)
        {
            AppLog.Information("Basic update channel set to {Channel}", channel);
            _channel = channel;
            _pending = null;
            _downloadedPath = null;
        }
    }

    /// <summary>
    /// Queries the releases API and stashes the newest qualifying release
    /// as pending. Throws on network/API errors (callers surface them).
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync()
    {
        string channel;
        lock (_lock)
        {
            channel = _channel;
        }

        (Version currentNumeric, bool currentPrerelease) = CurrentVersionParts();
        (string owner, string repo) = RepoParts();

        string url = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=100";
        using var response = await Http.GetAsync(url, CancellationToken.None);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(CancellationToken.None);

        PendingRelease? best = null;
        using (var doc = JsonDocument.Parse(json))
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Unexpected GitHub releases response (not an array).");
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var candidate = ParseCandidate(element, channel, currentNumeric, currentPrerelease);
                if (candidate is null)
                    continue;
                if (best is null || CompareReleases(candidate, best) > 0)
                    best = candidate;
            }
        }

        lock (_lock)
        {
            _pending = best;
            if (best is not null)
                _downloadedPath = null;
        }

        if (best is null)
            return new UpdateCheckResult(false, null);
        AppLog.Information("Basic update available: {Version} ({Tag})", best.Version, best.Tag);
        return new UpdateCheckResult(true, best.Version, best.Notes);
    }

    /// <summary>
    /// Downloads the pending release asset with progress. No-op when
    /// nothing is pending.
    /// </summary>
    public async Task DownloadPendingUpdateAsync(Action<int>? progress = null)
    {
        PendingRelease? pending;
        lock (_lock)
        {
            pending = _pending;
        }

        if (pending is null)
            return;

        string dir = Path.Combine(Path.GetTempPath(), AppMetadata.AppDataFolder, "updates", pending.Tag);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, pending.FileName);

        progress?.Invoke(0);
        using (var response = await Http.GetAsync(pending.DownloadUrl, CancellationToken.None))
        {
            response.EnsureSuccessStatusCode();
            long? total = response.Content.Headers.ContentLength;
            await using var content = await response.Content.ReadAsStreamAsync(CancellationToken.None);
            await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            long read = 0;
            int count;
            while ((count = await content.ReadAsync(buffer, CancellationToken.None)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, count), CancellationToken.None);
                read += count;
                if (total > 0)
                    progress?.Invoke((int)Math.Min(100, (read * 100L) / total.Value));
            }
        }

        progress?.Invoke(100);
        lock (_lock)
        {
            _downloadedPath = path;
        }

        AppLog.Information("Basic update downloaded to {Path}", path);
    }

    /// <summary>
    /// Launches the downloaded installer and exits. Throws when nothing
    /// was downloaded (callers surface it).
    /// </summary>
    public void ApplyPendingUpdateAndRestart()
    {
        string? path;
        lock (_lock)
        {
            path = _downloadedPath;
        }

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new InvalidOperationException("No downloaded update to apply. Download it first.");

        using var _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        Environment.Exit(0);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _http?.Dispose();
            _http = null;
        }

        GC.SuppressFinalize(this);
    }

    private static readonly SearchValues<char> TagSuffixSeparators =
        SearchValues.Create(new[] { '-', '+' });

    private sealed record PendingRelease(string Tag, string Version, string DownloadUrl, string FileName, string? Notes);

    private static (Version Numeric, bool Prerelease) CurrentVersionParts()
    {
        if (!TryParseReleaseVersion(AppInfo.Current.Version, out Version? numeric, out _))
            numeric = new Version(0, 0, 0);
        bool prerelease = ChannelResolver.IsBetaVersion(AppInfo.Current.InformationalVersion);
        return (numeric, prerelease);
    }

    private static (string Owner, string Repo) RepoParts()
    {
        try
        {
            var segments = new Uri(AppMetadata.RepoUrl).AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
                return (segments[^2], segments[^1]);
        }
        catch { }

        throw new InvalidOperationException(
            $"Cannot derive owner/repo from RepoUrl '{AppMetadata.RepoUrl}'. Set it in Services/Helpers/AppMetadata.cs.");
    }

    private static PendingRelease? ParseCandidate(
        JsonElement element, string channel, Version currentNumeric, bool currentPrerelease)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (IsTrue(element, "draft"))
            return null;
        if (!TryGetString(element, "tag_name", out string? tag) ||
            !TryParseReleaseVersion(tag, out Version? numeric, out bool tagPrerelease))
            return null;

        bool isPrerelease = IsTrue(element, "prerelease") || tagPrerelease;
        if (isPrerelease && channel != ChannelResolver.Beta)
            return null;
        if (!IsNewer(numeric, isPrerelease, currentNumeric, currentPrerelease))
            return null;
        if (!TryFindExeAsset(element, out string? downloadUrl, out string? fileName))
        {
            AppLog.Information("Basic update skipped {Tag}: no .exe asset attached", tag);
            return null;
        }

        string version = $"{numeric.Major}.{numeric.Minor}.{numeric.Build}";
        string? notes = null;
        if (TryGetString(element, "body", out string? body) && !string.IsNullOrWhiteSpace(body))
            notes = body.Trim();
        return new PendingRelease(tag, version, downloadUrl, fileName, notes);
    }

    /// <summary>Higher numeric wins; a stable release supersedes the same-number prerelease.</summary>
    private static bool IsNewer(Version numeric, bool isPrerelease, Version currentNumeric, bool currentPrerelease)
    {
        int order = numeric.CompareTo(currentNumeric);
        if (order != 0)
            return order > 0;
        return currentPrerelease && !isPrerelease;
    }

    /// <summary>Higher numeric wins; ties prefer the stable release.</summary>
    private static int CompareReleases(PendingRelease left, PendingRelease right)
    {
        _ = Version.TryParse(left.Version, out Version? l);
        _ = Version.TryParse(right.Version, out Version? r);
        return (l ?? new Version(0, 0, 0)).CompareTo(r ?? new Version(0, 0, 0));
    }

    private static bool TryFindExeAsset(
        JsonElement element,
        [NotNullWhen(true)] out string? downloadUrl,
        [NotNullWhen(true)] out string? fileName)
    {
        downloadUrl = null;
        fileName = null;
        if (!element.TryGetProperty("assets", out JsonElement assets) ||
            assets.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var asset in assets.EnumerateArray())
        {
            if (!TryGetString(asset, "name", out string? name) ||
                !TryGetString(asset, "browser_download_url", out string? url))
                continue;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(url))
                continue;
            downloadUrl = url;
            fileName = Path.GetFileName(name);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "setup.exe";
            return true;
        }

        return false;
    }

    private static bool TryParseReleaseVersion(
        string? tag,
        [NotNullWhen(true)] out Version? numeric,
        out bool prerelease)
    {
        numeric = null;
        prerelease = false;
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        string rest = tag.Trim();
        if (rest.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            rest = rest[1..];
        int cut = rest.AsSpan().IndexOfAny(TagSuffixSeparators);
        string suffix = cut >= 0 ? rest[cut..] : string.Empty;
        string core = cut >= 0 ? rest[..cut] : rest;
        prerelease = suffix.Length > 0;
        if (!Version.TryParse(core, out Version? parsed))
            return false;

        // Ignore Revision: assembly "0.0.3.0" and tag "v0.0.3" are the same release.
        numeric = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        return true;
    }

    private static bool IsTrue(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value))
            return false;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => string.Equals(
                value.GetString(), "true", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static bool TryGetString(
        JsonElement element, string property,
        [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (!element.TryGetProperty(property, out JsonElement found) ||
            found.ValueKind != JsonValueKind.String)
            return false;
        value = found.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
