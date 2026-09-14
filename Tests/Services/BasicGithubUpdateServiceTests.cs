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
        public HttpRequestMessage? LastApiRequest;
        public bool FailNetwork;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string url = request.RequestUri?.ToString() ?? string.Empty;
            if (url.Contains("api.github.com", StringComparison.OrdinalIgnoreCase))
            {
                LastApiRequest = request;
                if (FailNetwork)
                    throw new HttpRequestException("offline");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(ReleasesJson, Encoding.UTF8, "application/json"),
                });
            }

            var download = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(DownloadBytes),
            };
            download.Content.Headers.ContentLength = DownloadBytes.Length;
            return Task.FromResult(download);
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
    // not depend on the repo's version number.
    private static string Release(
        string tag, bool prerelease = false, bool draft = false, string asset = "AcmeSetup.exe") =>
        "{\"tag_name\":\"" + tag + "\",\"prerelease\":" + (prerelease ? "true" : "false") +
        ",\"draft\":" + (draft ? "true" : "false") +
        ",\"assets\":[{\"name\":\"" + asset +
        "\",\"browser_download_url\":\"https://example.com/" + asset + "\"}]}";

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
}
