## DevTem-WinUI 3

A modern **Windows 11** app built with **WinUI 3** (.NET 10) and shipped with
**Velopack** auto-updates over **GitHub Releases**. It also ships with a
ready-made **logging system** (Serilog), **dependency injection**,
**SQLite database**, and **typed HTTP client**, so it works as a
complete starting template for WinUI 3 apps.

### Tech stack

- .NET 10 · WinUI 3 / Microsoft.WindowsAppSDK 1.8
- Velopack 1.2 — installer + delta auto-updates (updates feature)
- Serilog 4 — console + rolling file logging
- Sentry 6 — crash reporting (opt-in via DSN, off by default)
- Microsoft.Extensions.DependencyInjection — IoC container
- Microsoft.Data.Sqlite — local database (database feature)
- Microsoft.Extensions.Http — typed HTTP client (database feature)
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

#### Auto-updates (Velopack) — updates feature

The app checks GitHub Releases on startup. When a new version is found, it
downloads with a live progress bar and prompts the user to restart once the
download finishes, then applies the update and restarts smoothly.

```powershell
# Tag-based release
git tag v0.0.3-beta
git push origin v0.0.3-beta
```

#### Notifications

Two complementary channels:

- **In-app toasts** (`NotificationService`) — small animated cards,
  bottom-right, theme-aware WinUI 3 styling. Used for update results,
  e.g. the "updates need an installed app" notice.
- **Desktop toasts** (`DesktopToastService`) — native Action Center
  notifications via the Windows App SDK. Used when the app minimizes to
  the system tray; clicking the toast reopens the app.

#### Logging (Serilog)

```csharp
using DevTemWinUi3.Services;

LoggingService.Log.Debug("Debug message");
LoggingService.Log.Information("User {UserId} logged in", userId);
LoggingService.Log.Error(ex, "Something failed");
```

Logs go to the debugger console and to `Logs/applog-YYYYMMDD.log` next to the
executable (daily rolling, 14 days kept).

#### Crash reporting (Sentry)

Unhandled exceptions (app-domain, task pool, UI thread) are logged locally
and, when a DSN is configured, reported to Sentry with release + channel
tags. Disabled by default — paste a DSN into `AppMetadata.SentryDsn` to
enable. Queued reports flush on clean exit.

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

#### Typed HTTP Client — database feature

```csharp
var api = ServiceLocator.GetRequiredService<ApiService>();
api.BaseAddress = new Uri("https://api.example.com");

var users = await api.GetAsync<List<User>>("/users");
var result = await api.PostAsync<CreateUserRequest, CreateUserResponse>("/users", request);
```

Uses `IHttpClientFactory` for proper lifecycle management. No socket exhaustion.

#### Localization

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

### Releases — updates feature

The app is unpackaged (`WindowsPackageType=None`) and updated via Velopack.
Publishing is automated with **GitHub Actions**: the simplest flow is to push a
version tag — the pipeline builds, packs deltas and releases it to GitHub
Releases, and your users get the update in-app.

```powershell
# 1. Bump the version, commit, then tag and push a release
git tag v0.0.3-beta
git push origin v0.0.3-beta        # -> .github/workflows/release.yml runs
```

Or run it manually from the **Actions** tab: *Run workflow* → enter the version
(e.g. `0.0.3-beta`) → select *beta* channel.

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
Services/
  AppInfo.cs               # Version helpers
  LoggingService.cs        # Serilog setup
  UpdateService.cs         # Velopack UpdateManager wrapper (updates feature)
  SettingsService.cs       # Persisted user preferences
  ResponsiveLayout.cs      # Breakpoints + DPI math (min size, compact pane)
  DesktopToastService.cs   # OS Action Center toasts (tray feature)
  NotificationService.cs   # In-app animated toast cards
  WindowStateService.cs    # Window size/position persistence
  FirstRunService.cs       # First-run detection + What's New dialog
  DatabaseService.cs       # SQLite database access (database feature)
  ApiService.cs            # Typed HTTP client (database feature)
  LocalizationService.cs   # Dictionary-based i18n (en-US, es-ES, fr-FR)
  ServiceLocator.cs        # Dependency injection container
Tests/
  Services/                # Unit tests (requires app runtime for DI tests)
  Controls/                # Layout math tests
Scripts/
  build-and-release.ps1    # publish + pack + upload (used by CI too) (updates feature)
  create-shortcut.ps1      # creates desktop shortcut
.github/
  workflows/release.yml    # automated release on tag push (updates feature)
```

### License

MIT.
