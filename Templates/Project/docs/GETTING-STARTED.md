# Getting started — scaffold to first run in 15 minutes

You scaffolded (or cloned) a DevTem app. This page takes you to a running,
renamed, rebranded app. Daily development continues in
[`TEMPLATE-GUIDE.md`](TEMPLATE-GUIDE.md) (pages, settings, layout);
template maintenance lives in [`MAINTAINERS.md`](MAINTAINERS.md).
Bootstrap order stays `AGENTS.md` → `docs/STATE.md` → `docs/WORKFLOW.md`.

## 1. Rename the template (do this first)

Identity lives in two layers. Code surfaces read `Services/Helpers/AppMetadata.cs`
— change the values there. Text/XAML surfaces are replaced by
`Scripts/init-template.ps1`, which also verifies zero leftovers.

| Token | Meaning | Example |
| --- | --- | --- |
| `DevTemWinUi3` | Namespace, mutex/event names, registry key, settings folder | `AcmeDesk` |
| `DevTem-WinUI 3` | Display name (titles, toasts, installer) | `Acme Desk` |
| `Fettah010/winui-3-easy-template` | GitHub repo (feeds, links, CI) | `acme/desk-app` |
| `Fettah` | Pack author | `Acme` |
| `devtem://` (+ `Name="devtem"` in the MSIX manifest) | Deep-link URI scheme | `acme://` |

```powershell
.\Scripts\init-template.ps1 -AppName "Acme Desk" -Company "Acme" -RepoUrl "https://github.com/acme/desk-app" -Scheme "acme://"
```

The script also renames `DevTemWinUi3[.Tests].csproj` to match, and fails if
any template identity remains. Frozen on purpose: `docs/archive/` (reference)
and this guide (it documents the tokens).

Manual spots the script cannot do: your app art — see §1b below, then run
`Scripts/create-shortcut.ps1` again.

### Configuration and extension seams

`Services/Configuration/ProductConfiguration.cs` is the generated,
secret-free product/deployment configuration file. Set product defaults there
after scaffolding:

- `PrimaryColor` and `AccentColor` are the documented brand color values
  (`#RRGGBB`) used when extending the app theme.
- `SupportUrl`, `PrivacyUrl`, and `UpdateFeedUrl` are deployment placeholders.
- `SentryDsn`, `SentryEnvironment`, and `SentryRelease` configure optional
  crash reporting.

Developer and CI overrides use environment variables with the `DEVTEM_`
prefix. Environment values take precedence over generated defaults; empty
values fall back to the generated file. Never commit DSNs or tokens.
Template-time identity and feature flags are fixed by `dotnet new`; runtime
user preferences belong in `SettingsService`.

Stable generated-region markers provide safe extension points:

- `<devtem:services>` in `Services/ServiceLocator.cs`
- `<devtem:routes>` in `MainWindow.xaml.cs`
- `<devtem:nav-items>` in `MainWindow.xaml` (nav shell: mode, items, selection)
- `Services/SettingsService.cs` for persisted settings
- `Services/Localization/{En,Es,Fr}Strings.cs` for matching localization keys

`Scripts/add-page.ps1` uses unique anchors, refuses dirty repositories, rolls
back on failure, and reports each operation. Add custom services and routes
at the markers instead of editing infrastructure registration or navigation
lifecycle code.

For a page workflow, use `dotnet new devtem-page -n Orders --route orders
--title "Order History" --icon Shop`. The item template always emits the page,
code-behind, transient ViewModel, MSTest stub, and three-language strings
snippet. `-Route` must be lowercase kebab-case and unique. The supported
one-command integration is:

```powershell
.\Scripts\add-page.ps1 -Name Orders -Route orders -Title "Order History" -Icon Shop
```

Use `-WhatIf` first to preview the files and registrations. Existing page
names, routes, anchors, or localization keys are rejected rather than
overwritten; this makes reruns safe and preserves user-owned code.

