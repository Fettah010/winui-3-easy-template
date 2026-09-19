# Template Guide — building YOUR app on DevTem-WinUI 3

> Scaffolded via `dotnet new devtem-winui`: the namespace, display name,
> company, repo and URI scheme were already replaced from your scaffold
> parameters. Disabled features were removed at scaffold time (their files
> and packages are absent) and their UI is gated at runtime
> (`Services/AppFeatures.cs`) — sections for missing features simply stay
> hidden. `§1` below stays useful if you ever re-brand with
> `Scripts/init-template.ps1`.

This is a starting template, not a finished app. Follow the recipes below and
you keep every feature (updates, tray, i18n, logging) working.

Read `docs/FEATURES.md` first. It is generated for this scaffold and records
which optional features and feature guides are present.

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
| `devtem://` (+ `Name="devtem"` in the MSIX manifest) | Deep-link URI scheme (self-registered per-user on first run; `Scripts/register-protocol.ps1` for manual setup) | `acme://` |

```powershell
.\Scripts\init-template.ps1 -AppName "Acme Desk" -Company "Acme" -RepoUrl "https://github.com/acme/desk-app" -Scheme "acme://"
```

The script also renames `DevTemWinUi3[.Tests].csproj` to match, and fails if
any template identity remains. Frozen on purpose: `docs/archive/` (reference)
and this guide (it documents the tokens).

### Configuration and extension seams

`Services/Configuration/ProductConfiguration.cs` contains secret-free product
and deployment defaults for support, privacy, update-feed, and Sentry values.
Developer or CI overrides use the corresponding `DEVTEM_` environment
variables, which take precedence without committing secrets. Runtime user
preferences remain owned by `SettingsService`.

Use the explicit extension markers when adding application code:

- `<devtem:services>` in `Services/ServiceLocator.cs`
- `<devtem:routes>` in `MainWindow.xaml.cs`
- `<devtem:nav-items>` in `MainWindow.xaml` (nav shell: mode, items, selection)
- `Services/SettingsService.cs` for persisted settings
- all localization dictionaries for new keys (`EnStrings` only in
  English-only scaffolds)

`Scripts/add-page.ps1` validates unique anchors, refuses dirty repositories,
and rolls back on failure.

The page item template supports `--route`, `--title`, and `--icon`. The
supported integration command is
`Scripts/add-page.ps1 -Name Orders -Route orders -Title "Order History" -Icon Shop`.
Use `-WhatIf` for a change preview. Existing names and routes are rejected;
the tool never overwrites user-owned page or registration code.
Generated page titles expose stable automation IDs such as
`OrdersTitleText` for FlaUI or other UI automation checks.

Branding defaults live in `Services/Configuration/ProductConfiguration.cs`.
Use `Scripts/set-app-icon.ps1 -Source .\logo.png` with a square PNG of at
least 512x512; it validates the input and regenerates the ICO and logo sizes
used by the app, tray, shortcut, splash, About, installer, and MSIX pipeline.

Manual spots the script cannot do: your app art — see §1b below, then run
`Scripts/create-shortcut.ps1` again.

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

## 2. Add a page (6 steps, all required)

## 2b. Scaffold a page (recommended)

Two flows. Flow A does everything; Flow B is the same pieces by hand (use it
when the page needs custom wiring halfway).

### Flow A — one command

```powershell
.\Scripts\add-page.ps1 -Name Orders -Title "Order History" -Icon Shop
```

| Argument | Meaning | Default |
| --- | --- | --- |
| `-Name` | PascalCase page name (single word recommended) | (required) |
| `-Title` | Page title + nav label | `-Name` |
| `-Icon` | Nav icon (a WinUI `Symbol` member): Home, Document, Shop, Mail, Calendar, People, Globe, Pictures, Video, Camera, Map, Phone | `Document` |

