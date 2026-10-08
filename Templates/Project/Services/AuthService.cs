using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DevTemWinUi3.Services.Abstractions;

namespace DevTemWinUi3.Services;

/// <summary>
/// Identity seam: SDK-free veneer, so every call site compiles for every
/// scaffold and degrades to a no-op when the auth feature is off. The MSAL
/// backend lives in <see cref="MsalAuthProvider"/> (scaffolded only with
/// the auth feature). Disabled twice by default: the template ships
/// <c>--auth false</c>, and an empty client id (the default) keeps even an
/// auth-enabled build inert until the owner registers an app.
/// </summary>
public sealed class AuthService
{
    public static AuthService Current { get; } = new();
    private AuthService() { }

    private readonly object _lock = new();
#pragma warning disable CA1859 // Seam by design: the interface decouples call sites from the (scaffold-optional) MSAL backend.
    private IAuthProvider? _provider;
#pragma warning restore CA1859
    private AuthAccount? _account;
    private Func<IntPtr>? _windowHandleProvider;

    /// <summary>Raised on the calling thread when sign-in state changes.</summary>
    public event EventHandler? AccountChanged;

    /// <summary>Last known signed-in account (in-memory only). Null when signed out.</summary>
    public AuthAccount? CurrentAccount
    {
        get { lock (_lock) { return _account; } }
    }

    /// <summary>Whether sign-in can actually run (feature on, backend created).</summary>
    public bool IsEnabled
    {
        get { lock (_lock) { return _provider is not null; } }
    }

    /// <summary>
    /// Creates the backend when the scaffold includes auth. Idempotent and
    /// never throws. Call once at startup; <paramref name="windowHandleProvider"/>
    /// parents the interactive UI (MainWindow passes its HWND).
    /// </summary>
    public void Initialize(Func<IntPtr>? windowHandleProvider = null)
    {
        try
        {
            if (!AppFeatures.Auth)
            {
                AppLog.Information("Auth disabled: scaffolded without the auth feature");
                return;
            }
        }
        catch
        {
            return;
        }
        lock (_lock)
        {
            if (_provider is not null)
                return;
            try
            {
                _windowHandleProvider = windowHandleProvider;
#if (auth)
                _provider = MsalAuthProvider.TryCreate();
#endif
                if (_provider is null)
                {
                    AppLog.Information("Auth disabled: no client id configured");
                    return;
                }
                AppLog.Information("Auth enabled (Entra ID)");
            }
            catch (Exception ex)
            {
                _provider = null;
                AppLog.Error(ex, "Auth failed to initialize");
            }
        }
    }

    /// <summary>
    /// Silent resume from the persisted cache (best-effort, e.g. at startup).
    /// Null when disabled, empty, or interaction is required. Never throws.
    /// </summary>
    public async Task<AuthAccount?> TryResumeAsync()
    {
        IAuthProvider? provider;
        lock (_lock) { provider = _provider; }
        if (provider is null)
            return null;
        try
        {
            var account = await provider.TryResumeAsync().ConfigureAwait(false);
            SetAccount(account);
            return account;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Auth resume failed");
            return null;
        }
    }

    /// <summary>
    /// Interactive sign-in. Null when disabled, cancelled, or unavailable.
    /// Never throws.
    /// </summary>
    public async Task<AuthAccount?> SignInAsync()
    {
        IAuthProvider? provider;
        Func<IntPtr>? handleProvider;
        lock (_lock) { provider = _provider; handleProvider = _windowHandleProvider; }
        if (provider is null)
            return null;
        IntPtr handle = IntPtr.Zero;
        try { handle = handleProvider?.Invoke() ?? IntPtr.Zero; } catch { }
        try
        {
            var account = await provider.SignInAsync(handle).ConfigureAwait(false);
            SetAccount(account);
            if (account is null)
                AppLog.Information("Auth sign-in did not complete");
            return account;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Auth sign-in failed");
            return null;
        }
    }

    /// <summary>Clears the account + persisted cache. Never throws.</summary>
    public async Task SignOutAsync()
    {
        IAuthProvider? provider;
        lock (_lock) { provider = _provider; }
        SetAccount(null);
        if (provider is null)
            return;
        try
        {
            await provider.SignOutAsync().ConfigureAwait(false);
            AppLog.Information("Auth signed out");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Auth sign-out failed");
        }
    }

    private void SetAccount(AuthAccount? account)
    {
        lock (_lock) { _account = account; }
        try { AccountChanged?.Invoke(this, EventArgs.Empty); } catch { }
    }

    /// <summary>
    /// Auth status attached to diagnostics (booleans only — never identity
    /// values). Pure and headless-testable.
    /// </summary>
    internal static Dictionary<string, string> BuildStatusContext()
    {
        var context = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            context["authEnabled"] = Current.IsEnabled ? "yes" : "no";
            context["signedIn"] = Current.CurrentAccount is null ? "no" : "yes";
        }
        catch { }
        return context;
    }

    /// <summary>
    /// MSAL cache file for a data folder. Pure and headless-testable; the
    /// backend resolves the folder from <see cref="AppPaths"/> per
    /// distribution (portable vs packaged).
    /// </summary>
    internal static string CacheFilePathFor(string dataFolder) =>
        Path.Combine(dataFolder, "msal-cache.dat");

    /// <summary>Disposes the backend and clears in-memory state. Never throws.</summary>
    public void Shutdown()
    {
        lock (_lock)
        {
            try
            {
                _provider?.Dispose();
            }
            catch { }
            _provider = null;
            _account = null;
        }
    }
}
