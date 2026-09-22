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
/// qualifying tag, downloads the attached Setup <c>.exe</c>, verifies its
/// SHA-256 against the sibling <c>.exe.sha256</c> asset, and launches it.
/// No update SDK, no installer framework, no background service —
/// one HTTP call per check. Channel mapping: <c>stable</c> ignores
/// prereleases, <c>beta</c> includes them. Process-lifetime singleton
/// (never disposed in practice); implements <see cref="IDisposable"/>
/// to own its <see cref="HttpClient"/> correctly (CA1001).
/// <para/>
/// Release convention (documented in
/// <c>docs/feature-guides/updates-basic.md</c>): attach the installer as a
/// <c>.exe</c> asset plus a <c>.exe.sha256</c> checksum file (hex digest,
/// bare or "<c>hex  filename</c>") to the GitHub release; tags look like
/// <c>v0.0.4</c> or <c>v0.0.4-beta</c>. Releases without the checksum are
/// refused: an unverified executable is never launched.
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
    /// Set for the last download whose SHA-256 verified. Apply launches
    /// ONLY this path: a present-but-unverified file is never executed.
    /// </summary>
    private string? _verifiedPath;

    /// <summary>Releases-API and checksum-fetch budget. Never infinite.</summary>
    internal static TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whole-download budget (large installers on slow links). Generous
    /// but finite: a stalled download degrades to an error, never a hang.
    /// </summary>
    internal static TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(30);

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

    /// <summary>
    /// Always true: the basic backend needs no initialization (symmetry
    /// with <see cref="UpdateService.IsInitialized"/> for shared callers).
    /// </summary>
    public bool IsInitialized => true;

    /// <summary>No-op: nothing to initialize (see <see cref="IsInitialized"/>).</summary>
    public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

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
                // Per-request CancellationTokenSources own every timeout
                // (CheckTimeout / DownloadTimeout below), so the client
                // itself must not impose HttpClient's 100s default — it
                // would abort large downloads mid-stream on slow links.
                _http.Timeout = Timeout.InfiniteTimeSpan;
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
            _verifiedPath = null;
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
        using var cts = new CancellationTokenSource(CheckTimeout);
        CancellationToken token = cts.Token;
        using var response = await Http.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync(token);

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
            {
                _downloadedPath = null;
                _verifiedPath = null;
            }
        }

        if (best is null)
            return new UpdateCheckResult(false, null);
        AppLog.Information("Basic update available: {Version} ({Tag})", best.Version, best.Tag);
        return new UpdateCheckResult(true, best.Version, best.Notes);
    }

    /// <summary>
    /// Downloads the pending release asset, verifies its SHA-256 against
    /// the sibling <c>.exe.sha256</c> asset, and stages it for launch.
    /// No-op when nothing is pending. Fails closed: a missing checksum, a
    /// mismatch, or a timeout throws (callers surface it) and any partial
    /// file is deleted — an unverified executable is never staged.
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

        if (string.IsNullOrWhiteSpace(pending.ChecksumUrl))
            throw new InvalidOperationException(
                $"Release {pending.Tag} has no {pending.FileName}.sha256 checksum asset. " +
                "Refusing an unverified installer (see docs/feature-guides/updates-basic.md).");

        string expected = await FetchChecksumAsync(pending, CancellationToken.None);

        string dir = Path.Combine(Path.GetTempPath(), AppMetadata.AppDataFolder, "updates", pending.Tag);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, pending.FileName);

        using var cts = new CancellationTokenSource(DownloadTimeout);
        CancellationToken token = cts.Token;
        progress?.Invoke(0);
        string actual;
        try
        {
            using (var response = await Http.GetAsync(pending.DownloadUrl, token))
            {
                response.EnsureSuccessStatusCode();
                long? total = response.Content.Headers.ContentLength;
                await using var content = await response.Content.ReadAsStreamAsync(token);
                await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using var sha = System.Security.Cryptography.IncrementalHash.CreateHash(
                    System.Security.Cryptography.HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                long read = 0;
                int count;
                while ((count = await content.ReadAsync(buffer, token)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, count), token);
                    sha.AppendData(buffer, 0, count);
                    read += count;
                    if (total > 0)
                        progress?.Invoke((int)Math.Min(100, (read * 100L) / total.Value));
                }

                await file.FlushAsync(token);
                actual = Convert.ToHexString(sha.GetHashAndReset());
            }
            // Streams are closed here, so a mismatch delete cannot race an
            // open handle (FileShare.None would refuse it).
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(path); } catch { }
                throw new InvalidOperationException(
                    $"Release {pending.Tag} checksum mismatch (expected {expected}, got {actual}). " +
                    "The download was discarded; the installer was not staged.");
            }
        }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
        {
            // The CTS above is the only token that can cancel here.
            throw new TimeoutException(
                $"Download of release {pending.Tag} timed out after {DownloadTimeout.TotalMinutes:F0} minutes.", ex);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Download of release {pending.Tag} timed out after {DownloadTimeout.TotalMinutes:F0} minutes.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            throw;
        }

        progress?.Invoke(100);
        lock (_lock)
        {
            _downloadedPath = path;
            _verifiedPath = path;
        }

        AppLog.Information("Basic update downloaded and verified: {Path}", path);
    }

    /// <summary>
    /// Fetches and parses the sibling checksum file: first whitespace-
    /// delimited token must be 64 hex chars (bare digest or
    /// "<c>hex  filename</c>" form). Pure apart from the fetch.
    /// </summary>
    private async Task<string> FetchChecksumAsync(PendingRelease pending, CancellationToken outer)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(outer);
        cts.CancelAfter(CheckTimeout);
        string text;
        try
        {
            using var response = await Http.GetAsync(pending.ChecksumUrl!, cts.Token);
            response.EnsureSuccessStatusCode();
            text = await response.Content.ReadAsStringAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"Checksum fetch for release {pending.Tag} timed out after {CheckTimeout.TotalSeconds:F0}s.");
        }

        string? digest = ParseChecksum(text);
        if (digest is null)
            throw new InvalidOperationException(
                $"Release {pending.Tag} has an unreadable {pending.FileName}.sha256 checksum asset. " +
                "Refusing an unverified installer (see docs/feature-guides/updates-basic.md).");
        return digest;
    }

    /// <summary>
    /// Parses a checksum file body to its hex digest, or null. Pure and
    /// headless-testable.
    /// </summary>
    internal static string? ParseChecksum(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        string token = text.Trim().Split(ChecksumSeparators, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;
        if (token.Length != 64)
            return null;
        foreach (char c in token)
        {
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex)
                return null;
        }

        return token.ToUpperInvariant();
    }

    /// <summary>
    /// Launches the downloaded, hash-verified installer and exits. Throws
    /// when nothing was downloaded AND verified (callers surface it): a
    /// present-but-unverified file is never executed.
    /// </summary>
    public void ApplyPendingUpdateAndRestart()
    {
        string? path;
        string? verified;
        lock (_lock)
        {
            path = _downloadedPath;
            verified = _verifiedPath;
        }

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new InvalidOperationException("No downloaded update to apply. Download it first.");
        if (!string.Equals(path, verified, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Downloaded update failed verification and was discarded. Check for updates again.");

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

    private static readonly char[] ChecksumSeparators = { ' ', '\t', '\r', '\n' };

    private sealed record PendingRelease(
        string Tag, string Version, string DownloadUrl, string FileName, string? Notes, string? ChecksumUrl);

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
        if (!TryFindExeAsset(element, out string? downloadUrl, out string? fileName, out string? checksumUrl))
        {
            AppLog.Information("Basic update skipped {Tag}: no .exe asset attached", tag);
            return null;
        }

        string version = $"{numeric.Major}.{numeric.Minor}.{numeric.Build}";
        string? notes = null;
        if (TryGetString(element, "body", out string? body) && !string.IsNullOrWhiteSpace(body))
            notes = body.Trim();
        return new PendingRelease(tag, version, downloadUrl, fileName, notes, checksumUrl);
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

    /// <summary>
    /// Finds the installer asset plus its sibling checksum file
    /// (<c>{name}.sha256</c>). The checksum URL may be null (release
    /// predates the convention) — the download refuses those releases.
    /// </summary>
    private static bool TryFindExeAsset(
        JsonElement element,
        [NotNullWhen(true)] out string? downloadUrl,
        [NotNullWhen(true)] out string? fileName,
        out string? checksumUrl)
    {
        downloadUrl = null;
        fileName = null;
        checksumUrl = null;
        if (!element.TryGetProperty("assets", out JsonElement assets) ||
            assets.ValueKind != JsonValueKind.Array)
            return false;

        string? exeUrl = null;
        string? exeName = null;
        var checksumByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in assets.EnumerateArray())
        {
            if (!TryGetString(asset, "name", out string? name) ||
                !TryGetString(asset, "browser_download_url", out string? url))
                continue;
            if (string.IsNullOrWhiteSpace(url))
                continue;
            if (name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
            {
                checksumByName[name] = url;
                continue;
            }

            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;
            if (exeUrl is null)
            {
                exeUrl = url;
                exeName = Path.GetFileName(name);
                if (string.IsNullOrWhiteSpace(exeName))
                    exeName = "setup.exe";
            }
        }

        if (exeUrl is null || exeName is null)
            return false;

        downloadUrl = exeUrl;
        fileName = exeName;
        checksumByName.TryGetValue(exeName + ".sha256", out checksumUrl);
        return true;
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
