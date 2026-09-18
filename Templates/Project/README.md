## DevTem-WinUI 3

A modern **Windows 11** app built with **WinUI 3** (.NET 10) and shipped with
auto-updates over **GitHub Releases** (**Velopack** by default; `--updates
basic` for a zero-dependency checker, `--updates none` to drop updating).
It also ships with a
ready-made **logging system** (Serilog), **dependency injection**,
optional **SQLite database**, and optional **typed HTTP client**, so it works
as a complete starting template for WinUI 3 apps.

The exact selected options, generated next steps, and feature-specific setup
are recorded in [`docs/FEATURES.md`](docs/FEATURES.md). Disabled feature
guides are not generated.

### Tech stack

- .NET 10 · WinUI 3 / Microsoft.WindowsAppSDK 1.8
- Velopack 1.2 — installer + delta auto-updates (`--updates velopack`, the default)
- Serilog 4 — console + rolling file logging
- Sentry 6 — crash reporting (opt-in via DSN, off by default)
- Microsoft.Extensions.DependencyInjection — IoC container
- Microsoft.Data.Sqlite — local database (`--database`)
- Microsoft.Extensions.Http — typed HTTP client (`--http`)
- Community Toolkit (SettingsControls, Segmented, Helpers)

### Quick start (runs from source)

```powershell
dotnet build -c Debug -p:Platform=x64
dotnet run -c Debug -p:Platform=x64
```

The app window uses a native **Mica** backdrop + rounded corners. On startup
it auto-checks the release feed in the background: when a new version exists it
is downloaded silently, and the user is asked to restart once it is ready.

### Features

### Starter profiles

Use the same template with a coherent preset, then override individual
feature flags when needed:

```powershell
# Minimal: shell, settings, localization and logging only
dotnet new devtem-winui -n MyApp --tray false --updates none --database false --http false --health false --logging none --crash false --localization false --tests false --attribution false

# Desktop: tray and notifications, without distribution services
dotnet new devtem-winui -n MyApp --updates none --database false --http false

# Production: all optional services enabled (the default)
dotnet new devtem-winui -n MyApp

# Store: packaged MSIX for Microsoft Store submission
dotnet new devtem-winui -n MyApp --distribution msix --updates store --setup false

# Dual: one binary for GitHub (Velopack) + Store (same MSIX, see distribution-dual.md)
dotnet new devtem-winui -n MyApp --publisher "CN=Your-ID"
```

These are documented presets rather than a separate `--profile` parameter;
explicit feature flags remain the authoritative customization surface.

#### Auto-updates — updates choice (`velopack`/`basic`/`none`/`appinstaller`/`store`)

The app checks GitHub Releases on startup. When a new version is found, it
downloads with a live progress dialog and prompts the user to restart once
the download finishes, then applies the update and restarts smoothly.
Transient states (checking, up-to-date, errors) report through in-app
toast cards; decisions (install, restart) use native WinUI 3 dialogs.
Settings links into the same flow.

- `velopack` (default): full installer + delta downloads + release pipeline.
- `basic`: zero-dependency checker — downloads the release's Setup `.exe`
  and launches it. No SDK, no pipeline; attach the `.exe` yourself.
- `none`: no update engine — the update popup reports the build has none.
- `appinstaller` / `store` (`--distribution msix` only): native MSIX
  updates — Windows owns the flow, Settings shows a status card instead
  (Store listing or Windows Apps settings).

```powershell
# Tag-based release
git tag v0.0.1-beta
git push origin v0.0.1-beta
```

#### First-run setup wizard — setup flag (portable only)

Install-time choices (location, shortcuts, launch) belong to the
installer — the MSIX package or Velopack setup — so first launch lands
on Home with a welcome dialog, never an in-app stepper
(`Pages/SetupWizardPage` stays available but never auto-opens).
`--setup false` drops it. Packaged (MSIX) scaffolds skip it — Windows
owns location and shortcuts there.

#### Notifications

Two complementary channels:

- **In-app toasts** (`NotificationService`) — small animated cards,
  bottom-right, theme-aware WinUI 3 styling. Used for update results,
  e.g. the "updates need an installed app" notice. Verify they render
  on any machine via Settings → Notifications → test toast.
- **Desktop toasts** (`DesktopToastService`) — native Action Center
  notifications via the Windows App SDK. Used when the app minimizes to
  the system tray; clicking the toast reopens the app.

#### Logging — logging choice (`serilog`/`mel`/`none`)

```csharp
using DevTemWinUi3.Services;

AppLog.Debug("Debug message");
AppLog.Information("User {UserId} logged in", userId);
AppLog.Error(ex, "Something failed");
```

App code logs through the `AppLog` facade, so call sites are identical on
every backend:

- `serilog` (default): debugger console + daily-rolling files
  (`Logs/applog-YYYYMMDD.log`, 14 days kept).
- `mel`: debugger + in-app buffer only — no log files.
- `none`: no logging packages; the in-app buffer stays only when the
  diagnostics page is on.

#### Crash reporting (Sentry) — crash feature (on by default)

Unhandled exceptions (app-domain, task pool, UI thread) are logged locally
and, when a DSN is configured, reported to Sentry with release + channel
tags. Disabled by default — paste a DSN into `AppMetadata.SentryDsn` to
enable. `SentryEnvironment` and `SentryRelease` optionally override the
derived environment and assembly version. Queued reports flush on clean exit.

#### Dependency Injection

```csharp
// Register services in ServiceLocator.cs
// Resolve anywhere:
var db = ServiceLocator.GetRequiredService<DatabaseService>();
var api = ServiceLocator.GetRequiredService<ApiService>();
```

