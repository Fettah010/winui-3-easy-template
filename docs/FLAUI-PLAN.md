# Implementation Queue — Plan (deferred)

Agreed order for upcoming sessions: **FlaUI first, then P5 → P6 → P7 picks.**
Nothing below is implemented; this file is the spec to implement from.

## 0. FlaUI Smoke Tests (first)

Status: IMPLEMENTED — `UI/DevTemWinUi3.SmokeTests/` (MSTest + FlaUI 5.0.0),
`.github/workflows/ui-tests.yml`, AutomationIds on nav items / combos /
page titles (app + `Templates/Project/` mirror). 4/4 green in ~24s local
(60s budget); theme + language restored after every run; failure screenshots
into `TestResults/` uploaded as CI artifacts. Guards: inconclusive when the
app exe is unbuilt or another instance runs (single-instance safety).
Context: all 93 unit tests run headless; nothing proves the app *runs*.
FlaUI (UI Automation) automates the eyeball check (see UPGRADE-ROADMAP §4).

## Scope (keep it tiny — smoke, not coverage)

New project `UI/DevTemWinUi3.SmokeTests/` (xunit or MSTest, net10.0-windows):
1. `App_Launches_And_ShowsHome` — start exe, wait for main window
   (title from `AppMetadata.AppName`), assert visible, kill process.
2. `Can_Navigate_All_Pages` — click nav items Home/About/Settings
   (AutomationIds — add `AutomationProperties.AutomationId` to the three
   nav items when implementing), assert each page title text appears.
3. `Theme_Switch_Applies` — Settings → theme combo → Dark → assert
   theme applied (read back combo selection at minimum).
4. `Language_Switch_Applies` — Settings → language combo → es-ES →
   assert nav Home label reads "Inicio".

Rules: every test waits with retries (app start, splash ~5s, first-run
dialog must be dismissed first — click "Get Started" if present);
screenshots on failure into `TestResults/`; total suite budget 60s;
`dotnet test` runs it like everything else. Flaky on CI runners is the
known risk — retries + generous timeouts, never `Thread.Sleep` constants
without a wait loop.

## CI shape (`.github/workflows/ui-tests.yml`, later)

`windows-latest`, after build: `dotnet test UI/...` only (unit tests stay
in the existing job). Needs a real Windows session — runners provide one.
Do NOT run UI tests on every template-matrix combo; run once against the
default app. Template flag-off runtime proof stays manual screenshots.

## Prerequisites before starting

- The §3 a11y names are in (done) — FlaUI locates controls through them.
- Add `AutomationId` to nav items + key buttons (names localize, Ids don't).
- Decide xunit vs MSTest to match Tests/ (MSTest today).

## P5 — App icon pipeline (`set-app-icon.ps1`)

Status: IMPLEMENTED — `Scripts/set-app-icon.ps1` (+ `Templates/Project/`
mirror, no template.json change: feature-independent), §1b in both
TEMPLATE-GUIDEs, `.icon-backup/` git-ignored. Verified on a temp Assets
copy from a generated 1024px source: 7-entry ICO (OS LoadImage-accepted),
5 PNGs at exact sizes with alpha intact, refusals (256px, non-square),
zero-write `-WhatIf`, timestamped backups; real `Assets/` untouched.

Problem: app art is 6+ hand-maintained files (`app.ico`, `Logo.png`,
`Logo-16/32/48/64`). No GUI is possible here — the VS template dialog only
renders text fields, checkboxes, and dropdowns (engine limit, not a gap) —
so the shape is one terminal command from VS's own terminal.

- Single source: square PNG, 1024px recommended (512 min), transparent bg,
  logo inside ~80% safe margins.
- `Scripts\set-app-icon.ps1 -Source C:\art\logo.png` generates via
  System.Drawing: `app.ico` {16,24,32,48,64,128,256} as multi-entry
  PNG-compressed ICO (~40-line writer; .NET can't emit multi-size ICO
  natively), `Logo.png` 256, `Logo-64/48/32/16` exact, `-WhatIf` preview,
  refuses non-square/tiny sources, backs up replaced files.
- MSIX tiles stay pack-time (`build-msix.ps1` already renders them) — no
  duplication. Template's own `icon.png` untouched (our branding).
- Mirror script + "App icons" section in both TEMPLATE-GUIDEs. No
  template.json changes (a path param would be dead data — CLI runs no
  post-actions, proven in P4).
- Verify: generate → build 0 warnings → screenshot Home/About/titlebar/tray
  → MSIX DryRun → matrix.

## P6 — Page scaffolding upgrade (titles, icons, one-command add)

Status: IMPLEMENTED (`92cb386` Layer 1, Layer 2 this commit).

Problem: `devtem-page -n Orders` + 4 manual wire-up steps; no title/icon
params; nav icon hardcoded by hand.

Layer 1 — new template params (CLI + VS dialog dropdown/textbox):
- `--title "Order History"` (text): XAML fallbacks + ready-to-paste loc snippet.
- `--icon Shop` (`datatype: choice`, ~12 Fluent members): template emits
  `Symbol.SampleIcon`, the value IS the enum member, `replaces` yields
  `Symbol.Shop` — no conditionals; `choice` guarantees validity. VM exposes
  `NavSymbol`.
- Strings gap (item templates can't edit files): emit
  `OrdersPage.strings.md` (EN + ES/FR TODO-translate); the coverage test
  fails until pasted — existing forcing function, extended.

Layer 2 — `Scripts\add-page.ps1 -Name Orders -Title "Order History" -Icon Shop`:
runs the template, inserts loc strings (3 dicts), AddTransient, route +
`<NavigationViewItem>` with `<SymbolIcon>`, nav-label line; builds + runs
page tests; refuses dirty trees (rollback-safe).
IMPLEMENTED: clean-tree guard (tracked edits only) + snapshot rollback,
namespace rewrite for renamed apps, tolerant template install, failure
output tails. Dogfooded in-repo (OrdersProbe: green, screenshotted, removed)
and in-matrix (OrdersSmoke in allon). Two deviations found by running:
$PSScriptRoot is invisible in param defaults (fixed here + P5 script), and
the nav insert must go BEFORE </NavigationView.MenuItems> (WMC0035).
- VS story (honest): dialog scaffold + 4 manual steps, OR one terminal
  command for zero manual steps. No custom VSIX wizard (rejected: duplication).
- Verify: scaffold → build → coverage green → screenshot → fresh `add-page`
  end-to-end → matrix → copy mirror + guide §2b rewrite.

## P7 — Next candidates (pick order when P6 lands)

1. Theme-aware tray/title icons (small) — light/dark variants from the theme
   listener. Pure Win11 polish.
2. Protocol registration end-to-end (medium) — installer + manifest
   registration for the deep-link scheme; parsing already exists.
3. Settings reset/export (small) — reset-to-defaults + JSON export/import.
4. In-app diagnostics viewer (medium) — page over `Logs/` + Sentry status.

## Cross-cutting rules

- Matrix gains: spaced-name coverage stays; add an `add-page.ps1` smoke.
- Docs: guide §2b rewrite (two flows), asset-mapping table for icons.
- Itemized verified commits (P5, P6, picks). Explicit non-goals: VS-dialog
  file pickers/wizards (engine can't), pre-generated MSIX tiles (pack-time
  does it).
