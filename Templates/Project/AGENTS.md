# AGENTS.md

Guidance for AI agents and developers working on this repository. Read this
first before making changes.

## What this is

**DevTem-WinUI 3** — a ready-to-use template for **WinUI 3** desktop apps.
It is a small starter app (a Home page + a Settings page) that demonstrates:

- WinUI 3 / Windows App SDK on **.NET 10** (`net10.0-windows10.0.19041.0`)
- Unpackaged app (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`)
- **Velopack 1.2.0** auto-updates over **GitHub Releases** (updates feature)
- **Serilog** logging (debugger console + rolling file, 14 days)
- **CommunityToolkit.Mvvm** for MVVM pattern (ObservableProperty, RelayCommand)
- **SettingsService** for persisted user preferences (theme, channel, etc.)
- One-command release pipeline (local script + GitHub Actions, no PAT) (updates feature)

Starred on GitHub: `Fettah010/winui-3-easy-template` (public). Platform: Windows.

## Layout / key files

| Path | Purpose |
| --- | --- |
| `Program.cs` | Entry point: `LoggingService.Initialize()`, `VelopackApp.Build()`, global exception handlers. |
| `App.xaml` / `App.xaml.cs` | XAML app, UI-thread exception logging, auto-update check on startup. |
| `MainWindow.xaml(.cs)` | Native Mica backdrop + custom title bar; NavigationView with Home/Settings pages. |
| `Pages/HomePage.*` | Landing page. |
| `Pages/SettingsPage.*` | App settings: theme selector, update channel, auto-check toggle, app info. |
| `Controls/WrapPanel.cs` + `Controls/WrapLayout.cs` | Dependency-free wrap panel (button rows) with pure, unit-tested layout math. |
| `Services/ResponsiveLayout.cs` | Breakpoints + DPI math: min window 720x540, compact pane <860, narrow page <700. |
| `Pages/UpdatesPage.*` | REMOVED — retired sample lives in `docs/archive/updates-legacy/` (reference only, not built). |
| `ViewModels/SettingsPageViewModel.*` | MVVM ViewModel for Settings page using CommunityToolkit.Mvvm. |
| `Templates/Page/` | `dotnet new devtem-page` item template (Page + VM + test stub). Excluded from build; sources live under `Templates/`. |
| `Services/UpdateService.cs` | Thin wrapper over Velopack `UpdateManager` (updates feature). |
| `Services/LoggingService.cs` | Serilog setup; log file `Logs/applog-YYYYMMDD.log` next to the exe. |
| `Services/SettingsService.cs` | Persisted user preferences (theme, channel, etc.) via `LocalSettingsStore`. |
| `Services/AppInfo.cs` | Assembly-version accessors. |
| `Services/DesktopToastService.cs` | OS Action Center toasts (tray feature); click reopens the app. |
| `Services/LocalizationService.cs` | en-US/es-ES/fr-FR dictionaries, instant switch via LanguageChanged, persisted choice. |
| `Services/NotificationService.cs` | In-app toast cards (bottom-right host). |
| `Assets/app.ico` | App + installer + tray + shortcut icon (single source, `vpk --icon`). |
| `Assets/Logo*.png` | In-app logo PNGs (splash, title bar, Home, About). |
| `Scripts/build-and-release.ps1` | **Single source of truth** for building+publishing a release (updates feature). |
| `Scripts/create-shortcut.ps1` | Creates desktop shortcut for the app. |
| `run-dev.vbs` / `run-dev.bat` | Dev-only `dotnet run` launchers (repo-relative paths). The desktop shortcut targets the built exe directly (fast cold start). |
| `.github/workflows/release.yml` | CI release pipeline (updates feature). |
| `Tests/` | xunit test project. |
| `README.md` | User-facing docs/questions. |
| `AGENTS.md` | This file - guidance for AI agents. |

## Branches & releases

### Branch structure

```
main          ← Development branch (latest code)
├── beta      ← Points to latest beta release commit
└── stable    ← Points to latest stable release commit (currently empty)
```

**IMPORTANT**: 
- `main` is for development - always has latest code
- `beta`/`stable` point to release commits
- Do NOT commit directly to `beta`/`stable` - they are updated by the release process
- Beta = testing releases, Stable = production releases

### Current state

| Branch | Points to | Purpose |
|--------|-----------|---------|
| `main` | Latest commit | Development |
| `beta` | v0.0.1-beta release | Beta channel |
| `stable` | (empty) | Stable channel - no releases yet |

### How releases work

1. **Tag naming determines channel:**
   - `v0.0.1-beta` → beta channel
   - `v0.0.1` (no suffix) → stable channel

2. **Release process:**
   ```powershell
   # 1. Make changes on main, commit, push
   git checkout main
   # ... edit files ...
   git add -A; git commit -m "feat: ..."; git push origin main
   
   # 2. Bump version in DevTemWinUi3.csproj (Version, AssemblyVersion, FileVersion)
   
   # 3. Tag and push (choose one):
   git tag v0.0.2-beta        # for beta release
   git push origin v0.0.2-beta
   
   # OR
   
   git tag v1.0.0             # for stable release
   git push origin v1.0.0
   ```

3. **CI automatically:**
   - Detects channel from tag name
   - Builds, packs, uploads to GitHub Releases
   - Creates `releases.<channel>.json` feed

4. **After release, update the channel branch:**
   ```powershell
   # For beta:
   git branch -f beta v0.0.2-beta
   git push origin beta --force
   
   # For stable:
   git branch -f stable v1.0.0
   git push origin stable --force
   ```

### Deleting old releases

Go to https://github.com/Fettah010/winui-3-easy-template/releases and delete
unwanted releases manually (click release → scroll down → Delete).

## Build / run / verify

```powershell
dotnet build -c Debug -p:Platform=x64        # primary local build check (must be 0 warnings)
dotnet run                                   # runs unpackaged (updates disabled)
```

- **IMPORTANT**: Always pass `-p:Platform=x64` (or `win-x64`). XAML compiler fails without it.
- **IMPORTANT**: Build must have **0 warnings, 0 errors**. MVVMTK0045 warnings are suppressed.
- Update checks are only active for **installed** apps.

## Running the app

- **Desktop shortcut**: Double-click `DevTem-WinUI 3` on desktop (targets the built exe)
- **Command line**: `dotnet run -c Debug -p:Platform=x64` (or `run-dev.bat`)

## Desktop shortcut setup

Run `Scripts\create-shortcut.ps1` to create the desktop shortcut. The shortcut
targets the built exe directly (fast cold start). `run-dev.vbs` / `.bat`
run `dotnet run` (handy for dev, but adds seconds to every launch) — the app
is WinExe so it never shows a terminal window on its own.

## Versioning

Current version: **0.0.2** (see `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
in `DevTemWinUi3.csproj` — keep all three in sync, plus `<InformationalVersion>`:
beta releases carry the `-beta` suffix (e.g. `0.0.2-beta`) so fresh installs
default to the beta channel; stable releases use the plain version).

