namespace DevTemWinUi3.Services.Abstractions;

/// <summary>
/// Signed-in account snapshot. Immutable data only — values never flow to
/// logs (see <c>RepoHygieneTests.Secrets_NeverReachLogs</c>). Public so the
/// public <see cref="Services.AuthService"/> surface can expose it; the
/// provider contract itself stays internal.
/// </summary>
public sealed class AuthAccount
{
    public AuthAccount(string username, string? displayName = null)
    {
        Username = username;
        DisplayName = displayName;
    }

    public string Username { get; }

    public string? DisplayName { get; }
}

/// <summary>
/// Auth-backend contract. Implemented by <see cref="Services.MsalAuthProvider"/>
/// (scaffolded only with the auth feature); the veneer below programs
/// against this so call sites never touch identity-SDK types.
/// </summary>
internal interface IAuthProvider : System.IDisposable
{
    /// <summary>
    /// Silent resume from the persisted cache. Null when nothing is cached,
    /// the cache is unreadable, or interaction is required. Never throws.
    /// </summary>
    System.Threading.Tasks.Task<AuthAccount?> TryResumeAsync();

    /// <summary>
    /// Interactive sign-in (broker-first with embedded fallback in the MSAL
    /// backend). Null when cancelled, unavailable, or unconfigured.
    /// <paramref name="windowHandle"/> parents the auth UI; zero means no
    /// parent (headless/tests). Never throws.
    /// </summary>
    System.Threading.Tasks.Task<AuthAccount?> SignInAsync(System.IntPtr windowHandle);

    /// <summary>Clears the account + persisted cache. Never throws.</summary>
    System.Threading.Tasks.Task SignOutAsync();
}
