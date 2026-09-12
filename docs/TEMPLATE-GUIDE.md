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

Manual spots the script cannot do: regenerate `Assets/app.ico` + `Logo*.png`
with your art, then run `Scripts/create-shortcut.ps1` again.

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

## 2b. Scaffold a page (dotnet new, recommended)

The 6-step recipe above ships as an item template. Install once per machine,
then scaffold from the repo root:

```powershell
dotnet new install .\Templates\Page
dotnet new devtem-page -n Orders
```

This generates 4 files (`Sample` → `Orders`):

| File | Contents |
| --- | --- |
| `Pages/OrdersPage.xaml` (+ `.xaml.cs`) | Card layout, `INavigationAware`, `ApplyLocalization`, responsive switch |
| `ViewModels/OrdersPageViewModel.cs` | Transient `[ObservableProperty]`/`[RelayCommand]` VM |
| `Tests/ViewModels/OrdersPageViewModelTests.cs` | VM unit tests + translation-coverage test |

Wire-up (4 steps, ~5 minutes):

1. **Strings** — add `OrdersTitle`/`OrdersDescription` to all three
   dictionaries in `LocalizationService` (the coverage test fails until
   every language has them):
   ```csharp
   ["OrdersTitle"] = "Orders",       // es: "Pedidos", fr: "Commandes"
   ["OrdersDescription"] = "...",    // es/fr translations
   ```
2. **DI** — register the VM in `ServiceLocator.Initialize()`:
   ```csharp
   services.AddTransient<OrdersPageViewModel>();
   ```
3. **Route + nav** — in `MainWindow`: `RegisterRoute("orders",
   typeof(OrdersPage))`, add a `NavigationViewItem` (`Tag="orders"`),
   and its label in `ApplyNavLocalization()` (+ a `NavOrders` string key).
4. **Verify** — `dotnet build -c Debug -p:Platform=x64` (0 warnings),
   then `dotnet test` (the new `Strings_AreTranslated` test proves the
   keys landed in all three languages).

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

| Parameter | Replaces | Example |
| --- | --- | --- |
| `-n` | Namespace, file names, mutex/registry/folders | `AcmeDesk` |
| `--displayName` | Display name (titles, toasts, installer) | `Acme Desk` |
| `--company` | Pack author | `Acme` |
| `--repo` | GitHub `org/name` (feeds, links, CI) | `acme/desk-app` |
| `--scheme` | Deep-link scheme | `acme://` |
| `--tray/--updates/--database` | Feature on/off (`false` drops it) | `--tray false` |

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
