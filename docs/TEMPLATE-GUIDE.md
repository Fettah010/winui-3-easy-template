# Template Guide — daily development on DevTem-WinUI 3

Recipes for everyday work: pages, settings, layout, sample removal. New
here? Start at [`GETTING-STARTED.md`](GETTING-STARTED.md) (scaffold to
first run). Cutting releases or keeping the mirror green?
[`MAINTAINERS.md`](MAINTAINERS.md). Follow the recipes below and you
keep every feature (updates, tray, i18n, logging) working.

Maintainers should treat [`docs/template-contract.md`](template-contract.md) as
the authoritative compatibility reference for symbols, feature boundaries,
generated files, and post-generation responsibilities. This guide focuses on
consumer workflows; the contract records what the template promises.

## 2. Add a page (7 steps, all required)

1. **XAML + code-behind** in `Pages/` following `HomePage`: root `Grid` →
   `ScrollViewer` (horizontal scrollbar `Disabled`) → viewport `Grid` →
   content `StackPanel` with `MaxWidth` (see "Layout" below).
2. **ViewModel** in `ViewModels/` (transient, `[ObservableProperty]` /
    `[RelayCommand]`), registered in `ServiceLocator.Initialize()`, injected
    into the page constructor — never `new`, never resolved at click time.
    Register the route's factory next to it (`PageFactory.Register("orders",
    () => new Pages.OrdersPage(GetRequiredService<OrdersPageViewModel>()))`) so
    navigation constructs the page with its VM.
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
    in `MainWindow` (`<devtem:routes>`), plus one line in
    `Services/NavigationRegistry.cs` (`_entries.Add(new NavEntry("orders",
    "NavOrders", Symbol.Shop, null, false, "NavOrdersItem"))` before the
    `<devtem:nav-entries>` end marker) — the rail builds from the registry,
    never from XAML. The label binds live via `NavEntry.Label` (no
    code-behind wiring; note: the built-in Settings item needs the explicit
    branch in `OnNavigated`).
6. **Responsive**: drive two-column → stacked switching from
    `ResponsiveLayout.ShouldUseNarrowPage(...)` in code-behind — `e.NewSize.Width`
    in `SizeChanged`, `ActualWidth` in `Loaded`/`OnNavigatedTo` — and early-out
    when the breakpoint is unchanged (resize drags fire ticks continuously;
    re-applying identical Grid lengths mid-resize reads as jitter);
    never `ColumnSpan` for stacking (spanned children join Auto sizing and blow
    the grid past the card); button rows go in `Controls/WrapPanel`.
    Padding-only pages (Settings, Diagnostics) use the same mechanism, not
    `VisualStateManager`/`AdaptiveTrigger` — VSM setters cannot retarget Grid
    rows/columns, so one mechanism covers padding AND grids; no page uses VSM.
7. **Keyboard**: new nav items get NO accelerator by default (avoid
    collisions). The reserved list lives in `Services/KeyboardShortcuts.cs`
    (Alt+Left/Right back/forward, Ctrl+, Settings, Esc dismisses the update
    flow) — claim a new chord there first, then wire it.

## 2b. Scaffold a page (recommended)

Two flows. Flow A does everything; Flow B is the same pieces by hand (use it
when the page needs custom wiring halfway).

### Flow A — one command

```powershell
.\Scripts\add-page.ps1 -Name Orders -Title "Order History" -Icon Shop
.\Scripts\add-page.ps1 -Kind list -Name Products -Title "Products" -Icon Shop
```

| Argument | Meaning | Default |
| --- | --- | --- |
| `-Kind` | `page` (hero-card content) or `list` (list/details with selection + empty state, `devtem-list-details`) | `page` |
| `-Name` | PascalCase page name (single word recommended) | (required) |
| `-Title` | Page title + nav label | `-Name` |
| `-Icon` | Nav icon (a WinUI `Symbol` member): Home, Document, Shop, Mail, Calendar, People, Globe, Pictures, Video, Camera, Map, Phone | `Document` |

The script scaffolds `devtem-page` (or `devtem-list-details` with
`-Kind list`) with your title/icon, pastes the strings
(EN + `TODO-translate` es/fr; list pages add empty-state + select-prompt
keys) into `Services/Localization/`, registers the
VM, adds route + bound nav item, then builds (0 warnings) and runs the tests. It refuses dirty trees,
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
| `Pages/OrdersPage.xaml` (+ `.xaml.cs`) | Card layout, `INavigationAware`, live loc bindings, responsive switch |
| `ViewModels/OrdersPageViewModel.cs` | Transient `[ObservableProperty]`/`[RelayCommand]` VM + `NavSymbol` |
| `Tests/ViewModels/OrdersPageViewModelTests.cs` | VM unit tests + icon/coverage tests |
| `OrdersPage.strings.md` | Ready-to-paste keys (EN + `TODO-translate` es/fr) — paste, then delete |

Wire-up (4 steps):

1. **Strings** — paste the snippet into `Services/Localization/EnStrings.cs`,
   `EsStrings.cs`, `FrStrings.cs` and delete the file (the coverage test
   fails until every language has the keys). es/fr land as `TODO-translate`
   markers — translate them before release.
2. **DI** — register the VM in `ServiceLocator.Initialize()`:
    ```csharp
    services.AddTransient<OrdersPageViewModel>();
    ```
    and its factory next to it (constructor injection — the page takes the
    VM as a parameter, never resolves it):
    ```csharp
    PageFactory.Register("orders", () => new Pages.OrdersPage(GetRequiredService<OrdersPageViewModel>()));
    ```
3. **Route + nav** — in `MainWindow`: `RegisterRoute("orders",
    typeof(OrdersPage))`, plus one registry line in
    `Services/NavigationRegistry.cs` (before `// </devtem:nav-entries>`):
    ```csharp
    _entries.Add(new NavEntry("orders", "NavOrders", Symbol.Shop, null, false, "NavOrdersItem"));
    ```
    The label binds live via `NavEntry.Label` — no XAML, no code-behind
    wiring (note: the built-in Settings item still needs its explicit
    branch in `OnNavigated`).
