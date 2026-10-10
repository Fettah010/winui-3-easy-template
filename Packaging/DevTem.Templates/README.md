# DevTem Templates

[![NuGet](https://img.shields.io/nuget/v/DevTem.Templates)](https://www.nuget.org/packages/DevTem.Templates)
[![License](https://img.shields.io/badge/license-MIT-67ac09)](https://github.com/Fettah010/winui-3-easy-template/blob/main/LICENSE)

`dotnet new` templates for production-ready **WinUI 3 desktop apps** (.NET 10)
plus an MVVM content-page scaffolder. Install once, scaffold in seconds —
in the terminal or Visual Studio's New Project dialog (same engine).

```powershell
dotnet new install DevTem.Templates
```

**Requirements:** Windows 10 version 19041+ to run scaffolds · [.NET 10
SDK](https://dotnet.microsoft.com/download) to build · Visual Studio 2026
(18.x) recommended for the designer and the New Project dialog.

![DevTem home page — welcome card, status, feature overview](https://raw.githubusercontent.com/Fettah010/winui-3-easy-template/main/docs/screenshots/home.png)

## `devtem-winui` — full desktop app

Unpackaged WinUI 3 app: Mica window, system tray, auto-updates (Velopack
by default; basic checker or none; native AppInstaller / Store updates for
packaged MSIX), first-run setup wizard, optional SQLite and HTTP services,
3-language UI (EN/ES/FR; English-only available), persisted settings,
flexible logging (Serilog by default) + optional Sentry crash reports,
opt-in Entra ID sign-in (MSAL, off by default), MSIX packaging script,
release pipeline.

```powershell
dotnet new devtem-winui -n AcmeDesk --displayName "Acme Desk" --company "Acme" `
    --repo "acme/desk-app" --scheme "acme://" --tray false
```

| Parameter | Meaning |
| --- | --- |
| `-n` | Namespace, file names, mutex/registry/folders (`AcmeDesk`) |
| `--displayName` | Display name (titles, toasts, installer) |
| `--company` | Pack author |
| `--repo` | GitHub `org/name` (feeds, links, CI) |
| `--scheme` | Deep-link URI scheme (`acme://`) |
| `--tray` / `--database` / `--http` / `--health` / `--crash` / `--localization` / `--tests` | Feature on/off (`false` drops it; all default on) |
| `--auth` | Opt-in Entra ID sign-in (`true` includes it; off by default, configure a client id to enable) |
| `--slnx` | Opt-in XML solution file (`true` adds `Safe.slnx` next to the classic `.sln`; off by default — the `.sln` alone expresses the x86/x64/ARM64 mappings) |
| `--updates` | Auto-updates: `velopack` (default), `basic` (checker), `none` — or `appinstaller` / `store` for packaged MSIX |
| `--distribution` | Distribution format: `portable` (default) or `msix` (Store or sideload) |
| `--publisher` | MSIX Publisher ID (Partner Center or cert subject; pre-fills manifest + script) |
| `--setup` | First-run setup wizard (portable only; `false` drops it) |
| `--logging` | Logging backend: `serilog` (default), `mel`, `none` |
| `--attribution` | One-line source comment (`false` omits it) |

Names with spaces work (`-n "My App"` → `My_App` identifiers and files).
In Visual Studio the parameters render as dialog fields, checkboxes, and
dropdowns (the choices). Every generated project also includes `docs/FEATURES.md`, which records the
selected options and next steps; disabled feature guides are omitted.

![DevTem settings page — theme, language, updates, account, tray, backup](https://raw.githubusercontent.com/Fettah010/winui-3-easy-template/main/docs/screenshots/settings.png)

### After scaffolding

Your copy's `README.md` walks through first steps — reset the version,
rebrand identity/links/license, then prove consistency:

```powershell
powershell -File Scripts/bump-version.ps1 -Version 0.0.1
.\Scripts\init-template.ps1 -AppName "Acme Desk" -Company "Acme" `
    -RepoUrl "https://github.com/acme/desk-app" -Scheme "acme://"
.\Scripts\init-template.ps1 -Validate
```

### Profile presets

The template keeps one composable set of feature switches. These stable
presets provide coherent starting points without hiding the individual
options:

| Profile | Command flags | Included optional services |
| --- | --- | --- |
| Minimal | `--tray false --updates none --database false --http false --health false --logging none --crash false --localization false --tests false --attribution false` | None |
| Desktop | `--updates none --database false --http false` | Tray and desktop notifications |
| Production | no feature overrides | Tray, updates, SQLite, HTTP, attribution |
| Store | `--distribution msix --updates store --setup false` | Packaged MSIX for Store submission |
| Dual | `--publisher "CN=Your-ID"` (on defaults) | One binary for GitHub (Velopack) + Store (same MSIX) |

Apply a preset by copying its flags into the scaffold command, then change
any individual flag as needed. Individual flags are authoritative; there is
no separate `--profile` parameter.

## `devtem-page` — content pages

XAML + code-behind (`INavigationAware`, localization, responsive) +
transient ViewModel + MSTest stub. Wire-up (strings, DI, route) takes
~5 minutes; the generated test proves the strings landed in all languages.

```powershell
dotnet new devtem-page -n Orders                                    # hero-card content
dotnet new devtem-list-details -n Products                           # master/details
dotnet new devtem-datagrid -n Inventory                              # sortable rows (DataGrid 7.1.2)
dotnet new devtem-contentgrid -n Catalog                             # reflowing cards + search/sort/paging
dotnet new devtem-tabview -n Workspace                              # document tabs
```

Or wire any kind in one command inside the app repo:
`Scripts/add-page.ps1 -Kind page|list|grid|contentgrid|tab`.

## Updating

```powershell
dotnet new update --check-only
dotnet new update
```

Template versions ride `templates-v*` tags (currently `0.6.0`, matched with
the app); per-release notes live on
[GitHub releases](https://github.com/Fettah010/winui-3-easy-template/releases).

## Links

- Source + docs: <https://github.com/Fettah010/winui-3-easy-template>
- Issues: <https://github.com/Fettah010/winui-3-easy-template/issues>
- License: MIT
