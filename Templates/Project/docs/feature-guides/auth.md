# Entra ID auth (MSAL, opt-in)

Sign-in is scaffolded only with `--auth true` (default **off**): the MSAL
backend (`Services/MsalAuthProvider.cs`) plus its package refs live behind
that flag, while the SDK-free veneer (`Services/AuthService.cs`) ships in
every scaffold and degrades to a no-op when auth is off.

```csharp
// ProductConfiguration.cs (never commit a real client id; prefer env overrides)
public const string EntraClientId = "";   // or DEVTEM_ENTRA_CLIENT_ID at runtime
public const string EntraAuthority = "https://login.microsoftonline.com/common";
public const string EntraScopes = "User.Read";
```

Register an app in Microsoft Entra (a client id + `http://localhost`
loopback redirect for the fallback path; the broker redirect is derived
from the client id) and paste the client id — or set `DEVTEM_ENTRA_*` —
to enable. The Settings account section appears only in auth scaffolds.

## Broker vs loopback (measured, not guessed)

- Packaged runs (MSIX, package identity present) try the broker (WAM)
  first: SSO + conditional access, no credential handling.
- Unpackaged runs (portable) use the system-browser loopback flow: zero
  packaging prerequisites, no SSO.
- A broker miss rebuilds once onto the loopback path instead of failing
  the sign-in; every step is never-throw.

## Privacy posture (pinned by tests)

- The cache is an MSAL v3 blob in a DPAPI file
  (`msal-cache.dat` under `AppPaths.DataFolder`: `%LocalAppData%` portable,
  package `LocalFolder` when packaged) — MSAL serialization, never custom
  storage. Sign-out removes the account AND deletes the file.
- `BuildStatusContext()` carries two booleans (`authEnabled`, `signedIn`):
  the exact key set and yes/no values are asserted in
  `AuthServiceTests.BuildStatusContext_CarriesNoIdentityValues`, so no
  username, id, or error text can sneak into diagnostics later.
- Identity values never reach logs (`RepoHygieneTests` fails the build on
  any secret-adjacent `AppLog` call, auth included).

Remove with `--auth false`: the backend, its packages, its tests, and this
guide vanish; the veneer stays as a no-op and the account section hides.
