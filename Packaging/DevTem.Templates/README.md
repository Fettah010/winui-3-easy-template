# DevTem Templates

[![NuGet](https://img.shields.io/nuget/v/DevTem.Templates)](https://www.nuget.org/packages/DevTem.Templates)
[![License](https://img.shields.io/badge/license-MIT-67ac09)](https://github.com/Fettah010/winui-3-easy-template/blob/main/LICENSE)

`dotnet new` templates for production-ready **WinUI 3 desktop apps** (.NET 10)
plus an MVVM content-page scaffolder. Install once, scaffold in seconds —
in the terminal or Visual Studio's New Project dialog (same engine).

```powershell
dotnet new install DevTem.Templates
```

## `devtem-winui` — full desktop app

Unpackaged WinUI 3 app: Mica window, system tray, auto-updates (Velopack
by default; basic checker or none; native AppInstaller / Store updates for
packaged MSIX), first-run setup wizard, optional SQLite and HTTP services,
3-language UI (EN/ES/FR; English-only available), persisted settings,
flexible logging (Serilog by default) + optional Sentry crash reports,
MSIX packaging script, release pipeline.

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
| `--updates` | Auto-updates: `velopack` (default), `basic` (checker), `none` — or `appinstaller` / `store` for packaged MSIX |
| `--distribution` | Distribution format: `portable` (default) or `msix` (Store or sideload) |
| `--setup` | First-run setup wizard (portable only; `false` drops it) |
| `--logging` | Logging backend: `serilog` (default), `mel`, `none` |

Names with spaces work (`-n "My App"` → `My_App` identifiers and files).
In Visual Studio the parameters render as dialog fields, checkboxes, and
dropdowns (the choices). Every generated project also includes `docs/FEATURES.md`, which records the
selected options and next steps; disabled feature guides are omitted.

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

Apply a preset by copying its flags into the scaffold command, then change
any individual flag as needed. Individual flags are authoritative; there is
no separate `--profile` parameter.

## `devtem-page` — content page

XAML + code-behind (`INavigationAware`, localization, responsive) +
transient ViewModel + MSTest stub. Wire-up (strings, DI, route) takes
~5 minutes; the generated test proves the strings landed in all languages.

```powershell
dotnet new devtem-page -n Orders
```

## Updating

```powershell
dotnet new update --check-only
dotnet new update
```

## Links

- Source + docs: <https://github.com/Fettah010/winui-3-easy-template>
- Issues: <https://github.com/Fettah010/winui-3-easy-template/issues>
- License: MIT