All services are registered in `ServiceLocator.Initialize()` and available
via `ServiceLocator.Services`.

#### SQLite Database — database feature

```csharp
var db = ServiceLocator.GetRequiredService<DatabaseService>();
await db.InitializeAsync();

// Key-value settings
await db.SetSettingAsync("theme", "dark");
var theme = await db.GetSettingAsync("theme");

// Raw SQL
await db.ExecuteAsync("INSERT INTO Logs (Message) VALUES ($msg)",
    new SqliteParameter("$msg", "Hello"));
```

Database file is created at `Data/app.db` next to the executable.

#### Typed HTTP Client — HTTP feature

```csharp
var api = ServiceLocator.GetRequiredService<ApiService>();
api.BaseAddress = new Uri("https://api.example.com");

var users = await api.GetAsync<List<User>>("/users");
var result = await api.PostAsync<CreateUserRequest, CreateUserResponse>("/users", request);
```

Uses `IHttpClientFactory` for proper lifecycle management. No socket exhaustion.

#### Localization — full 3-language set (English-only with `--localization false`)

```csharp
// Get localized string
var title = LocalizationService.Current.GetString("HomeTitle");

// Change language (takes effect immediately in UI)
LocalizationService.Current.SetLanguage("es-ES");

// Available languages: en-US, es-ES, fr-FR
```

The app supports instant runtime language switching via a language selector
in Settings — no restart needed. The choice is persisted across launches.
All UI strings are managed through `LocalizationService` using a
dictionary-based approach for reliable unpackaged app support.

Scaffolded English-only (`--localization false`), only `en-US` exists and
the picker is hidden; `Scripts/add-page.ps1` targets 3-language scaffolds
(it inserts the new page's strings into all three dictionaries).

### Releases — `--updates velopack` scaffolds only

The app is unpackaged (`WindowsPackageType=None`) and, with the default
updater, updated via Velopack.
Publishing is automated with **GitHub Actions**: the simplest flow is to push a
version tag — the pipeline builds, packs deltas and releases it to GitHub
Releases, and your users get the update in-app.

Packaged scaffolds (`--distribution msix`) ship as one runtime-adaptive
binary: pack it with `Scripts/build-msix.ps1` (Store `.msixupload` via
`-StoreUpload -Publisher "<your Publisher ID>"`, sideload feed via
`-AppInstaller`) and submit or host the output. `Scripts/publish-store.ps1`
wraps the whole Store flow (validate → upload → WACK → Partner Center);
scaffold with `--publisher` to pre-fill your ID. First-run setup wizard
is portable-only (Windows owns location and shortcuts for MSIX).

```powershell
# 1. Bump the version, commit, then tag and push a release
git tag v0.0.1-beta
git push origin v0.0.1-beta        # -> .github/workflows/release.yml runs
```

Dual-track: the same tag also packs the Store `.msixupload` when the
`DEVTEM_MSIX_PUBLISHER` repo secret is set (skipped gracefully without
it) — one binary, GitHub + Store at one version. See
`docs/feature-guides/distribution-dual.md` for the rules that bite
(same family or reinstall, Store owns Store installs).

Or run it manually from the **Actions** tab: *Run workflow* → enter the version
(e.g. `0.0.1-beta`) → select *beta* channel.

### Offline / local testing

```powershell
.\Scripts\local-smoke-test.ps1   # packs v1.0.0 into Releases/ and v1.0.1 into ReleasesLocal/
```

### Repository layout

```
Program.cs                 # Velopack bootstrap + logging init
App.xaml(.cs)              # Application entry + DI initialization
MainWindow.xaml(.cs)       # Shell: custom title bar + NavigationView
Controls/
  WrapPanel.cs             # Dependency-free wrap panel for button rows
  WrapLayout.cs            # Pure, unit-tested wrapping math
Pages/
  HomePage.xaml            # Landing page
  AboutPage.xaml           # App info, version, links
  SettingsPage.xaml        # Theme, update channel, auto-check
  UpdateCenterPage.xaml    # Check → download → install + release notes
  SetupWizardPage.xaml     # First-run wizard (portable + setup only)
Services/
  AppInfo.cs               # Version helpers
  AppPaths.cs              # Writable data root (portable vs packaged)
  AppFeatures.cs           # Scaffold-time flags (distribution, setup, updates)
  LoggingService.cs        # Serilog setup
  UpdateService.cs         # Velopack UpdateManager wrapper (--updates velopack)
  BasicGithubUpdateService.cs # GitHub-releases checker (--updates basic)
  SettingsService.cs       # Persisted user preferences
  ResponsiveLayout.cs      # Breakpoints + DPI math (min size, compact pane)
  DesktopToastService.cs   # OS Action Center toasts (tray feature)
  NotificationService.cs   # In-app animated toast cards
  WindowStateService.cs    # Window size/position persistence
  FirstRunService.cs       # First-run detection + What's New dialog
  DatabaseService.cs       # SQLite database access (database feature)
  ApiService.cs            # Typed HTTP client (HTTP feature)
  LocalizationService.cs   # Dictionary-based i18n (en-US, es-ES, fr-FR)
  ServiceLocator.cs        # Dependency injection container
Tests/
  Services/                # Unit tests (tests feature; requires app runtime for DI tests)
  Controls/                # Layout math tests
Scripts/
  build-and-release.ps1    # publish + pack + upload (used by CI too) (updates feature)
  create-shortcut.ps1      # creates desktop shortcut
.github/
  workflows/release.yml    # automated release on tag push (updates feature)
```

### License

MIT.
