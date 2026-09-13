# Restructure Plan — Codebase Audit & Cleanup

Source: full audit of app-layer code (2026-09-13). Status per phase below.
Execution rules: app + `Templates/Project/` mirror move together, proven with
`test-mirror-parity.ps1` + matrix. Verification tier from `docs/WORKFLOW.md`.
Bars: 0 warnings, unit all-pass, parity OK, matrix PASSED (template touched),
MSIX DryRun (manifest touched), smoke 4/4 (UI touched).
Commit per phase; never push/tag without a direct user request.

## Audit summary

Well-kept codebase (consistent naming, honest comments, 116 unit + 4 smoke
tests). Problems are structural: logic in the wrong layer, one 470-line data
blob, triplicated native code.

## Findings

- **F1 — MVVM violation:** update flow (check/download/install/progress,
  auto-check orchestration, backup pickers) lives in `Pages/SettingsPage.xaml.cs`
  (444 lines); `SettingsPageViewModel.cs` (166) holds only toggles. The most
  complex flow is untestable (0/116 tests cover it). → Phase 2
- **F2 — Localization blob + mechanical mapping:** 3 languages inline in
  `Services/LocalizationService.cs:43–468` (~470 lines data); every page
  hand-assigns strings (`ApplyLocalization`, ~150 lines total) instead of
  binding. `HomeShipBody` hardcodes `v0.0.4-beta` (3×) → version bumps touch
  loc files. → Phase 1
- **F3 — `MainWindow.xaml.cs` (510) god-file:** ctor wires 12 subsystems;
  inline P/Invoke subclassing; title-bar math; dialogs. Topmost `SetWindowPos`
  dance triplicated (MainWindow ×2, SystemTrayService ×1). → Phase 3
- **F4 — `SystemTrayService.cs` (583) does 3 jobs:** tray icon/window,
  HKCU auto-start registry (static methods on instance service), 150-line
  P/Invoke region; event declared mid-file (`:553`). → Phase 4
- **F5 — `App.xaml.cs`:** orchestration + `PromptRestart` hardcoded English
  (only unlocalized user-facing text). → Phase 5
- **F6 — `Services/` mixes services with pure helpers** (`ChannelResolver`,
  `ResponsiveLayout`, `AppIconService`, `AppMetadata`, `AppInfo`, DTO
  `TrayNavigationRequest`); no interfaces anywhere. → Phase 6
- **F7 — Hygiene:** `NotificationService.Show:47` is `public async void`;
  `App.m_window` naming; `ApiService` has zero call sites. → Phase 6
- **Good, do not touch:** `ProtocolService`, `ServiceLocator` root,
  `ChannelResolver`, `LocalSettingsStore`, `ResponsiveLayout` math,
  `WrapPanel/WrapLayout` + tests, `Program.cs` single-instance flow.
  `DatabaseService.Settings` table is app-data KV (not a rival to
  `SettingsService`) — document, don't merge.

## Phase 0 — Delete old plans + baseline ✅ DONE
- Deleted `docs/RELEASE-PLAN.md` (shipped v0.0.4-beta; runbook folded into
  `docs/WORKFLOW.md` first). Refreshed `docs/STATE.md`.
- Keep: `AGENTS.md`, `WORKFLOW.md`, `DECISIONS.md`, `TEMPLATE-GUIDE.md`,
  `docs/archive/` (reference-only).
- Baseline: build 0 warn, unit 116/116, parity OK (81 files).

## Phase 1 — Localization overhaul ✅ DONE
1. Split dictionaries → `Services/Localization/{EnStrings,EsStrings,FrStrings}.cs`
   (service file → ~100 lines logic).
2. `HomeShipBody` version → format slot via `GetString(key, args)` from
   `AppInfo.Current.Version` (bumps never touch loc files again).
3. `LocalizationService` implements `INotifyPropertyChanged` + `this[string key]`
   indexer (raises `Item[]`); `Controls/LocExtension.cs` (`{loc:Loc Key=…}`)
   replaces all `ApplyLocalization()` (~150 lines deleted). Automation `Name`s move to
   XAML attributes (smoke tests use Ids — unaffected).
4. Mirrored to `Templates/Project/` **with `#if (tray|updates)` guards
   preserved**; extended `LocalizationCoverageTests`
   (key-parity exists — add "no hardcoded version" assertion).
5. Verified: FULL tier (build 0/0, unit 118/118, parity OK 87, matrix
   PASSED, smoke 4/4 — incl. `Language_Switch_Applies` through bindings).
6. `add-page.ps1` rewritten for the split (per-file inserts, bound nav
   item); item template teaches bindings; both TEMPLATE-GUIDEs updated.

## Phase 2 — Update flow → ViewModel ✅ DONE
1. `Services/Abstractions/` (`IUpdateService`, `UpdateCheckResult`,
   `IFilePickerService`) + `Services/FilePickerService.cs`. `IDialogService`
   skipped (YAGNI — the flow has no dialogs).
