# Template Guide — building YOUR app on DevTem-WinUI 3

This is a starting template, not a finished app. Follow the recipes below and
you keep every feature (updates, tray, i18n, logging) working.

## 1. Rename the template (do this first)

Identity lives in two layers. Code surfaces read `Services/AppMetadata.cs`
— change the values there. Text/XAML surfaces are replaced by
`Scripts/init-template.ps1`, which also verifies zero leftovers.

| Token | Meaning | Example |
| --- | --- | --- |
| `DevTemWinUi3` | Namespace, mutex/event names, registry key, settings folder | `AcmeDesk` |
| `DevTem-WinUI 3` | Display name (titles, toasts, installer) | `Acme Desk` |
| `Fettah010/winui-3-easy-template` | GitHub repo (feeds, links, CI) | `acme/desk-app` |
| `Fettah` | Pack author | `Acme` |

```powershell
.\Scripts\init-template.ps1 -AppName "Acme Desk" -Company "Acme" -RepoUrl "https://github.com/acme/desk-app"
```

The script also renames `DevTemWinUi3[.Tests].csproj` to match, and fails if
any template identity remains. Frozen on purpose: `docs/archive/` (reference)
and this guide (it documents the tokens).

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
installer, tray, shortcuts), `Assets/Logo.png` (256: splash, Home, About)
and `Assets/Logo-64/48/32/16.png`, verifies every output by reading it
back, and backs up the replaced files to `.icon-backup\<timestamp>\`
(git-ignored). Preview with `-WhatIf`; point at a copied assets folder
with `-AssetsDir` for dry runs. MSIX tiles need no step: `build-msix.ps1`
renders them from `Logo.png` at pack time.

## 2. Add a page (6 steps, all required)

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
   `LocalizationService`, `x:Name` every user-facing element, apply them in
   one `ApplyLocalization()` method called from constructor, `OnNavigatedTo`,
   and (Settings-style) after `SetLanguage`.
5. **Route**: `NavigationService.RegisterRoute("orders", typeof(OrdersPage))`
   in `MainWindow`, plus a `NavigationViewItem` (menu or footer) and a
   selection-sync case (note: the built-in Settings item needs the explicit
   branch in `OnNavigated`).
6. **Responsive**: drive two-column → stacked switching from
   `ResponsiveLayout.ShouldUseNarrowPage(ActualWidth)` in code-behind
   (`SizeChanged` + `Loaded` + `OnNavigatedTo`); never `ColumnSpan` for
   stacking (spanned children join Auto sizing and blow the grid past the
   card); button rows go in `Controls/WrapPanel`.

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
(EN + `TODO-translate` es/fr), registers the VM, adds route + nav item +
label, then builds (0 warnings) and runs the tests. It refuses dirty trees,
and any failure rolls the tree back. Afterwards: replace the
`TODO-translate` markers (grep for them), run the app, check the new nav item.

### Flow B — manual (~5 minutes)

Item templates have no VS dialog (Add → New Item is VS-only territory) — the
CLI below works in the VS terminal too:

```powershell
dotnet new install .\Templates\Page
dotnet new devtem-page -n Orders --title "Order History" --icon Shop
```

This generates 5 files (`Sample` → `Orders`):

| File | Contents |
| --- | --- |
| `Pages/OrdersPage.xaml` (+ `.xaml.cs`) | Card layout, `INavigationAware`, `ApplyLocalization`, responsive switch |
| `ViewModels/OrdersPageViewModel.cs` | Transient `[ObservableProperty]`/`[RelayCommand]` VM + `NavSymbol` |
| `Tests/ViewModels/OrdersPageViewModelTests.cs` | VM unit tests + icon/coverage tests |
| `OrdersPage.strings.md` | Ready-to-paste keys (EN + `TODO-translate` es/fr) — paste, then delete |

Wire-up (4 steps):

1. **Strings** — paste the snippet into all three dictionaries in
   `LocalizationService` and delete the file (the coverage test fails until
   every language has the keys). es/fr land as `TODO-translate` markers —
   translate them before release.
2. **DI** — register the VM in `ServiceLocator.Initialize()`:
   ```csharp
   services.AddTransient<OrdersPageViewModel>();
   ```
3. **Route + nav** — in `MainWindow`: `RegisterRoute("orders",
   typeof(OrdersPage))`, add a `NavigationViewItem` (`Tag="orders"`,
   `<SymbolIcon Symbol="Shop"/>`), and its label in
   `ApplyNavLocalization()` (the `NavOrders` key is in the snippet).
4. **Verify** — `dotnet build -c Debug -p:Platform=x64` (0 warnings),
   then `dotnet test` (the new `Strings_AreTranslated` +
   `NavSymbol_MatchesChosenIcon` tests prove the keys and icon landed).

Uninstall when done: `dotnet new uninstall .\Templates\Page`.

## 2c. Scaffold a whole app (dotnet new project template)

`Templates/Project/` is the full app as a project template with identity
parameters and feature flags (all flags default on):

```powershell
dotnet new install .\Templates\Project     # from this repo, or:
dotnet new install DevTem.Templates::<version>   # versioned NuGet package
dotnet new devtem-winui -n AcmeDesk --displayName "Acme Desk" --company "Acme" `
    --repo "acme/desk-app" --scheme "acme://" --tray false --updates false --database false
```

