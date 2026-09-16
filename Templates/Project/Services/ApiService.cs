using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DevTemWinUi3.Services;

/// <summary>
/// Typed API result for the non-swallowing surface
/// (<see cref="ApiService.GetResultAsync{T}"/>): transports cancel,
/// HTTP-error, and network-error distinctly instead of collapsing them to
/// <c>default</c> (performance plan P2-2). The legacy nullable methods
/// below keep the template's never-throw promise at its own call sites.
/// </summary>
public sealed record ApiResult<T>(
    bool Success,
    T? Value,
    HttpStatusCode? StatusCode,
    string? Error,
    bool Canceled);

/// <summary>
/// Minimal exponential-backoff retry handler (performance plan P2-2):
/// in-repo instead of Polly (zero package weight for a starter template).
/// Retries network failures and 408/429/5xx, up to
/// <see cref="MaxRetries"/> times; honors cancellation; never retries a
/// request whose content cannot be re-sent. Add it via
/// <c>AddHttpMessageHandler&lt;ExponentialRetryHandler&gt;</c> (wired in
/// <c>ServiceLocator.AddHttp</c>).
/// </summary>
public sealed class ExponentialRetryHandler : DelegatingHandler
{
    /// <summary>Retries after the initial attempt (3 total tries).</summary>
    public const int MaxRetries = 3;

    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>Attempts performed by the last send (tests pin the policy).</summary>
    public int LastAttempts { get; private set; } = 1;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastAttempts = 1;
        HttpResponseMessage? response = null;
        for (int attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastAttempts = attempt + 1;
            bool canRetry = attempt < MaxRetries
                && (request.Content is null || attempt == 0);
            try
            {
                response?.Dispose();
                response = null;
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!IsRetryableStatus(response.StatusCode) || !canRetry)
                    return response;
            }
            catch (HttpRequestException) when (canRetry)
            {
                // Network failure before/without a response: back off and retry.
            }
            if (!canRetry)
            {
                response?.Dispose();
                throw new HttpRequestException("Request failed and is not retryable.");
            }
            try
            {
                await Task.Delay(Backoff(attempt), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                response?.Dispose();
                throw;
            }
        }
    }

    internal static bool IsRetryableStatus(HttpStatusCode status) =>
        status == HttpStatusCode.RequestTimeout
        || status == (HttpStatusCode)429
        || ((int)status >= 500 && (int)status <= 599);

    internal static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * (1 << Math.Min(attempt, 4)));
}

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
    public Task<T?> GetAsync<T>(string url) => GetAsync<T>(url, CancellationToken.None);

    /// <summary>
    /// Gets a typed object from the API, honoring
    /// <paramref name="cancellationToken"/> (P2-2).
    /// </summary>
    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        try
        {
            AppLog.Debug("GET {Url}", url);
            return await _httpClient.GetFromJsonAsync<T>(url, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "GET {Url} failed", url);
            return default;
        }
    }

    /// <summary>
    /// Typed GET without swallowing (P2-2): cancel, HTTP-error, and
    /// network-error arrive distinctly in <see cref="ApiResult{T}"/>.
    /// New call sites should prefer this over <see cref="GetAsync{T}(string)"/>.
    /// </summary>
    public async Task<ApiResult<T>> GetResultAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            AppLog.Debug("GET {Url}", url);
            using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<T>(false, default, response.StatusCode, $"GET {url} failed: {(int)response.StatusCode}", false);
            var value = await response.Content.ReadFromJsonAsync<T>(
                CreateJsonOptions(), cancellationToken).ConfigureAwait(false);
            return new ApiResult<T>(true, value, response.StatusCode, null, false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return new ApiResult<T>(false, default, null, ex.Message, true);
        }
        catch (HttpRequestException ex)
        {
            AppLog.Warning(ex, "GET {Url} failed", url);
            return new ApiResult<T>(false, default, ex.StatusCode, ex.Message, false);
        }
        catch (Exception ex)
        {
            AppLog.Warning(ex, "GET {Url} failed", url);
            return new ApiResult<T>(false, default, null, ex.Message, false);
        }
    }

    /// <summary>
    /// Posts data to the API and returns a typed response.
    /// </summary>
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data) =>
        PostAsync<TRequest, TResponse>(url, data, CancellationToken.None);

    /// <summary>
    /// Posts data to the API and returns a typed response, honoring
    /// <paramref name="cancellationToken"/> (P2-2).
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url, TRequest data, CancellationToken cancellationToken)
    {
        try
        {
            AppLog.Debug("POST {Url}", url);
            var response = await _httpClient.PostAsJsonAsync(url, data, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);
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
    public Task<bool> PostAsync<TRequest>(string url, TRequest data) =>
        PostAsync(url, data, CancellationToken.None);

    /// <summary>
    /// Posts data to the API (fire-and-forget), honoring
    /// <paramref name="cancellationToken"/> (P2-2).
    /// </summary>
    public async Task<bool> PostAsync<TRequest>(
        string url, TRequest data, CancellationToken cancellationToken)
    {
        try
        {
            AppLog.Debug("POST {Url}", url);
            var response = await _httpClient.PostAsJsonAsync(url, data, cancellationToken).ConfigureAwait(false);
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
    public Task<string?> GetStringAsync(string url) => GetStringAsync(url, CancellationToken.None);

    /// <summary>
    /// Gets raw JSON string from the API, honoring
    /// <paramref name="cancellationToken"/> (P2-2).
    /// </summary>
    public async Task<string?> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            AppLog.Debug("GET (string) {Url}", url);
            return await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
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
