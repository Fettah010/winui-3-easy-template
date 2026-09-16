# Typed HTTP client

This scaffold includes `ApiService`, `IHttpClientFactory`, the HTTP package
reference, and API service tests. Configure the API base address in the
application layer; do not place credentials in generated source.

## Resilience kit (P2-2)

- `ExponentialRetryHandler` (in `Services/ApiService.cs`) retries network
  failures and 408/429/5xx with exponential backoff (200 ms base, 3
  retries), honors cancellation, and never re-sends a request whose content
  cannot be re-sent. It is wired in `ServiceLocator.AddHttp` via
  `AddHttpMessageHandler`. Deliberately in-repo instead of Polly: zero
  package weight for a starter template (see `docs/DECISIONS.md`).
- Every `ApiService` method has a `CancellationToken` overload — plumb the
  page's token through instead of fire-and-forget.
- New call sites should prefer `GetResultAsync<T>`: it returns a typed
  `ApiResult<T>` (success / value / status / error / canceled) instead of
  collapsing cancel-vs-404-vs-500 to `default`. The legacy nullable
  methods keep the never-throw promise at the template's own call sites.
- One typed client per backend: register additional backends with their own
  `AddHttpClient<TBackend>` next to `AddHttp`, each with its own timeout
  and handler chain.