Releases of the package are cut with `templates-v*` tags (CI packs +
pushes to NuGet); every push touching templates runs the scaffold matrix
(`Scripts/test-templates.ps1`: all flag combos built with 0 warnings and
tested).

**Visual Studio (no VSIX needed).** VS's New Project dialog runs the same
template engine: after `dotnet new install` (path or NuGet package),
`devtem-winui` appears in File → New → Project with its icon, and the
parameters render as dialog fields (text) and checkboxes (the three flags,
checked by default). No separate extension to build or maintain — the NuGet
package IS the Visual Studio distribution channel.

| Parameter | Replaces | Example |
| --- | --- | --- |
| `-n` | Namespace, file names, mutex/registry/folders | `AcmeDesk` |
| `--displayName` | Display name (titles, toasts, installer) | `Acme Desk` |
| `--company` | Pack author | `Acme` |
| `--repo` | GitHub `org/name` (feeds, links, CI) | `acme/desk-app` |
| `--scheme` | Deep-link scheme | `acme://` |
| `--tray/--updates/--database` | Feature on/off (`false` drops it) | `--tray false` |

Names with spaces work: `-n "My App"` produces `My_App` identifiers and
project files (engine sanitization), and the build stays clean — the test
matrix covers a spaced name every run.

How flags work (verified over all-on, all-off, and mixed scaffolds):

- **Files**: services, tests, release scripts and feature packages are
  excluded per flag (`sources.modifiers` in `template.json`).
- **Code**: `.cs` `#if (tray|updates|database)` blocks (the engine only
  evaluates markers in code files).
- **Packages**: `Build/Features.*.props` imported with `Exists` guards, so
  one static csproj serves every combo.
- **UI**: no markers in `.xaml` (the engine ignores them there) — instead
  `Services/AppFeatures.cs` (values rendered from the flags) gates
  visibility, and `HomePage` fills its card grid from the visible cards.
- **Docs**: `README`/`AGENTS` rows carry `(feature)` qualifiers.

Engine gotchas learned while building it (do not regress):

- `dotnet new` CLI does NOT run script post-actions (IDE-only); there is no
  post-scaffold script — the design above needs none.
- Never author `$var`-heavy `.ps1` content relying on engine behavior; keep
  template scripts plain.
- The nested page template ships dormant
  (`Templates/Page/config.hold/template.json.hold`) so installing the project
  package does not register a stale global `devtem-page`; scaffolded apps
  rename it back per §2b.
- `init-template.ps1` still works in scaffolded apps for re-branding.

### Releasing template updates (upload + smooth updates)

The whole flow is automated; the maintainer only cuts a tag:

```powershell
git tag templates-v0.2.0
git push origin templates-v0.2.0
# CI (templates-publish.yml): dotnet pack -p:Version=0.2.0, push to NuGet
```

Prerequisites (one time, no secrets): on nuget.org go to your account →
Trusted Publishing → Create, and register a policy for package
`DevTem.Templates` from this repo + workflow `templates-publish.yml`.
The first push through that policy also reserves the package ID for you.
CI (`templates-publish.yml`) authenticates with OIDC — there is no API key
to create, store, or rotate. (Manual `dotnet nuget push` from your machine
still needs a classic API key; prefer tags so every release is traceable.)
Local dry run before tagging: `dotnet pack Packaging/DevTem.Templates -o
nupkgs`, then install the file and scaffold once.

Users update with one command (VS picks it up in its dialog too):

```powershell
dotnet new update --check-only        # what's new
dotnet new update                     # update all template packages
```

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
  shift the layout: viewport `Grid` → `StackPanel` with `MaxWidth`.
- Text always `TextWrapping="Wrap"`; horizontal rows either fit provably or
  use `WrapPanel`; `ScrollViewer` horizontal scrollbar stays `Disabled`.
- Breakpoints live in `Services/ResponsiveLayout.cs` (single source) and are
  unit-tested; the nav pane compacts below 860px window width.
- Verify with screenshots: switch EN ↔ ES at 900px and at 1920px; card edges
  must be pixel-identical (see `Tests/` ABA approach in git history).

## 5. Releases

Bump `<Version>`/`<AssemblyVersion>`/`<FileVersion>` (keep in sync) plus
`<InformationalVersion>` (`-beta` suffix on beta releases so fresh installs
default to the beta channel). Commit, push, tag (`v0.0.3-beta`), push the tag
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
call with `--signParams` once you hold a cert.

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
- CI (`msix.yml`) proves the pipeline on every change with an ephemeral
  self-signed cert and uploads the package + cert.
