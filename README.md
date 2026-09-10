## DevTem-WinUI 3

A modern **Windows 11** app built with **WinUI 3** (.NET 10) and shipped with
**Velopack** auto-updates over **GitHub Releases**.

### Tech stack

- .NET 10 · WinUI 3 / Microsoft.WindowsAppSDK
- Velopack 1.2 — installer + delta auto-updates
- Community Toolkit (SettingsControls, Segmented, Helpers)

### Quick start (runs from source)

```powershell
dotnet build -c Debug
dotnet run
```

The app window uses a native **Mica** backdrop + rounded corners. Open
**Updates** in the left nav to use the manual *Check for Updates* button.

### Before your first release — replace placeholder credentials

| Where | What |
| --- | --- |
| `Services/UpdateService.cs` | `GitHubRepoUrl` + `GitHubAuthToken` (PAT with `repo` scope) |
| `Scripts/build-and-release.ps1` | `$GithubRepoUrl` (token comes from `$env:GITHUB_TOKEN`) |
| `Scripts/upload-github.ps1` | `$GithubRepoUrl` |
| `Scripts/local-smoke-test.ps1` | optional, local-only |

### Publishing a release

```powershell
# 1. Install the Velopack CLI once
dotnet tool install --global vpk

# 2. Set your GitHub token (placeholder until you set a real one)
$env:GITHUB_TOKEN = "ghp_YOUR_TOKEN"

# 3. Build, pack and upload
.\Scripts\build-and-release.ps1 -Version 1.0.1 -Channel stable
```

This publishes an installer (`setup.exe`) plus delta-enabled update packages to
a **draft** GitHub Release. Add `-Publish` to upload, or use
`.\Scripts\upload-github.ps1 -Publish` afterwards.

Users then install once via `setup.exe` and every later version installs
through the in-app **Updates** view (check → download → restart).

### Offline / local testing

```powershell
.\Scripts\local-smoke-test.ps1   # packs v1.0.0 into Releases/ and v1.0.1 into ReleasesLocal/
```

Run `Releases\setup.exe` to install v1.0.0, then point `UpdateService` at the
local folder (`SimpleFileSource`/file path) to demo a real update.

> Note: Placeholder `YOUR_USERNAME`, `YOUR_REPOSITORY`, `YOUR_GITHUB_TOKEN`,
> `YOUR_NAME_OR_ORG` values must be replaced before the app can reach GitHub.