Generated page titles expose stable `AutomationProperties.AutomationId`
values such as `OrdersTitleText`, so FlaUI or another UI automation harness
can verify navigation without depending on translated text.

## Branding and assets

Use `Scripts/set-app-icon.ps1 -Source .\path\to\logo.png` as the single
replacement workflow. The source must be a square PNG of at least 512x512
(1024x1024 is recommended), with transparent background and roughly 80%
safe margins. The script produces the ICO variants and `Logo.png`,
`Logo-64.png`, `Logo-48.png`, `Logo-32.png`, and `Logo-16.png` used by the
title bar, tray, shortcut, splash, Home, About, installer, and MSIX tile
pipeline. Use `-WhatIf` to preview writes; invalid paths, extensions, aspect
ratios, and dimensions fail before assets are changed.

Keep `AppMetadata` and `ProductConfiguration` as the product-facing source of
truth. `create-shortcut.ps1` targets the generated executable and
`Assets/app.ico`; MSIX tile art is derived from `Assets/Logo.png` by
`build-msix.ps1`.

## 1b. App icons (one command)

All icon assets regenerate from a single square source PNG (1024px
recommended, 512 minimum, transparent background, logo inside ~80% safe
margins — tight edge-to-edge art clips in circular hosts):

```powershell
.\Scripts\set-app-icon.ps1 -Source C:\art\logo.png
```

This writes `Assets/app.ico` (multi-entry 16–256 for taskbar, title bar,
installer, tray, shortcuts) plus `app-light.ico`/`app-dark.ico` copies
(theme-aware tray/title icons prefer them, `app.ico` fallback),
`Assets/Logo.png` (256: splash, Home, About)
and `Assets/Logo-64/48/32/16.png`, verifies every output by reading it
back, and backs up the replaced files to `.icon-backup\<timestamp>\`
(git-ignored). Preview with `-WhatIf`; point at a copied assets folder
with `-AssetsDir` for dry runs. MSIX tiles need no step: `build-msix.ps1`
renders them from `Logo.png` at pack time.

| Source (you draw) | Generated | Used by |
| --- | --- | --- |
| Square PNG (1024 rec., 512 min, transparent, ~80% safe margins) | `Assets/app.ico` {16,24,32,48,64,128,256} | Taskbar, title bar, installer, tray, shortcuts |
| | `Assets/app-light.ico` / `app-dark.ico` (copies) | Theme-aware tray/title icons (`app.ico` fallback) |
| | `Assets/Logo.png` (256) | Splash, Home hero, About |
| | `Assets/Logo-16/32/48/64.png` (exact) | Title bar (32), cards |
| `Assets/Logo.png` at pack time | MSIX tiles (StoreLogo, Square44/150, Wide310, SplashScreen) | Rendered by `build-msix.ps1` — never hand-edit |

## Scaffold a whole app (dotnet new project template)

`Templates/Project/` is the full app as a project template with identity
parameters and feature flags (all flags default on):

```powershell
dotnet new install .\Templates\Project     # from this repo, or:
dotnet new install DevTem.Templates::<version>   # versioned NuGet package
dotnet new devtem-winui -n AcmeDesk --displayName "Acme Desk" --company "Acme" `
    --repo "acme/desk-app" --scheme "acme://" --tray false --updates none --database false --http false --health false --logging none --crash false --localization false --tests false --attribution false
```

**Presets (no flag archaeology).** `Scripts/init-profile.ps1` bundles the
flags into three presets — `minimal` (leanest build), `recommended`
(everything on, portable + Velopack), `full` (everything on, MSIX) — with
optional `-Distribution`/`-Updates` overrides. Omit `-Preset` for the
interactive menu; `-WhatIf` prints the `dotnet new` command without running
it. The template itself gains no `--profile` parameter (the engine forbids
it by decision) — the script is sugar over the same flags above.

