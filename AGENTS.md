# AGENTS.md

Guidance for AI agents and developers working on this repository. Read this
first before making changes.

## What this is

**DevTem-WinUI 3** — a ready-to-use template for **WinUI 3** desktop apps.
It is a small starter app (a Home page + an Updates page) that demonstrates:

- WinUI 3 / Windows App SDK on **.NET 10** (`net10.0-windows10.0.19041.0`)
- Unpackaged app (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`)
- **Velopack 1.2.0** auto-updates over **GitHub Releases**
- **Serilog** logging (debugger console + rolling file, 14 days)
- One-command release pipeline (local script + GitHub Actions, no PAT)

Starred on GitHub: `Fettah010/winui-3-easy-template` (public). Platform: Windows.

## Layout / key files

| Path | Purpose |
| --- | --- |
| `Program.cs` | Entry point: `LoggingService.Initialize()`, `VelopackApp.Build()`, global exception handlers (AppDomain + TaskScheduler + try/catch around startup). |
| `App.xaml` / `App.xaml.cs` | XAML app, UI-thread exception logging, **auto-update check on startup** (background check → silent download → restart prompt dialog). |
| `MainWindow.xaml(.cs)` | Native Mica backdrop + custom title bar; NavigationView with Home/Updates pages. |
| `Pages/HomePage.*` | Landing page. |
| `Pages/UpdatesPage.*` | Manual "Check for Updates" with channel selector (`stable`/`beta`/`dev`), download progress, install/restart. |
| `Services/UpdateService.cs` | Thin wrapper over Velopack `UpdateManager` (GitHub source, lazy per-channel manager, `IsInstalled`, download/apply). |
| `Services/LoggingService.cs` | Serilog setup; log file `Logs/applog-YYYYMMDD.log` next to the exe. |
| `Services/AppInfo.cs` | Assembly-version accessors. |
| `Assets/app.ico` | App icon; wired via `<ApplicationIcon>` and passed to vpk pack with `--icon`. |
| `Scripts/build-and-release.ps1` | **Single source of truth** for building+publishing a release (used locally AND by CI). |
| `.github/workflows/release.yml` | CI release pipeline (tag push `v*` or manual `workflow_dispatch`). |
| `README.md` | User-facing docs/questions. |

## Build / run / verify

```powershell
dotnet build -c Debug -p:Platform=x64        # primary local build check
dotnet run                                   # runs unpackaged (updates disabled)
```

- Project uses `<Platforms>x86;x64;ARM64`, XAML compiler runs only with a
  platform specified — always pass `-p:Platform=x64` (or `win-x64`).
- Update checks are only active for **installed** apps (`UpdateService.IsInstalled`);
  running from the build output logs "app is not installed" and skips. To test
  real updates, install via `setup.exe` from a release first.

## Versioning & releases

Current version: **0.0.2** (see `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
in `DevTemWinUi3.csproj` — keep all three in sync).

### How to release (tag flow, recommended)

1. Bump all three version fields in the csproj (e.g. `0.0.3`).
2. Commit + push `main`.
3. `git tag v0.0.3 && git push origin v0.0.3`
4. Workflow auto-runs: publish → `vpk download` (builds a delta from the
   previous release) → `vpk pack` → `vpk upload` → publishes to GitHub Releases.

Manual alternative: **Actions → Release → Run workflow** with a version/channel/
pre-release inputs (CI uses `workflow_dispatch`).

Release results land on https://github.com/Fettah010/winui-3-easy-template/releases
with assets: `Setup.exe`, `Portable.zip`, `*-full.nupkg`, `*-delta.nupkg`,
`releases.<channel>.json`.

### Deliverables produced per release

- `DevTemWinUi3-<v>-<channel>-full.nupkg` — full update package
- `DevTemWinUi3-<v>-<channel>-delta.nupkg` — delta from previous release
- `DevTemWinUi3-<channel>-Setup.exe` — installer
- `DevTemWinUi3-<channel>-Portable.zip`
- `releases.<channel>.json` — the update feed the app queries

## Release pipeline details

