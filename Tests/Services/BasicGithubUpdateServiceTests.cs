using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class BasicGithubUpdateServiceTests
{
    /// <summary>Stubs the releases API (JSON) and the asset download (bytes) by URL.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        public string ReleasesJson = "[]";
        public byte[] DownloadBytes = Encoding.UTF8.GetBytes("fake-setup-bytes");

        /// <summary>
        /// Checksum body served for *.sha256 URLs. Null = serve the correct
        /// digest of <see cref="DownloadBytes"/>; "MISSING" = 404.
        /// </summary>
        public string? ChecksumText;

        /// <summary>Artificial delay (ms) honoring cancellation, for timeout tests.</summary>
        public int DelayMs;

        /// <summary>When true, <see cref="DelayMs"/> applies to the .exe download only.</summary>
        public bool DelayDownloadOnly;

        public HttpRequestMessage? LastApiRequest;

        public bool FailNetwork;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri?.ToString() ?? string.Empty;
            bool isApi = url.Contains("api.github.com", StringComparison.OrdinalIgnoreCase);
            bool isChecksum = url.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase);
            if (DelayMs > 0 && (!DelayDownloadOnly || (!isApi && !isChecksum)))
                await Task.Delay(DelayMs, cancellationToken);
            if (isApi)
            {
                LastApiRequest = request;
                if (FailNetwork)
                    throw new HttpRequestException("offline");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ReleasesJson, Encoding.UTF8, "application/json"),
                };
            }

            if (url.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
            {
                if (ChecksumText == "MISSING")
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                string body = ChecksumText ??
                    Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(DownloadBytes));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/plain"),
                };
            }

            var download = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(DownloadBytes),
            };
            download.Content.Headers.ContentLength = DownloadBytes.Length;
            return download;
        }
    }

    private string _storePath = string.Empty;

    [TestInitialize]
    public void Init()
    {
        _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        LocalSettingsStore.SetTestPath(_storePath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        LocalSettingsStore.SetTestPath(null);
        try { File.Delete(_storePath); } catch { }
        // v99 tags are test-only; never leave fake installers behind.
        try { Directory.Delete(UpdatesDir(), recursive: true); } catch { }
    }

    private static string UpdatesDir() =>
        Path.Combine(Path.GetTempPath(), AppMetadata.AppDataFolder, "updates");

    // v99 tags stay newer than any real current version, so these tests do
    // not depend on the repo's version number. The checksum asset ships by
    // default (the secure release convention); pass checksum: false for the
    // fail-closed tests.
    private static string Release(
        string tag, bool prerelease = false, bool draft = false, string asset = "AcmeSetup.exe", bool checksum = true)
    {
        string assets = "{\"name\":\"" + asset +
            "\",\"browser_download_url\":\"https://example.com/" + asset + "\"}";
        if (checksum)
            assets += ",{\"name\":\"" + asset + ".sha256" +
                "\",\"browser_download_url\":\"https://example.com/" + asset + ".sha256\"}";
        return "{\"tag_name\":\"" + tag + "\",\"prerelease\":" + (prerelease ? "true" : "false") +
        ",\"draft\":" + (draft ? "true" : "false") +
        ",\"assets\":[" + assets + "]}";
    }

    private static string Feed(params string[] releases) => "[" + string.Join(",", releases) + "]";

    [TestMethod]
    public async Task StableChannel_PicksNewestStable_SkipsPrereleaseAndDraft()
    {
        var stub = new StubHandler
        {
            ReleasesJson = Feed(Release("v99.0.1-beta", prerelease: true), Release("v99.0.0"), Release("v99.0.2", draft: true)),
        };
        using var svc = new BasicGithubUpdateService(stub);
        svc.SetChannel("stable"); // Explicit: the test host's default channel is environment-defined.
        var result = await svc.CheckAsync();
        Assert.IsTrue(result.HasUpdate);
        Assert.AreEqual("99.0.0", result.Version);
        Assert.IsTrue(svc.HasPendingUpdate);
    }

    [TestMethod]
    public async Task BetaChannel_IncludesPrerelease()
    {
        var stub = new StubHandler { ReleasesJson = Feed(Release("v99.0.1-beta", prerelease: true)) };
        using var svc = new BasicGithubUpdateService(stub);
        svc.SetChannel("beta");
        var result = await svc.CheckAsync();
        Assert.IsTrue(result.HasUpdate);
        Assert.AreEqual("99.0.1", result.Version);
    }

    [TestMethod]
    public async Task NoUpdate_WhenFeedOlderOrMissingExe()
    {
        var stub = new StubHandler { ReleasesJson = Feed(Release("v0.0.0")) };
        using var svc = new BasicGithubUpdateService(stub);
        var result = await svc.CheckAsync();
        Assert.IsFalse(result.HasUpdate);
        Assert.IsNull(result.Version);
        Assert.IsFalse(svc.HasPendingUpdate);

        stub.ReleasesJson = Feed(Release("v99.0.0", asset: "notes.txt"));
        result = await svc.CheckAsync();
        Assert.IsFalse(result.HasUpdate);
    }

    [TestMethod]
    public async Task NetworkError_Propagates_AndMalformedJson_Throws()
    {
        var stub = new StubHandler { FailNetwork = true };
        using var svc = new BasicGithubUpdateService(stub);
        await Assert.ThrowsAsync<HttpRequestException>(() => svc.CheckAsync());

        stub.FailNetwork = false;
        stub.ReleasesJson = "not json";
        await Assert.ThrowsAsync<JsonException>(() => svc.CheckAsync());
    }

    [TestMethod]
    public async Task Download_WritesFile_ReportsProgress()
    {
        var stub = new StubHandler { ReleasesJson = Feed(Release("v99.0.0")) };
        using var svc = new BasicGithubUpdateService(stub);
        var check = await svc.CheckAsync();
        Assert.IsTrue(check.HasUpdate);

        var seen = new List<int>();
        await svc.DownloadPendingUpdateAsync(seen.Add);
        Assert.Contains(0, seen);
        Assert.Contains(100, seen);

        string path = Path.Combine(UpdatesDir(), "v99.0.0", "AcmeSetup.exe");
        Assert.IsTrue(File.Exists(path));
        Assert.AreEqual(
            Convert.ToBase64String(stub.DownloadBytes),
            Convert.ToBase64String(File.ReadAllBytes(path)));
    }

    [TestMethod]
    public async Task SetChannel_ClearsPending_AndApply_WithoutDownload_Throws()
    {
        var stub = new StubHandler { ReleasesJson = Feed(Release("v99.0.0")) };
        using var svc = new BasicGithubUpdateService(stub);
        await svc.CheckAsync();
        Assert.IsTrue(svc.HasPendingUpdate);
        // Toggle twice: one of them necessarily differs from the ambient
        // channel, so pending is cleared deterministically.
        svc.SetChannel("beta");
        svc.SetChannel("stable");
        Assert.IsFalse(svc.HasPendingUpdate);

        using var fresh = new BasicGithubUpdateService(stub);
        Assert.Throws<InvalidOperationException>(() => fresh.ApplyPendingUpdateAndRestart());
    }

    [TestMethod]
    public async Task Request_CarriesUserAgent()
    {
        var stub = new StubHandler { ReleasesJson = Feed(Release("v0.0.0")) };
        using var svc = new BasicGithubUpdateService(stub);
        await svc.CheckAsync();
        Assert.IsNotNull(stub.LastApiRequest);
        Assert.IsFalse(string.IsNullOrWhiteSpace(
            stub.LastApiRequest!.Headers.UserAgent.ToString()));
    }

    [TestMethod]
    public void ParseChecksum_AcceptsBareAndFilenames_RejectsGarbage()
    {
        string good = "ABCDEF0123456789abcdef0123456789ABCDEF0123456789abcdef0123456789";
        Assert.AreEqual(good.ToUpperInvariant(), BasicGithubUpdateService.ParseChecksum(good));
        Assert.AreEqual(
            good.ToUpperInvariant(),
            BasicGithubUpdateService.ParseChecksum(good.ToLowerInvariant() + "  AcmeSetup.exe"));
        Assert.IsNull(BasicGithubUpdateService.ParseChecksum(null));
        Assert.IsNull(BasicGithubUpdateService.ParseChecksum(""));
        Assert.IsNull(BasicGithubUpdateService.ParseChecksum("short"));
        Assert.IsNull(BasicGithubUpdateService.ParseChecksum(new string('g', 64)));
    }

    [TestMethod]
    public async Task Download_RefusesReleaseWithoutChecksum()
    {
        var stub = new StubHandler { ReleasesJson = Feed(Release("v99.0.0", checksum: false)) };
        using var svc = new BasicGithubUpdateService(stub);
        var check = await svc.CheckAsync();
        Assert.IsTrue(check.HasUpdate);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DownloadPendingUpdateAsync());
        Assert.Contains("sha256", ex.Message);
        Assert.IsFalse(File.Exists(Path.Combine(UpdatesDir(), "v99.0.0", "AcmeSetup.exe")));
    }

    [TestMethod]
    public async Task Download_DiscardsFile_OnChecksumMismatch()
    {
        var stub = new StubHandler
        {
            ReleasesJson = Feed(Release("v99.0.0")),
            ChecksumText = new string('0', 64),
        };
        using var svc = new BasicGithubUpdateService(stub);
        var check = await svc.CheckAsync();
        Assert.IsTrue(check.HasUpdate);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DownloadPendingUpdateAsync());
        Assert.Contains("mismatch", ex.Message);
        Assert.IsFalse(File.Exists(Path.Combine(UpdatesDir(), "v99.0.0", "AcmeSetup.exe")));
    }

    [TestMethod]
    public async Task Download_TimesOut_InsteadOfHanging()
    {
        var stub = new StubHandler
        {
            ReleasesJson = Feed(Release("v99.0.0")),
            DelayMs = 30_000,
            DelayDownloadOnly = true,
        };
        using var svc = new BasicGithubUpdateService(stub);
        var check = await svc.CheckAsync();
        Assert.IsTrue(check.HasUpdate);

        TimeSpan old = BasicGithubUpdateService.DownloadTimeout;
        BasicGithubUpdateService.DownloadTimeout = TimeSpan.FromMilliseconds(100);
        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() => svc.DownloadPendingUpdateAsync());
        }
        finally
        {
            BasicGithubUpdateService.DownloadTimeout = old;
        }
    }
}