The script scaffolds `devtem-page` with your title/icon, pastes the strings
into `Services/Localization/` (EN + `TODO-translate` es/fr; EN only in
English-only scaffolds, where the script is unsupported — add strings to
`EnStrings.cs` by hand), registers the
VM (rewriting the `DevTemWinUi3.*` namespaces to yours automatically), adds
route + bound nav item, then builds (0 warnings) and runs the tests. It
refuses dirty trees, and any failure rolls the tree back. Afterwards: replace
the `TODO-translate` markers (grep for them), run the app, check the new nav
item.

Needs a `devtem-page` template: install once via
`dotnet new install DevTem.Templates` (NuGet). If your app came from this
repo you can point at its item template instead:
`.\Scripts\add-page.ps1 -TemplateSource .\Templates\Page` — not needed for
scaffolded apps (that folder is not shipped). `devtem-page` is CLI-only
(it never appears in VS Add → New Item — engine limit, not a gap). After
updating the template package, if VS still shows the old parameters:
close VS, run `dotnet new update`, reopen (VS caches templates).

### Flow B — manual (~5 minutes)

The 6-step recipe above ships as an item template. Its config ships dormant
as `config.hold/template.json.hold` (so installing the project template does
not register a stale global copy) — activate, install, then scaffold:

```powershell
Rename-Item .\Templates\Page\config.hold\template.json.hold template.json
Rename-Item .\Templates\Page\config.hold .template.config
dotnet new install .\Templates\Page
dotnet new devtem-page -n Orders --title "Order History" --icon Shop
```

This generates 5 files (`Sample` → `Orders`):

| File | Contents |
| --- | --- |
| `Pages/OrdersPage.xaml` (+ `.xaml.cs`) | Card layout, `INavigationAware`, live loc bindings, responsive switch |
| `ViewModels/OrdersPageViewModel.cs` | Transient `[ObservableProperty]`/`[RelayCommand]` VM + `NavSymbol` |
| `Tests/ViewModels/OrdersPageViewModelTests.cs` | VM unit tests + icon/coverage tests |
| `OrdersPage.strings.md` | Ready-to-paste keys (EN + `TODO-translate` es/fr) — paste, then delete |

Wire-up (5 steps):

0. **Namespaces** — replace `DevTemWinUi3` with your root namespace in the
   4 code files (`x:Class`, usings, namespaces). Flow A does this for you.
1. **Strings** — paste the snippet into `Services/Localization/EnStrings.cs`,
   `EsStrings.cs`, `FrStrings.cs` and delete the file (the coverage test
   fails until every language has the keys). es/fr land as `TODO-translate`
   markers — translate them before release.
2. **DI** — register the VM in `ServiceLocator.Initialize()`:
   ```csharp
   services.AddTransient<OrdersPageViewModel>();
   ```
3. **Route + nav** — in `MainWindow`: `RegisterRoute("orders",
   typeof(OrdersPage))`, add a `NavigationViewItem` (`Tag="orders"`,
   `Content="{loc:Loc Key=NavOrders}"`, `<SymbolIcon Symbol="Shop"/>`).
   The label binds — no code-behind wiring.
4. **Verify** — `dotnet build -c Debug -p:Platform=x64` (0 warnings),
   then `dotnet test` (the new `Strings_AreTranslated` +
   `NavSymbol_MatchesChosenIcon` tests prove the keys and icon landed).

Uninstall when done: `dotnet new uninstall .\Templates\Page`.

1. **XAML + code-behind** in `Pages/` following `HomePage`: root `Grid` →
   `ScrollViewer` (horizontal scrollbar `Disabled`) → viewport `Grid` →
   content `StackPanel` with `MaxWidth` (see "Layout" below).
2. **ViewModel** in `ViewModels/` (transient, `[ObservableProperty]` /
   `[RelayCommand]`), registered in `ServiceLocator.Initialize()`, resolved
   in the page via `ServiceLocator.GetRequiredService<T>()` — never `new`.
