using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// Sample typed HTTP client (REST): the template's HTTP starting point,
/// registered via <c>AddHttpClient</c> (database feature). It ships with no
/// production call sites on purpose — point <c>BaseAddress</c> at your API
/// and call it from a ViewModel, or delete this file (plus its registration
/// and tests) if your app has no backend.
/// </summary>
public sealed class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Base address for API calls.
    /// </summary>
    public Uri? BaseAddress
    {
        get => _httpClient.BaseAddress;
        set => _httpClient.BaseAddress = value;
    }

    /// <summary>
    /// Gets a typed object from the API.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        try
        {
            AppLog.Debug("GET {Url}", url);
            return await _httpClient.GetFromJsonAsync<T>(url);
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "GET {Url} failed", url);
            return default;
        }
    }

    /// <summary>
    /// Posts data to the API and returns a typed response.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        try
        {
            AppLog.Debug("POST {Url}", url);
            var response = await _httpClient.PostAsJsonAsync(url, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "POST {Url} failed", url);
            return default;
        }
    }

    /// <summary>
    /// Posts data to the API (fire-and-forget).
    /// </summary>
    public async Task<bool> PostAsync<TRequest>(string url, TRequest data)
    {
        try
        {
            AppLog.Debug("POST {Url}", url);
            var response = await _httpClient.PostAsJsonAsync(url, data);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "POST {Url} failed", url);
            return false;
        }
    }

    /// <summary>
    /// Gets raw JSON string from the API.
    /// </summary>
    public async Task<string?> GetStringAsync(string url)
    {
        try
        {
            AppLog.Debug("GET (string) {Url}", url);
            return await _httpClient.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "GET (string) {Url} failed", url);
            return null;
        }
    }

    /// <summary>
    /// Creates a JsonSerializerOptions configured for the app.
    /// </summary>
    public static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }
}
