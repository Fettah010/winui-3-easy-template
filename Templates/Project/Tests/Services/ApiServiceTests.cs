using System;
using System.Net.Http;
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

    private class TestResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
