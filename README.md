# DevTem WinUI 3 starter

<p align="center">
  <img src="Assets/Logo.png" alt="DevTem logo" width="120" />
</p>

<p align="center">
  <a href="https://github.com/Fettah010/winui-3-easy-template/stargazers"><img src="https://img.shields.io/github/stars/Fettah010/winui-3-easy-template?style=for-the-badge" alt="GitHub Repo stars" /></a>
  <a href="https://www.nuget.org/packages/DevTem.Templates"><img src="https://img.shields.io/nuget/v/DevTem.Templates?style=for-the-badge&label=NuGet" alt="NuGet package version" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-67ac09?style=for-the-badge" alt="MIT license" /></a>
</p>

Production-ready **WinUI 3 starter template** for Windows desktop apps: a real app shell with updates, settings, diagnostics, and release automation already wired up.

It is built for teams that want a real app shell instead of a blank canvas: **MVVM**, **Mica window**, **system tray**, **Velopack auto-updates**, **localization**, **logging**, **SQLite**, **typed HTTP client**, **diagnostics page**, **setup wizard**, and a **release pipeline** already wired up.

## Contents

- [Install the template](#install-the-template) · [Requirements](#requirements) · [Screenshots](#screenshots)
- [Why DevTem](#why-devtem) · [What ships out of the box](#what-ships-out-of-the-box) · [What's new](#whats-new-in-060-beta)
- [Starter presets](#starter-presets) · [Pages](#pages-from-030) · [Ship it](#ship-it) · [Identity & notifications](#identity--notifications)
- [Run this repo locally](#run-this-repo-locally) · [FAQ](#faq) · [Repository layout](#repository-layout)

![Home page — welcome card, status, and feature overview](docs/screenshots/home.png)

## Install the template

```powershell
dotnet new install DevTem.Templates
```

Scaffold a full app:

```powershell
dotnet new devtem-winui -n AcmeDesk --displayName "Acme Desk" --company "Acme" --repo "acme/desk-app" --scheme "acme://"
```

Or add a page to an existing app:

```powershell
dotnet new devtem-page -n Orders
```

The same templates appear in Visual Studio's New Project dialog (same
engine, same flags as checkboxes and dropdowns). After scaffolding, your
copy's `README.md` walks through first steps (version reset, rebrand,
`init-template -Validate`).

## Requirements

- Windows 10 version 19041 or newer (x64, x86, ARM64) to run the app
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build
- Visual Studio 2026 (18.x) recommended for the designer, XAML Hot Reload,
  and the New Project dialog — the CLI alone is fully supported too

## Screenshots

Dark theme, English, maximized window. Every shot below is re-captured per
release from the running app (see `docs/screenshots/`).

![Settings page — theme, language, update channel, account, tray, backup](docs/screenshots/settings.png)

![Diagnostics page — status, metrics, live log tail, export bundle](docs/screenshots/diagnostics.png)

![About page — version, stack, license, links](docs/screenshots/about.png)

## Why DevTem

### DevTem vs. the official blank templates

| Option | Best for | What is missing | DevTem tradeoff |
| --- | --- | --- | --- |
| Official `dotnet new` WinUI blank | Learning WinUI basics | App shell, updates, tray, settings, diagnostics, release automation | Not a production starter |
| Template Studio | Visual scaffolding | Opinionated desktop app starter, release workflow, ready-made working patterns | More wizard-driven, less code-first |
| DevTem | Real-world desktop app starter | Some flexibility in favor of a working baseline | Curated defaults; composable at scaffold time |

DevTem is not trying to compete with the official blank app as a minimal "hello world." It is the production-ready WinUI 3 starter for the workloads that blank templates leave out on purpose.

## What ships out of the box

- WinUI 3 / Windows App SDK on .NET 10
- Mica-based desktop shell with native title bar behavior
- System tray support with single-instance activation
- Auto-updates (Velopack installer + deltas by default; zero-dependency checker or none at scaffold time; native AppInstaller / Store updates for packaged MSIX)
- First-run welcome (installers own setup choices; wizard page available but never auto-opens)
- Packaged MSIX distribution with runtime-adaptive paths, autostart, and protocol (`--distribution msix`)
- 3-language runtime localization (en-US, es-ES, fr-FR; English-only at scaffold time)
- Logging through one facade (Serilog console + file by default; MEL or none at scaffold time)
- Optional Sentry crash reporting (DSN-gated; SDK droppable at scaffold time)
- Optional Entra ID sign-in (MSAL broker-first with loopback fallback, DPAPI cache per distribution; off by default, SDK droppable at scaffold time)
- SQLite data layer and typed HTTP client (each droppable at scaffold time)
- Diagnostics page (startup time, live log tail with filters, export bundle)
- MVVM pattern with CommunityToolkit.Mvvm
- GitHub Actions release pipeline and version tag flow

Every bullet above except MVVM/Mica is a scaffold-time choice: the repo
app shows the all-on reference, `dotnet new devtem-winui --help` lists
the flags, and each scaffold records its picks in a generated
`docs/FEATURES.md` (see `docs/template-features.json` for the schema).

![Settings page — theme, language, update channel, test toast, tray](docs/screenshots/settings.png)

## What's new in 0.6.0-beta

- DevEx + packaging ergonomics: opt-in `--slnx` adds an XML solution file
  next to the classic `.sln` (VS 2026 and newer; the `.sln` stays the
  default — it alone expresses the x86/x64/ARM64 mappings);
  `init-template -Validate` v2 checks repo/publisher/scheme/URL consistency
  (placeholder Publisher on msix trees, template-default scheme/repo after
  a rename, malformed URLs); NuGet face GA (per-release notes, verified
  icon/tags).
- Evaluated and declined on record: a `--framework` selector (each TF value
  multiplies the scaffold matrix — net10 stays the default, retarget is a
  documented 3-line edit) and `--cpm` central package management (no XML
  conditional path keeps default scaffolds pristine). Reasons in
  `docs/DECISIONS.md`.
- No behavior change on any scaffold default (scaffold surface + docs + CI only).

Presets re-verified: `minimal` (leanest) / `recommended` (everything on,
portable + Velopack) / `full` (everything on except the portable-only setup
wizard, MSIX) via `Scripts/init-profile.ps1 -Preset`. "Everything on" means product surface —
identity (`--auth`, needs a tenant) and editor ergonomics (`--slnx`) stay
opt-in by design. In Visual Studio the template parameters render as dialog
fields, checkboxes, and dropdowns; `dotnet new devtem-winui --help` lists
every flag.

## Starter presets

Same template, coherent presets (override any flag individually, or run
`Scripts/init-profile.ps1 -Preset` for the interactive version):

| Preset | Command flags | Best for |
| --- | --- | --- |
| Minimal | `--tray false --updates none --database false --http false --health false --logging none --crash false --localization false --tests false --attribution false` | Leanest shell, no services |
| Desktop | `--updates none --database false --http false` | Tray app with notifications, no data layer |
| Production | no overrides (the defaults) | Full product surface, portable + Velopack |
| Store | `--distribution msix --updates store --setup false` | Packaged MSIX for Store submission |
| Dual | `--publisher "CN=Your-ID"` (on defaults) | One binary for GitHub (Velopack) + Store (same MSIX) |

Presets are documentation, not a `--profile` parameter: explicit flags
always win.

## Pages (from 0.3.0)

| Kind | Use | Command |
| --- | --- | --- |
| `page` (default) | Static content, forms, single-object views | `add-page.ps1 -Name Orders` |
| `list` | Master/details with selection + details strip | `add-page.ps1 -Kind list` |
| `grid` | Dense comparable rows, sortable columns | `add-page.ps1 -Kind grid` |
| `contentgrid` | Browsable catalogs, reflowing cards + search/sort/paging | `add-page.ps1 -Kind contentgrid` |
| `tab` | Document workspaces, several open documents in one view | `add-page.ps1 -Kind tab` |

Shells from 0.2.0 still hold: rail default, tabs for document apps,
menubar as a command pattern over the same commands
(`docs/TEMPLATE-GUIDE.md` "Shells").

![Tabbed workspace — document tabs with add/close over one rail route](docs/screenshots/shell-tabs.png)

Earlier releases: full notes in [CHANGELOG.md](CHANGELOG.md).

## Ship it

Every updates × distribution cell is proven, not just documented — the
truth table below carries a dated proof line per valid cell in
[`docs/feature-guides/distribution-dual.md`](docs/feature-guides/distribution-dual.md)
(matrix runs plus installed-app runs where complete).

| `--updates` \ `--distribution` | `portable` (default) | `msix` |
| --- | --- | --- |
| `velopack` (default) | ✅ Setup.exe + feed, full in-app flow | ❌ Guard (ship Dual tracks instead) |
| `basic` | ✅ Setup.exe + `.sha256` checker | ❌ Guard |
| `none` | ✅ No update code | ✅ Slim status surface |
| `appinstaller` | ❌ Guard (needs package identity) | ✅ `.msix` + `.appinstaller` feed |
| `store` | ❌ Guard (needs package identity) | ✅ Partner Center submission |

Releases ride two tag channels: `v*-beta` builds the beta feed
(`beta` branch follows) and `templates-v*` publishes the NuGet package;
`build-and-release.ps1` is the single source of truth, Store submission is
one command (`publish-store.ps1` + `submit-store.ps1`).

## Identity & notifications

Opt-in Entra ID sign-in (MSAL broker-first, DPAPI token cache per
distribution) lands as a Settings account section plus an `AuthService`
behind the veneer — off by default, zero weight when off. Toasts go to the
Action Center unpackaged (plus in-app cards everywhere) and to
`AppNotificationManager` when packaged.

![Settings account section — sign-in state before a client id is configured](docs/screenshots/auth.png)

![In-app notification card over Settings](docs/screenshots/notification.png)

## Run this repo locally

```powershell
dotnet build -c Debug -p:Platform=x64
dotnet run -c Debug -p:Platform=x64
```

## Quick demo

This repo is both a full sample app and the source of the project template. It demonstrates a working desktop app model that you can customize rather than build from a bare shell.

```text
App shell
├── Home page
├── Settings page (theme, language, updates, account, tray, backup)
├── Diagnostics page (status, metrics, live log tail, export bundle)
├── Setup wizard (first-run, portable only)
├── Toasts + native update dialogs (check → download → restart)
├── First-run welcome (installers own setup)
├── Tray + toast behavior
├── Update checks + restart prompt
├── Localization + theme persistence
└── Release pipeline ready to use
```

## FAQ

### Why does Check for updates say "only available for installed apps"?

That message is correct when the app runs unpackaged (`dotnet run` or a
loose exe): Velopack can only apply updates to an installed app. Run the
installed build (Start menu entry or `setup.exe` install) for real update
checks. If an *installed* build says it, check
`%LocalAppData%\DevTemWinUi3\Logs` — every check stage is logged there.

### Is this a good WinUI 3 starter template?

Yes. DevTem is designed as a production-ready starter for desktop apps, not a bare sample. It includes the plumbing teams usually end up recreating: update checks, tray integration, app settings, i18n, logging, and release automation.

### Is this better than the official blank WinUI app?

For a real application, yes. The official blank app is the correct starting point for learning WinUI or writing a minimal app from scratch. DevTem is the better starting point when you want a working desktop app foundation from day one.

### How do I ship to the Microsoft Store?

Scaffold with `--distribution msix --updates store --setup false --publisher "CN=Your-Publisher-ID"`, pack with `Scripts/build-msix.ps1`, and submit with `Scripts/submit-store.ps1` (or the `store-submit.yml` workflow). See the "Ship it" truth table above and `docs/feature-guides/distribution-dual.md` for the proven cells.

### How do I enable Entra ID sign-in?

Scaffold with `--auth true`, then configure a client id (see `docs/feature-guides/auth.md`). Sign-in stays off until configured; scaffolds without the flag carry no identity SDK at all.

### Do I need the `.slnx` file?

No. Scaffold with `--slnx true` only if you want an XML solution file for VS 2026+ alongside the classic `.sln` — build with the `.sln`, which alone carries the x86/x64/ARM64 mappings.

### How heavy is a scaffolded app?

The all-on reference publishes ~274 MB self-contained (WindowsAppSDK + .NET runtime included); every smaller flag combination is lighter, and nightly per-combo ceilings guard the trend. Cold start on a dev box is ~300 ms to splash, ~500 ms to window (see `docs/STATE.md` for the current flame).

## Repository layout

```text
Program.cs
App.xaml / App.xaml.cs
MainWindow.xaml / MainWindow.xaml.cs
Pages/
Services/
Controls/
Tests/
Templates/
Packaging/
Scripts/
.github/workflows/
```

## Support / contribute

- Source: https://github.com/Fettah010/winui-3-easy-template
- Issues: https://github.com/Fettah010/winui-3-easy-template/issues
- NuGet: https://www.nuget.org/packages/DevTem.Templates

If you find it useful, please give the repo a star. It helps more than you might think for discoverability, trust, and search visibility.

## License

MIT.