2. Check/download/install/progress/auto-check-delegate + export/import
   orchestration moved from `SettingsPage.xaml.cs` (424 lines) to
   `SettingsPageViewModel` (~380, fully covered); `UpdateService` holds the
   pending update internally (Velopack types never leave the service —
   `App.xaml.cs` migrated to the same API). Page keeps only view timing
   (first-render wait, CTS, armed flag) + combo reselect (~180 lines).
3. `SettingsUpdateFlowTests`: 9 tests (VM + fakes, incl. busy-guard,
   cancel, export/import round-trip). Unit total 118 → 128.
4. Template mirror hand-merged (tray/updates guards restored in VM,
   ServiceLocator, App); `UpdateServiceTests` untouched (only covers
   surviving members).
5. Verified: FULL tier (build 0/0, unit 128/128, parity OK 92, matrix
   PASSED incl. flag-off combos, smoke 4/4).

## Phase 3 — MainWindow decomposition ✅ DONE
1. `Services/WindowChromeService.cs` (Mica, title bar, colors, icon,
   min-size subclassing, entrance animation; P/Invoke in
   `Services/Native/WindowChromeNative.cs`).
2. First-run/what's-new dialogs → `Services/FirstRunDialogService.cs`.
3. One `Services/WindowActivator.cs`; all 3 `SetWindowPos` copies deleted
   (MainWindow ×2, SystemTrayService ×1).
4. Ctor is compose/routes/restore/wire; file 505 → 241 lines.
5. Verified: FULL tier (build 0/0, unit 128/128, parity OK 96, matrix
   PASSED, smoke 4/4 ×2 — one environmental red in between, rerun green,
   no code change).
6. Template mirror hand-merged (7 tray guards restored, directive count
   matches HEAD).

## Phase 4 — SystemTrayService split ✅ DONE
1. P/Invoke + structs → `Services/Native/TrayNative.cs` (dead imports
   dropped: subclass accessors, FindWindow, unused consts).
2. Auto-start → `Services/AutoStartService.cs` (instance singleton;
   ViewModel call sites swapped). No DI registration (Current-direct, like
   NotificationService).
3. Event to top; service keeps icon/window/menu: 566 → 334 lines.
4. Template mirror: verbatim (file excluded when !tray) except VM, where
   tray guards dropped 4 → 1 (new code only touches unconditional types).
5. Verified: FULL tier (build 0/0, unit 128/128, parity OK 98, matrix
   PASSED, smoke 4/4).

## Phase 5 — App.xaml.cs orchestration ✅ DONE
1. `Services/BackgroundUpdateService.cs` (update check, periodic loop,
   localized restart prompt) + `Services/DatabaseInitializer.cs` (split so
   the template engine can exclude each feature per flag).
2. Restart prompt localized (4 keys × 3 — the last hardcoded user-facing
   English); `App.xaml.cs` 232 → ~140 lines, launch-only.
3. Tests: `BackgroundUpdateServiceTests` (unpackaged guard) +
   `DatabaseInitializerTests` + restart-prompt coverage. Unit 128 → 131.
4. Template mirror: guards restored; `template.json` excludes extended;
   `test-mirror-parity.ps1` allowlist gained the new conditioned file.
5. Verified: FULL tier (build 0/0, unit 131/131, parity OK 101, matrix
   PASSED — incl. a noupd failure fixed by the service split, smoke 4/4).

## Phase 6 — Folder honesty + hygiene ✅ DONE
1. `Services/Helpers/` for `ChannelResolver`, `ResponsiveLayout`,
   `AppIconService`, `TrayNavigationRequest`, `AppMetadata`, `AppInfo`
   (namespace unchanged → zero code churn; `git mv` both trees; path refs
   in AGENTS/guides/scripts/comments updated).
2. `NotificationService.Show`: `async void` → `async Task` (helpers discard;
   faulted toasts can no longer crash the process).
3. `App.m_window` → `MainWindowInstance` (app + template + FilePicker +
   ThemeService).
4. `ApiService`: kept + documented as the template's HTTP starting point
   (point BaseAddress at your API or delete + registration + tests).
5. Verified: FULL tier (build 0/0, unit 131/131, parity OK 101, matrix
   PASSED — incl. fixing the identity check's AppMetadata path, smoke 4/4).

## Phase 7 — Notification cards to XAML ✅ DONE
`Controls/NotificationCard.xaml(.cs)` (layout in XAML, type visuals applied
once in code) + `NotificationItem` model; service keeps host lifetime and
motion. `DismissCard(Border)` → `(FrameworkElement)`.
Verified: FULL tier (build 0/0, unit 131/131, parity OK 103, matrix
PASSED, smoke 4/4).

## Expected outcome
LocalizationService 558→~100 (+3 data files) · SystemTrayService 583→~300 (+2)
· MainWindow 510→~250 (+2) · SettingsPage 444→~150 (logic into tested VM) ·
net −350 lines, update flow tested, zero `ApplyLocalization`, no hardcoded
versions in strings, folders that mean what they say.

## Carried debt (from retired RELEASE-PLAN.md P3)
- Dependabot: add `github-actions` ecosystem (only NuGet covered).
- Scaffolded apps get unit tests but no smoke harness (`UI/` doesn't ship) —
  make it an explicit documented decision either way.