- `.github/workflows/release.yml`:
  - Triggers: push tag `v*` **and** `workflow_dispatch` (inputs: `version`
    required string, `channel` in `stable|beta|alpha`, `prerelease` boolean).
  - `permissions: contents: write` + built-in `GITHUB_TOKEN` — no PAT anywhere.
  - Steps: checkout (fetch-depth 0) → setup .NET 10 → install vpk → resolve
    version (TrimStart `v`; from tag or input) → generate ReleaseNotes.md →
    run `Scripts/build-and-release.ps1`.
- `Scripts/build-and-release.ps1`:
  - Params: `-Version`, `-Channel`, `-Runtime` (default `win-x64`),
    `-ReleaseNotes`, `-SkipBuild`, `-Download` (delta), `-Publish` (publish vs
    draft), `-PreRelease`, `-Tag`.
  - Env: `VPK_REPO_URL` (fork override, defaults to this repo) and `GITHUB_TOKEN`.
  - Does: `dotnet publish` (self-contained, trim/single-file off,
    `WindowsAppSDKSelfContained=true`) → optional `vpk download github`
    (non-fatal on failure) → `vpk pack` (passes `--icon` when present) →
    `vpk upload github` (draft unless `-Publish`).

## Known gotchas (important — these have caused real failures)

1. **PowerShell array splats bind positionally, not by name.** When invoking a
   PowerShell script with parameters built dynamically, use a **hashtable**
   splat (`@{}`), never an array splat (`@(...)`). An array splat shifts every
   argument by one (e.g. `-Runtime` receives `-Channel`) and fails confusingly.
   Array splats to **native** commands (`vpk`, `dotnet`) are fine — native apps
   don't do PowerShell named binding.
2. **vpk v1.x (1.2.0) flag names differ from old guides/v0 CLI.** Icon flag is
   `--icon` (NOT `--packIcon`); ids are `-u/--packId`, `-v/--packVersion`,
   `-p/--packDir`, `-e/--mainExe`. Check `vpk pack --help` when unsure.
3. **Velopack versions are global and must keep increasing.** The same version
   can generally only be released to one channel; **promotion = ship a new
   version to the target channel**, not a same-version copy.
4. **Channels ≠ git branches.** Channels are separate feeds
   (`releases.stable.json`, `releases.beta.json`, …). A user on the `beta`
   channel only reads the beta feed. The channel is chosen at runtime in the
   app AND at pack/upload time in the pipeline — two separate switches.
5. **Channel-name mismatch to keep in mind:** the workflow input offers
   `stable|beta|alpha`, but `UpdatesPage.xaml.cs` lets users pick
   `stable|beta|dev`. A release to `alpha` cannot be seen by any client.
6. **`--pre` is not a channel.** It only marks the GitHub Release as a
   pre-release (UI badge); clients decide what to download by channel feed.
7. **Velopack `0.0.x` versions**: plain `0.0.1` was rejected by vpk before
   v0.0.610; fine on current vpk (1.2.0). No action needed.
8. **GitHubSource token**: optional for public repos (`GithubSource(repo, null)`,
   no OAuth) — GITHUB_TOKEN may be set to raise the 60 req/h rate limit.
9. **Velopack 1.2 API**: use `VelopackApp.Build().OnFirstRun(...).OnRestarted(...)`
   (there is no `WithFirstRun` anymore). Use `GithubSource` without `useOAuth:
   true`.
10. **CI upload token**: the workflow sets `GITHUB_TOKEN` from `${{ github.token }}`.
    Locally, `vpk upload` requires a personal token with `repo` scope in
    `GITHUB_TOKEN`. Without a token the script uploads nothing and prints a warning.

## Conventions

- Keep `Scripts/build-and-release.ps1` the single source of truth for release
  logic; CI must delegate to it rather than duplicating commands.
- `#` header comments in source files explain intent (the project's style);
  keep them meaningful. Do not add noisy inline comments.
- Use `MicaBackdrop` + extended-titlebar pattern for the window (don't revert
  to the default title bar).
- Add real value only: this repo deliberately avoids dependencies that aren't
  used (checked in: WindowsAppSDK, Velopack, Serilog + sinks, CommunityToolkit
  SettingsControls/Segmented/Helpers).