**Visual Studio (no VSIX needed).** VS's New Project dialog runs the same
template engine: after `dotnet new install` (path or NuGet package),
`devtem-winui` appears in File → New → Project with its icon, and the
parameters render as dialog fields (text), checkboxes (bool flags), and
dropdowns (choice parameters like `--updates`), all set to their defaults. No separate extension to build or maintain — the NuGet
package IS the Visual Studio distribution channel. Not seeing a new
version in VS? VS caches templates: close VS, run
`dotnet new update` (or uninstall + reinstall the package), reopen.
`devtem-page` never appears in VS (Add → New Item is a VS-only surface
with no template-engine support) — pages stay CLI-only by engine design,
not by choice.

| Parameter | Replaces | Example |
| --- | --- | --- |
| `-n` | Namespace, file names, mutex/registry/folders | `AcmeDesk` |
| `--displayName` | Display name (titles, toasts, installer) | `Acme Desk` |
| `--company` | Pack author | `Acme` |
| `--repo` | GitHub `org/name` (feeds, links, CI) | `acme/desk-app` |
| `--scheme` | Deep-link scheme (registered end-to-end: HKCU self-register on first run, manifest for MSIX; `Scripts/register-protocol.ps1` for manual setup) | `acme://` |
| `--tray/--database/--http/--health/--crash/--localization/--tests` | Feature on/off (`false` drops it) | `--tray false` |
| `--updates` | Update mechanism: `velopack` (default), `basic` (checker), `none` — or `appinstaller` / `store` for packaged MSIX (`--distribution msix`) | `--updates basic` |
| `--distribution` | Distribution format: `portable` (default, unpackaged) or `msix` (packaged, for Store or sideload) | `--distribution msix` |
| `--setup` | First-run setup wizard: install location, shortcuts, launch options (`false` drops it; portable only, ignored for msix) | `--setup false` |
| `--publisher` | MSIX Publisher ID: Partner Center ID (Store) or cert subject (sideload); pre-fills the manifest + `build-msix.ps1` | `--publisher "CN=Acme"` |
| `--attribution` | Keep the one-line DevTem source comment in generated `DevTemAttribution.cs` | `true` |

Names with spaces work: `-n "My App"` produces `My_App` identifiers and
project files (engine sanitization), and the build stays clean — the test
matrix covers a spaced name every run.


### Stable starter profiles

The public template remains one composable template. Use these documented
presets when you want a coherent starting point without learning every
internal service:

| Profile | Flags | Result |
| --- | --- | --- |
| Minimal | `--tray false --updates none --database false --http false --health false --logging none --crash false --localization false --tests false --attribution false` | Shell, settings, English UI |
| Desktop | `--updates none --database false --http false` | Minimal plus tray and desktop notifications |
| Production | no overrides | All optional services and release tooling |
| Store | `--distribution msix --updates store --setup false` | Packaged MSIX for Store submission (tray stays valid packaged; autostart moves to `StartupTask`) |
| Dual | `--publisher "CN=Your-ID"` (on Production defaults) | One binary for GitHub (Velopack) + Store (same MSIX); see `distribution-dual.md` |

Copy a preset's flags into the scaffold command and override any individual
flag. Individual flags always win; profiles are intentionally documentation
presets rather than a second template symbol.

## First run (5 minutes)

```powershell
dotnet build -c Debug -p:Platform=x64         # 0 warnings, 0 errors
dotnet run -c Debug -p:Platform=x64           # unpackaged dev run
Scripts\bump-version.ps1 -Version 0.0.1       # reset the inherited template version first (E6: one command, no magic)
```

Then verify: replace `Assets` art (see "App icons" above), grep
`TODO-translate` and translate the es/fr markers before release, open
every page once (Home, Settings, About, Diagnostics), and run
`dotnet test`. Daily work continues in
[`TEMPLATE-GUIDE.md`](TEMPLATE-GUIDE.md).