4. **Verify** — `dotnet build -c Debug -p:Platform=x64` (0 warnings),
   then `dotnet test` (the new `Strings_AreTranslated` +
   `NavSymbol_MatchesChosenIcon` tests prove the keys and icon landed).

Uninstall when done: `dotnet new uninstall .\Templates\Page`.

### Restyling the shell (read before replacing NavigationView)

`Scripts/add-page.ps1` wires navigation by appending one line to
`Services/NavigationRegistry.cs` (before `// </devtem:nav-entries>`) — it
never touches `MainWindow.xaml`, so restyles cannot break page wiring.
After any shell restyle, bind `NavigationRegistry.MenuEntries` /
`FooterEntries` (or hand-place items with `Tag` = route) and keep the
contract: one item per page, `Tag` equals the route registered under
`<devtem:routes>`, selection synced in `OnNavigated` (the built-in
Settings item needs its explicit branch). `MainWindow.xaml` documents the
full contract above `<!-- </devtem:nav-items> -->` (kept as the
documented fallback region for exotic shells).

### Hardening patterns (learned from production apps)

- **Dialogs:** never `ShowAsync()` a XAML-declared `ContentDialog`
  directly — a `Visibility="Collapsed"` declarer no-ops silently (no
  dialog, no exception, no log). Show every dialog through
  `Services/DialogHelper.cs` (uncollapses around the call, restores in
  `finally`, serializes concurrent shows). Button language: Primary is a
  verb (Install now, Restart now, Retry, Get Started), the dismiss button
  is a Close/Cancel/Later word per state (never two verbs that both
  dismiss); single-button info dialogs use one or the other, never a
  second verb. `Tests/Services/DialogButtonLanguageTests.cs` pins the
  FirstRun/whats-new/update trio per language — add new dialogs to its
  pair table.
- **Pickers:** never await a native picker unbounded — a wedged shell or
  automation host can stall it forever. `FilePickerService` races every
  call against a 60s timeout and degrades to cancel; UI tests assert the
  app *survives* picker attempts, not dialog pixels.