## Known gotchas

1. **PowerShell array splats bind positionally, not by name.** Use hashtable splat `@{}`.
2. **vpk v1.x flag names:** `--icon` (NOT `--packIcon`); `-u/--packId`, `-v/--packVersion`.
3. **Velopack versions must keep increasing.** Same version can only go to one channel.
4. **Channels ≠ git branches.** Channels are separate feeds. Branches track releases.
5. **Tag naming determines channel:** `v0.0.1-beta` → beta, `v0.0.1` → stable.
6. **Never use ApplicationData.LocalSettings.** It does not persist for unpackaged
   apps (no settings.dat is ever written) — all settings go through
   `LocalSettingsStore` (`%LocalAppData%\DevTemWinUi3\settings.json`).
7. **MVVM toolkit AOT warnings.** Suppressed via `<NoWarn>$(NoWarn);MVVMTK0045</NoWarn>`.
8. **Desktop shortcut targets the built exe directly.** The app is WinExe (no
   console), so no VBS wrapper is needed; `.bat`/VBS run `dotnet run` and add
   seconds to launch.
9. **Retired code lives in `docs/archive/`** (excluded from build). Do not
   resurrect it into `Pages/`; treat it as a reading reference only.

## Conventions

- Keep `Scripts/build-and-release.ps1` as single source of truth for release logic.
- Use `MicaBackdrop` + extended-titlebar pattern for the window.
- **MVVM pattern**: Use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm.
- Wrap all external service calls in try-catch.
- Build must always have 0 warnings before committing.