3. **Navigation awareness**: implement `INavigationAware`
   (`Services/NavigationService.cs`) instead of overriding `OnNavigatedTo` —
   the service calls it with the navigation parameter.
4. **Strings**: add `HomeTitle`-style keys to all three dictionaries in
   `Services/Localization/` (`En/Es/FrStrings.cs`) and bind them in XAML
   with `{loc:Loc Key=…}` (`Controls/LocExtension.cs`) — no code-behind
   mapping; language switches apply instantly. State-driven text (busy
   states, composed status lines) stays in code and refreshes on
   `LanguageChanged` — see `SettingsPage`/`DiagnosticsPage`.
5. **Route**: `NavigationService.RegisterRoute("orders", typeof(OrdersPage))`
   in `MainWindow` (`<devtem:routes>`), plus a `NavigationViewItem` (menu or
   footer, in the `<devtem:nav-items>` region) and a selection-sync case
   (note: the built-in Settings item needs the explicit branch in `OnNavigated`).
6. **Responsive**: drive two-column → stacked switching from
   `ResponsiveLayout.ShouldUseNarrowPage(...)` in code-behind — `e.NewSize.Width`
   in `SizeChanged`, `ActualWidth` in `Loaded`/`OnNavigatedTo` — and early-out
   when the breakpoint is unchanged (resize drags fire ticks continuously;
   re-applying identical Grid lengths mid-resize reads as jitter);
   never `ColumnSpan` for stacking (spanned children join Auto sizing and blow
   the grid past the card); button rows go in `Controls/WrapPanel`.

## 2d. Removing sample content

Sample blocks carry `devtem:optional:<id>` markers (XAML comments bracketing
the deletable range through `devtem:end:<id>`). Deleting a block is three
moves: cut the XAML range, cut the companions the marker names
(code-behind layout lines, `Services/Localization/*Strings.cs` keys), and
run build + tests. Current markers:

| Marker | What | Companions |
| --- | --- | --- |
| `home-features` | Home feature-cards grid | `FeatureCol1`/`FeatureCard2-4` lines in `HomePage.xaml.cs`, `HomeFeat*` loc keys |
| `about-tech` | About technology panel | `TechCol`/`TechPanel` lines in `AboutPage.xaml.cs`, `AboutCardUpdates/Logging/Mvvm/Ui*` loc keys |
| `about-links` | About links panel | `LinksCol`/`LinksPanel` lines in `AboutPage.xaml.cs`, `AboutCardSource/Releases/Issues*` loc keys |
| `settings-notify-test` | Settings test-toast card (dev scaffolding) | `SendTestToastButton_Click` in `SettingsPage.xaml.cs`, `SettingsNotifyTest*` loc keys |
| `diagnostics-page` | Whole Diagnostics page (header comment only) | Checklist in `Pages/DiagnosticsPage.xaml`: route, nav item, page + VM + test files, `ServiceLocator` registration, `Diagnostics*` loc keys |

Whole-page removal (any page) follows the Diagnostics checklist: route in
`MainWindow.xaml.cs`, nav item (+ `OnNavigated` selection case) in
`MainWindow.xaml`, page files, ViewModel (+ `ServiceLocator`
registration), view-model tests, loc keys. Features you scaffolded out
are already gone (files absent, UI runtime-gated in
`Services/AppFeatures.cs`) — the checklist above only concerns pages you
can see. Settings sections work the same way without markers: each
`StackPanel` section (appearance, language, updates, tray, backup)
deletes independently.

## 3. Add a setting

1. Key + property in `Services/SettingsService.cs` (backed by
   `LocalSettingsStore`, JSON file — never `ApplicationData.LocalSettings`,
   which does not persist unpackaged).
2. `[ObservableProperty]` in the page ViewModel with a guarded change handler
   (see `SettingsPageViewModel`: load without side effects, ignore invalid
   values so programmatic refreshes never corrupt persisted state).
