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

Unpackaged WinUI 3 app: Mica window, system tray, Velopack auto-updates,
SQLite + HTTP data layer, 3-language UI (EN/ES/FR), persisted settings,
Serilog logging + optional Sentry crash reports, MSIX packaging script,
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
| `--tray` / `--updates` / `--database` | Feature on/off (`false` drops it; all default on) |

Names with spaces work (`-n "My App"` → `My_App` identifiers and files).
In Visual Studio the parameters render as dialog fields and checkboxes.

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
