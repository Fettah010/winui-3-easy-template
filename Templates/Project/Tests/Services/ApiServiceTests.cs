using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DevTemWinUi3.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DevTemWinUi3.Tests.Services;

[TestClass]
public class ApiServiceTests
{
    [TestMethod]
    public void CreateJsonOptions_ReturnsConfiguredOptions()
    {
        var options = ApiService.CreateJsonOptions();
        Assert.IsNotNull(options);
        Assert.IsNotNull(options.PropertyNamingPolicy);
        Assert.IsTrue(options.PropertyNameCaseInsensitive);
    }

    [TestMethod]
    public void Constructor_WithHttpClient_Succeeds()
    {
        using var client = new HttpClient();
        var api = new ApiService(client);
        Assert.IsNotNull(api);
    }

    [TestMethod]
    public async Task GetStringAsync_InvalidUrl_ReturnsNull()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") };
        client.Timeout = TimeSpan.FromMilliseconds(500);
        var api = new ApiService(client);
        var result = await api.GetStringAsync("/test");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetAsync_InvalidUrl_ReturnsNull()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") };
        client.Timeout = TimeSpan.FromMilliseconds(500);
        var api = new ApiService(client);
        var result = await api.GetAsync<TestResponse>("/test");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task PostAsync_InvalidUrl_ReturnsFalse()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") };
        client.Timeout = TimeSpan.FromMilliseconds(500);
        var api = new ApiService(client);
        var result = await api.PostAsync("/test", new { });
        Assert.IsFalse(result);
    }

    private sealed class TestResponse
    {
        public string Url { get; set; } = string.Empty;
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _script;
        public int Calls;

        public ScriptedHandler(Func<int, HttpResponseMessage> script) => _script = script;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_script(Calls));
        }
    }

    private static HttpResponseMessage JsonOk(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json) };

    [TestMethod]
    public async Task RetryHandler_FlakyThenOk_Succeeds()
    {
        // P2-2: 2x500 then 200 resolves to success within the attempt cap.
        var flaky = new ScriptedHandler(call =>
            call < 3
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : JsonOk("{\"url\":\"x\"}"));
        var retry = new ExponentialRetryHandler { InnerHandler = flaky };
        using var client = new HttpClient(retry) { BaseAddress = new Uri("http://localhost/") };
        var api = new ApiService(client);

        var result = await api.GetResultAsync<TestResponse>("/thing");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("x", result.Value.Url);
        Assert.IsGreaterThanOrEqualTo(3, retry.LastAttempts);
        Assert.IsLessThanOrEqualTo(ExponentialRetryHandler.MaxRetries + 1, retry.LastAttempts);
    }

    [TestMethod]
    public async Task RetryHandler_Persistent500_StopsAtCap()
    {
        var down = new ScriptedHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var retry = new ExponentialRetryHandler { InnerHandler = down };
        using var client = new HttpClient(retry) { BaseAddress = new Uri("http://localhost/") };
        var api = new ApiService(client);

        var result = await api.GetResultAsync<TestResponse>("/thing");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.AreEqual(ExponentialRetryHandler.MaxRetries + 1, retry.LastAttempts);
    }

    [TestMethod]
    public async Task GetResultAsync_Cancelled_ReportsCanceled()
    {
        // P2-2: cancellation arrives typed, not swallowed to default.
        var slow = new ScriptedHandler(_ => JsonOk("{}"));
        using var client = new HttpClient(slow) { BaseAddress = new Uri("http://localhost/") };
        var api = new ApiService(client);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await api.GetResultAsync<TestResponse>("/thing", cts.Token);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Canceled);
    }

    [TestMethod]
    public async Task GetResultAsync_NotFound_ReportsStatus()
    {
        var missing = new ScriptedHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(missing) { BaseAddress = new Uri("http://localhost/") };
        var api = new ApiService(client);

        var result = await api.GetResultAsync<TestResponse>("/thing");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.Canceled);
        Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Error));
    }
}
