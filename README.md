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

The app window uses a native **Mica** backdrop + rounded corners. Open
**Updates** in the left nav to use the manual *Check for Updates* button.

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
`Scripts/build-and-release.ps1` does the full publish → pack → upload flow.

```powershell
# 1. Install the Velopack CLI once
dotnet tool install --global vpk

# 2. Export a GitHub token with repo scope (used for uploading releases)
$env:GITHUB_TOKEN = "ghp_YOUR_TOKEN"

# 3. Build, pack and upload (draft release by default)
.\Scripts\build-and-release.ps1 -Version 1.0.1 -Channel stable
```

This publishes an installer (`setup.exe`) plus delta-enabled update packages to
a **draft** GitHub Release on this repo (Fettah010/winui-3-easy-template).
Add `-Publish` to upload, or use `.\Scripts\upload-github.ps1 -Publish`
afterwards.

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
  build-and-release.ps1    # publish + pack + upload
  upload-github.ps1        # upload an already-packed Releases/ folder
  local-smoke-test.ps1     # local-only update demo
```

### License

MIT.