- **Icons:** every `Symbol="X"` string must be a real `Symbol` member —
  bad values compile green and throw `XamlParseException` on first
  navigation. `Tests/Services/XamlSymbolAuditTests.cs` audits all XAML
  against the known-good set (mirrors the item template's `--icon`
  choices); decorative icons should be `FontIcon` glyphs (bad glyph
  shows tofu, never throws).
- **Overlay math:** convert units once at the boundary through
  `Services/Helpers/UnitConversion.cs` (96-DPI pixels vs points) —
  `HardeningHelpersTests` pins A4 in both systems so a 75% shrink fails
  the build instead of landing highlights on empty lines.
- **Mutation repaints:** after merge/rotate/delete-style ops, bump a
  `Services/Helpers/ChangeEpoch` counter and have the view watch it —
  property-change notification is not change detection.
- **Cancellation scope must match the concurrency model:** per-call
  tokens for concurrent loads (one shared slot cancels siblings);
  reserve a shared slot for superseding single-canvas renders only.
  Review every shared `CancellationTokenSource` on sight.
- **DI gate:** a dropped `AddTransient` compiles green and explodes on
  user click. `Tests/Services/ServiceLocatorTests.cs`
  (`ViewModels_ResolveTransient`) resolves EVERY registered VM — adding
  a service touches one module plus that list.
- **Stale binaries:** the desktop shortcut targets the built exe, so it
  can launch an old binary where new routes do not exist.
  `Scripts/run-app.ps1` prints binary time vs repo HEAD at launch, and
  About shows the build commit (`AppInfo.BuildCommit`, baked from
  `DEVTEM_BUILD_COMMIT` in CI).





## 2d. Removing sample content

One command (the inverse of `add-page.ps1` — same dirty-tree guard and
rollback semantics):

```powershell
.\Scripts\remove-sample-content.ps1 -WhatIf   # preview
.\Scripts\remove-sample-content.ps1           # home-features + about-tech + about-links
.\Scripts\remove-sample-content.ps1 -Blocks home-features   # one block only
```

Sample blocks carry `devtem:optional:<id>` markers (XAML comments bracketing
the deletable range through `devtem:end:<id>`). The script cuts the XAML
range, the companions the marker names (code-behind layout lines,
`Services/Localization/*Strings.cs` keys), then builds (0 warnings) and
runs the tests. Manual fallback is the same three moves: cut the XAML
range, cut the companions, run build + tests. Current markers:

| Marker | What | Companions |
| --- | --- | --- |
| `home-features` | Home feature-cards grid | `FeatureCol1`/`FeatureCard2-4` lines in `HomePage.xaml.cs`, `HomeFeat*` loc keys |
| `about-tech` | About technology panel | `TechCol`/`TechPanel` lines in `AboutPage.xaml.cs`, `AboutCardUpdates/Logging/Mvvm/Ui*` loc keys |
| `about-links` | About links panel | `LinksCol`/`LinksPanel` lines in `AboutPage.xaml.cs`, `AboutCardSource/Releases/Issues*` loc keys |
| `settings-notify-test` | Settings test-toast card (dev scaffolding) | `SendTestToastButton_Click` in `SettingsPage.xaml.cs`, `SettingsNotifyTest*` loc keys |
| `diagnostics-page` | Whole Diagnostics page (header comment only) | Checklist in `Pages/DiagnosticsPage.xaml`: route, registry entry, page + VM + test files, `ServiceLocator` VM + factory registrations, `Diagnostics*` loc keys |

Whole-page removal (any page, e.g. About) follows the Diagnostics
checklist: route in `MainWindow.xaml.cs`, registry entry in
`Services/NavigationRegistry.cs`, page files, ViewModel (+
`ServiceLocator` VM + factory registrations), view-model tests, loc keys. Unused loc
keys are harmless at runtime, but remove them anyway — the coverage
tests assert exact translations per key. Settings sections work the same
way without markers: each `StackPanel` section (appearance, language,
updates, tray, backup) deletes independently; the updates section
additionally needs `--updates none` semantics (no engine registered →
status card reports the mode, check button hidden).

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