3. `SettingsCard` in `SettingsPage.xaml` with localized header/description.

## 4. Layout rules (learned the hard way)

- Page content width must NEVER depend on content length, or translations
  shift the layout: viewport `Grid` → `Stretch` `StackPanel` with `MaxWidth`
  (centers above the cap by design; mid-slide calm comes from the idempotent
  code-behind guard, not from the wrapper).
- Text always `TextWrapping="Wrap"`; horizontal rows either fit provably or
  use `WrapPanel`; `ScrollViewer` horizontal scrollbar stays `Disabled`.
- Breakpoints live in `Services/Helpers/ResponsiveLayout.cs` (single source) and are
  unit-tested; the nav pane is an overlay (`LeftCompact`) that never resizes the
  content, so toggles reflow nothing — breakpoints only fire on window resizes.
- Verify with screenshots: switch EN ↔ ES at 900px and at 1920px; card edges
  must be pixel-identical (see `Tests/` ABA approach in git history).

## 5. Releases

Bump `<Version>`/`<AssemblyVersion>`/`<FileVersion>` (keep in sync) plus
`<InformationalVersion>` (`-beta` suffix on beta releases so fresh installs
default to the beta channel). Commit, push, tag (`v0.0.1-beta`), push the tag
(CI builds/packs/uploads), then move the `beta`/`stable` pointer. Full flow
is in `AGENTS.md` ("Branches & releases").

### Code signing (do this before distributing)

Unsigned installers trip SmartScreen. Velopack signs every PE it packs
(your exe, its `Update.exe`, the setup) when you pass signtool args:

```powershell
vpk pack ... --signParams "/fd SHA256 /td SHA256 /f C:\certs\app.pfx /tr http://timestamp.digicert.com"
```

Notes from the Velopack docs (verified against `vpk pack -h`):

- Use **absolute paths** in the params; vpk may invoke signtool elsewhere.
- Get signing working on **one binary with signtool first**, then move it
  into `--signParams`. Quote-with-backslash anything containing spaces.
- Secrets belong in **env, not the command line**: every `vpk` option also
  reads `VPK_*` (e.g. `VPK_SIGN_PARAMS`). In CI, store the PFX base64-encoded
  in a GitHub secret, decode it at workflow time, pass password via secret.
- Alternatives: `--signTemplate "<cmd> {{file}}"` for custom signers, and
  `--azureTrustedSignFile` for Azure Trusted Signing.
- Test locally with a self-signed cert (`New-SelfSignedCertificate`),
  imported into Trusted People so your machine trusts it.
- Reputation is separate from validity: brand-new certs still SmartScreen-warn
  until trust builds; EV certs skip the queue, OV certs wait it out.

`Scripts/build-and-release.ps1` does not sign today — extend its `vpk pack`
call with `--signParams` once you hold a cert (omit the whole section with
`--updates none`: no installer exists to sign).

### MSIX packaging (sideload or Store)

`Packaging/Msix/Package.appxmanifest` + `Scripts/build-msix.ps1` produce an
MSIX from a Release publish (tile art is generated from `Assets/Logo.png`,
version is synced from the csproj). This **replaces the Velopack installer**,
not the app: under MSIX the in-app updater reports "not installed" by design
(Store/AppInstaller owns updates), and registry autostart does not apply —
the manifest registers a disabled StartupTask instead, and the app disables
its autostart toggle itself when packaged (`AppInfo.IsPackaged`).

```powershell
powershell -File Scripts/build-msix.ps1 -DryRun     # validate without SDK
powershell -File Scripts/build-msix.ps1 -Publisher "CN=Acme" -CertificatePath C:\certs\app.pfx -CertificatePassword "secret"
```

- `Publisher` MUST match the signing certificate subject.
- Unsigned packages validate the pipeline but cannot be installed — sign
  them, even self-signed for testing.
