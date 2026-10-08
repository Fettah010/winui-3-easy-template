using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DevTemWinUi3.Services.Abstractions;
using DevTemWinUi3.Services.Configuration;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace DevTemWinUi3.Services;

/// <summary>
/// MSAL.NET owner behind <see cref="AuthService"/>: created only when a
/// client id is configured. Scaffolded only with the auth feature; without
/// it the service veneer is a no-op and this file (plus the MSAL package)
/// is absent. Never throws.
/// Spike verdict (WB-02, see DECISIONS): broker (WAM) where the app has
/// package identity, system-browser loopback everywhere else — attempted in
/// that order, falling back automatically. Either way the cache is an MSAL
/// v3 blob in a DPAPI file under <see cref="AppPaths"/> (per distribution),
/// never custom storage, and identity values never reach logs.
/// </summary>
internal sealed class MsalAuthProvider : IAuthProvider
{
    internal static MsalAuthProvider? TryCreate()
    {
        try
        {
            string clientId = DeploymentConfiguration.EntraClientId;
            if (string.IsNullOrWhiteSpace(clientId))
                return null;
            string authority = DeploymentConfiguration.EntraAuthority;
            if (string.IsNullOrWhiteSpace(authority))
                authority = "https://login.microsoftonline.com/common";
            string[] scopes = SplitScopes(DeploymentConfiguration.EntraScopes);
            if (scopes.Length == 0)
                scopes = new[] { "User.Read" };
            // Broker needs package identity; unpackaged runs would need an
            // explicit SID registration per install, so they start on the
            // loopback path and only broker when packaged.
            bool preferBroker = AppInfo.IsPackaged;
            var provider = new MsalAuthProvider(clientId.Trim(), authority.Trim(), scopes, preferBroker);
            if (!provider.Rebuild(preferBroker))
                return null;
            return provider;
        }
        catch
        {
            return null;
        }
    }

    private static readonly char[] s_scopeSeparators = { ' ', ';', ',' };

    internal static string[] SplitScopes(string? raw)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();
            return raw.Split(s_scopeSeparators, StringSplitOptions.RemoveEmptyEntries);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private readonly string _clientId;
    private readonly string _authority;
    private readonly string[] _scopes;
    private IPublicClientApplication? _app;
    private IAccount? _account;
    private bool _disposed;

    private MsalAuthProvider(string clientId, string authority, string[] scopes, bool preferBroker)
    {
        _clientId = clientId;
        _authority = authority;
        _scopes = scopes;
        PreferBroker = preferBroker;
    }

    internal bool PreferBroker { get; private set; }

    private bool Rebuild(bool withBroker)
    {
        try
        {
            var builder = PublicClientApplicationBuilder.Create(_clientId)
                .WithAuthority(_authority)
                .WithRedirectUri(withBroker
                    ? "ms-appx-web://microsoft.aad.brokerplugin/" + _clientId
                    : "http://localhost");
            if (withBroker)
            {
                try
                {
                    // Desktop broker (WAM) lives in the Broker extension
                    // package (MSAL 4.61+): single SSO-capable path when the
                    // app has package identity.
                    builder = builder.WithBroker(
                        new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
                }
                catch
                {
                    withBroker = false;
                }
            }
            _app = builder.Build();
            PreferBroker = withBroker;
            WireCache(_app.UserTokenCache);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WireCache(ITokenCache cache)
    {
        try
        {
            cache.SetBeforeAccess(BeforeAccess);
            cache.SetAfterAccess(AfterAccess);
        }
        catch { }
    }

    private static void BeforeAccess(TokenCacheNotificationArgs args)
    {
        try
        {
            string path = AuthService.CacheFilePathFor(AppPaths.DataFolder);
            if (!File.Exists(path))
                return;
            byte[] protectedBytes = File.ReadAllBytes(path);
            byte[] plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            args.TokenCache.DeserializeMsalV3(plain, shouldClearExistingCache: true);
        }
        catch { }
    }

    private static void AfterAccess(TokenCacheNotificationArgs args)
    {
        try
        {
            if (!args.HasStateChanged)
                return;
            string path = AuthService.CacheFilePathFor(AppPaths.DataFolder);
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            byte[] plain = args.TokenCache.SerializeMsalV3();
            File.WriteAllBytes(path, ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser));
        }
        catch { }
    }

    public async Task<AuthAccount?> TryResumeAsync()
    {
        try
        {
            if (_disposed || _app is null)
                return null;
            var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
            _account = accounts.FirstOrDefault();
            if (_account is null)
                return null;
            try
            {
                var result = await _app.AcquireTokenSilent(_scopes, _account)
                    .ExecuteAsync().ConfigureAwait(false);
                return FromResult(result);
            }
            catch (MsalUiRequiredException)
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public async Task<AuthAccount?> SignInAsync(IntPtr windowHandle)
    {
        try
        {
            if (_disposed || _app is null)
                return null;
            // Broker attempt first when preferred; a broker miss rebuilds
            // once onto the loopback path instead of failing the sign-in.
            AuthenticationResult? result = await AcquireInteractiveAsync(windowHandle).ConfigureAwait(false);
            if (result is null && PreferBroker && Rebuild(false))
                result = await AcquireInteractiveAsync(windowHandle).ConfigureAwait(false);
            return FromResult(result);
        }
        catch
        {
            return null;
        }
    }

    private async Task<AuthenticationResult?> AcquireInteractiveAsync(IntPtr windowHandle)
    {
        try
        {
            if (_app is null)
                return null;
            var builder = _app.AcquireTokenInteractive(_scopes);
            if (windowHandle != IntPtr.Zero)
            {
                try { builder = builder.WithParentActivityOrWindow(windowHandle); } catch { }
            }
            var result = await builder.ExecuteAsync().ConfigureAwait(false);
            return result;
        }
        catch (MsalException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            if (_app is not null && _account is not null)
            {
                try { await _app.RemoveAsync(_account).ConfigureAwait(false); } catch { }
            }
            _account = null;
            try
            {
                string path = AuthService.CacheFilePathFor(AppPaths.DataFolder);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
        catch { }
    }

    private AuthAccount? FromResult(AuthenticationResult? result)
    {
        try
        {
            if (result?.Account is null)
                return null;
            _account = result.Account;
            string username = result.Account.Username;
            if (string.IsNullOrWhiteSpace(username))
                username = result.Account.HomeAccountId?.ObjectId ?? "account";
            return new AuthAccount(username, result.Account.Username);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        try
        {
            _disposed = true;
            _app = null;
            _account = null;
        }
        catch { }
    }
}
