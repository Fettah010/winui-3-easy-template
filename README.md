## DevTem-WinUI 3

A modern **Windows 11** app built with **WinUI 3** (.NET 10) and shipped with
**Velopack** auto-updates over **GitHub Releases**. It also ships with a
ready-made **logging system** (Serilog) and one-command release scripts, so it
works as a starting template for WinUI 3 apps.

### Tech stack

- .NET 10 · WinUI 3 / Microsoft.WindowsAppSDK 1.8
- Velopack 1.2 — installer + delta auto-updates
- Serilog 4 — console + rolling file logging
- Community Toolkit (SettingsControls, Segmented, Helpers)

### Quick start (runs from source)

```powershell
dotnet build -c Debug
dotnet run
```

The app window uses a native **Mica** backdrop + rounded corners. On startup
it auto-checks the release feed in the background: when a new version exists it
is downloaded silently, and the user is asked to restart once it is ready. Open
**Updates** in the left nav to use the manual *Check for Updates* button instead.

> Running from `dotnet run` (not installed), update checks are skipped with a
> friendly warning - install once via `setup.exe` to test real updates.

### Logging

```csharp
using DevTemWinUi3.Services;

LoggingService.Log.Debug("Debug message");
LoggingService.Log.Information("User {UserId} logged in", userId);
LoggingService.Log.Error(ex, "Something failed");
```

Logs go to the debugger console and to `Logs/applog-YYYYMMDD.log` next to the
executable (daily rolling, 14 days kept). Initialized in `Program.cs`.

### Releases

The app is unpackaged (`WindowsPackageType=None`) and updated via Velopack.
Publishing is automated with **GitHub Actions**: the simplest flow is to push a
version tag — the pipeline builds, packs deltas and releases it to GitHub
Releases, and your users get the update in-app.

```powershell
# 1. Bump the version, commit, then tag and push a release
git tag v1.0.2
git push origin v1.0.2        # -> .github/workflows/release.yml runs, no PAT needed
```

Or run it manually from the **Actions** tab: *Run workflow* → enter the version
(e.g. `1.0.2`) → optional *beta/alpha* channel or *pre-release* flag.

The workflow uses the built-in `GITHUB_TOKEN` (no personal token required) and
delegates all packaging to `Scripts/build-and-release.ps1`, so the exact same
build can be reproduced locally:

```powershell
# Prereqs once
dotnet tool install --global vpk
$env:GITHUB_TOKEN = "ghp_YOUR_TOKEN"      # repo scope, for local uploads

# Full local release (publish -> pack with deltas -> upload, published)
.\Scripts\build-and-release.ps1 -Version 1.0.2 -Channel stable -Download -Publish

# Local-only build & pack (no upload), ReleaseNotes.md optional
.\Scripts\build-and-release.ps1 -Version 1.0.2 -SkipBuild -ReleaseNotes .\release-notes.md

# Upload an already-packed Releases/ folder afterwards
$env:GITHUB_TOKEN = "ghp_YOUR_TOKEN"
.\Scripts\upload-github.ps1 -Channel stable -Publish
```

Signature: `-Download` fetches the previous release so delta packages are
generated; without it users get a full (larger) package. Skip `-Publish` to
keep the release as a draft and flip it in the GitHub UI later.

Users install once via `setup.exe` and every later version installs through
the in-app **Updates** view (check → download → restart).

> A `GITHUB_TOKEN` is optional for end users checking updates (GitHub's public
> API limit is 60 requests/hour per IP). Set one to avoid the limit if you
> have many users. The `vpk` CLI always needs `$env:GITHUB_TOKEN` to upload.

### Offline / local testing

```powershell
.\Scripts\local-smoke-test.ps1   # packs v1.0.0 into Releases/ and v1.0.1 into ReleasesLocal/
```

Run `Releases\setup.exe` to install v1.0.0, then point `UpdateService` at the
local folder (`SimpleFileSource`/file path) to demo a real update.

### Repository layout

```
Program.cs                 # Velopack bootstrap + logging init
App.xaml(.cs)              # Application entry
MainWindow.xaml(.cs)       # Shell: custom title bar + NavigationView
Pages/
  HomePage.xaml            # Landing page
  UpdatesPage.xaml         # Manual update UI (check → download → install)
Services/
  AppInfo.cs               # Version helpers
  LoggingService.cs        # Serilog setup
  UpdateService.cs         # Velopack UpdateManager wrapper
Scripts/
  build-and-release.ps1    # publish + pack + upload (used by CI too)
  upload-github.ps1        # upload an already-packed Releases/ folder
  local-smoke-test.ps1     # local-only update demo
.github/
  workflows/release.yml    # automated release on tag push / manual dispatch
```

### License

MIT.