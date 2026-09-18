# Changelog

All notable changes to DevTem-WinUI 3. `release.yml` sources the GitHub
release notes from the matching section below (falls back to a stub when
the version is missing, and fails when the tag disagrees with the csproj).

















## [0.0.21-beta] - 2026-09-18

### Changed

- Second half of the back-to-back update test: no app behavior changes
  besides the version itself. With `v0.0.20-beta` installed, this
  version proves updating on the fixed pipeline — visible toasts at
  every stage, and the Settings test toast as the render proof.

## [0.0.20-beta] - 2026-09-18

### Fixed

- Invisible toasts (the actual root cause of every "nothing happens"
  report): the toast card Border carried `Opacity="0"` while the
  entrance animation drives the card itself, so opacity multiplied to
  zero forever — no toast ever rendered on any machine. Removed, with
  a comment guarding the invariant.
- Startup identity log (version + binary path + packaged flag): the
  data dir and log file are shared between installed and dev runs, so
  "not installed" lines are now attributable to the right binary.

### Changed

- Setup splash goes dark mode: dark Win11 card art with a light-blue
  progress bar (`#4CC2FF`); generator takes `-Theme Dark|Light`.

## [0.0.19-beta] - 2026-09-18

### Changed

- Second half of the back-to-back update test: no app behavior changes
  besides the version itself. With `v0.0.18-beta` installed, this
  version proves steady-state updating on the instrumented flow — and
  the new Settings → Notifications test toast proves the cards render.

## [0.0.18-beta] - 2026-09-18

### Fixed

- Toast pipeline forensics: toast faults now log with the exception
  (they used to die silently in fire-and-forget tasks), and a
  **Show test toast** button in Settings → Notifications proves the
  in-app cards render on any machine independent of updates.
- No-update checks now always produce visible feedback: disabled
  "Checking…" button for the whole check plus the latest-version
  toast, both on the UI thread.

### Changed

- Setup.exe splash is rendered per release with the version pill,
  channel, payload size, and an "Installing…" caption on a Windows 11
  light card, with a Windows-accent progress bar.

## [0.0.17-beta] - 2026-09-17

### Changed

- Second half of the back-to-back update test: no app behavior changes
  besides the version itself. With `v0.0.16-beta` installed, this
  version proves steady-state updating (toast → dialog → progress →
  restart) on the fixed flow.

## [0.0.16-beta] - 2026-09-17

### Fixed

- Manual update checks that silently died after the network round-trip:
  the update dialog service now stays on the UI thread for its whole
  flow (background-thread UI calls threw into the never-throw guards
  and the user saw nothing), and toasts self-marshal to the UI thread
  so background callers can never lose them.
- Missing update feedback: a toast now announces "update available"
  before the install dialog, a toast confirms "download complete"
  before the restart prompt, and the no-update toast reliably appears.
- Home "Check for updates" now visibly drives the Settings check
  (same thread fix — the wiring was already there).

### Changed

- Velopack setup branding: branded splash art (`Assets/SetupSplash.png`)
  plus a Windows-accent (`#0078D4`) progress bar instead of the
  default green.

## [0.0.15-beta] - 2026-09-17

### Changed

- Update-test release: no app behavior changes besides the version
  itself. With `v0.0.14-beta` installed via `setup.exe`, this version
  exercises the fixed update flow end to end (busy check button →
  toast/dialog → download progress → restart prompt).

## [0.0.14-beta] - 2026-09-17

### Fixed

- "Check now" doing nothing with no feedback: each deferred window
  service now initializes in its own guard (one failure can no longer
  silently kill the update surface), the check button disables with a
  "Checking…" label for the whole in-flight check, and the update flow
  logs every stage to `Logs/applog-*.log` (including dropped toasts and
  a not-ready window) so a silent check is diagnosable.
- Release uploads slimmed ~33%: `Portable.zip` duplicates the full
  payload and the updater never reads it, so it is packed locally but
  no longer uploaded unless `-IncludePortableZip` is passed
  (~340MB/28min → ~230MB/~18min); the parallel uploader now logs
  totals (MB, elapsed, effective MB/s).

### Added

- Headless regression test proving every update-dialog entry completes
  with no window wired (never hangs).

## [0.0.13-beta] - 2026-09-17

### Changed

- Update demo release: no app behavior changes besides the version
  itself. Install `v0.0.12-beta` via `setup.exe`, then watch the new
  update flow pick this version up (toast on detect, native dialog for
  install, progress, restart prompt).

## [0.0.12-beta] - 2026-09-17

### Changed

- Setup wizard no longer auto-opens in the app: install-time choices
  (location, shortcuts, launch) belong to the installer — the MSIX
  package on packaged runs, Velopack setup on portable runs. First run
  lands on Home with a welcome dialog.
- Update experience rebuilt around toasts + native WinUI 3 dialogs:
  checking / up-to-date / not-installed report via the app's
  notification toasts; update-available and restart prompts are native
  `ContentDialog`s instead of the custom overlay.
- Manual update checks always terminate: unpackaged runs short-circuit
  with "updates are only available for installed apps" and feed checks
  race a 30s timeout (fixes checking forever on no-update).

### Fixed

- MSIX path re-validated (`build-msix -Validate` + `-DryRun` with the
  `.appinstaller` feed); portable Velopack defaults unchanged.

## [0.0.11-beta] - 2026-09-17

### Fixed

- Update-flow verification release: exercises the 0.0.10 popup end to
  end (background detection → silent download → animated restart
  prompt). No app behavior changes besides the version itself.

## [0.0.10-beta] - 2026-09-16

### Fixed

- Updates no longer hijack launch: Velopack auto-apply stays off and all
  update work runs past the first frame, so the app always opens first
  and asks afterwards (previously a staged update applied silently on
  start — click, nothing opens, second click shows the new version).
- Check-for-updates feedback: the animated update popup always answers
  (checking animation, up-to-date confirmation, progress, error + retry) —
  no more silent no-op when already on the latest version.
- Diagnostics Clear now clears the live buffer (was filters-only); the
  live list glides to new arrivals while unpaused; the file tail
  auto-refreshes instead of showing stale content. Fixed a concurrent
  counter race in the event buffer trimmer.
- READMEs describe the app (SEO navel-gazing removed); obsolete plan
  files deleted from both trees.

### Added

- Update Center page retired in favor of one animated popup
  (`Controls/UpdatePopup` + `UpdateDialogService`): checking (animated
  dots, linear bar — no spinner), release notes, in-popup download
  progress, Restart now / Later. Entry points: Settings, tray, Home,
  background detection.
- Settings "Install updates automatically" (default on): off means the
  popup asks before anything downloads. "Later" keeps the download
  staged and re-prompts after the next launch — never applies at startup.

## [0.0.9-beta] - 2026-09-16

### Added

- Performance & scalability plan P0-P3 (`docs/PERFORMANCE-SCALABILITY-PLAN.md`):
  launch without fixed waits (completion-driven splash/chrome transitions,
  reduced-motion + fast-launch gates), settings cache with debounced
  coalesced writes, non-blocking Velopack init, deferred tray/toast/icon
  work past the first frame.
- Steady-state caps: Release log level `Information`, 256 MB log-directory
  quota, O(1) event buffer, throttled download progress, debounced
  diagnostics filter with off-thread capped tail reads, bounded
  cancellable toasts.
- Data layer v1: WAL + busy-timeout, retryable init, `schema_version`
  migration runner, diagnostics DB path fix, `IRepository<T>` sample.
- Growth seams: modular DI (`AddData/AddUpdates/AddPresence/AddCore/AddHttp`),
  in-repo HTTP retry handler + cancellation + typed `ApiResult<T>`,
  per-page navigation timings on the diagnostics page, startup-budget
  guardrails, publish-weight tracking (`Scripts/measure-publish-weight.ps1`,
  baseline 268.2 MB).
- Tests: +52 headless tests (267 passed + 4 skipped); mirror parity green;
  FlaUI smoke 5 passed + 1 skipped; scaffold matrix green.

## [0.0.8-beta] - 2026-09-15

### Fixed

- Slow silent launch when an update waited: Velopack auto-apply ran
  before the window with no UI. Auto-apply is now off
  (`SetAutoApplyOnStartup(false)`); launches stay fast and a prepared
  update surfaces as a visible restart prompt after the first frame
  (new `IUpdateService.PendingRestartVersion`, apply falls back to the
  previous session's prepared update).
- Update Center card parked off-center until the first check: the page
  missed the viewport-width Grid wrapper (same latent trap as
  SettingsPage).

### Added

- Update Center upgrade: status/details sections, current-version and
  last-checked rows, auto-check on first arrival per session, refreshed
  copy in all three languages.

## [0.0.7-beta] - 2026-09-15

### Fixed

- Store upload wrap failed on PowerShell 7 (`Compress-Archive`
  rejects non-`.zip` destinations): zip to a temp name, then move it
  over the `.msixupload`. Proved by the `msix` CI leg.
- Smoke `App_Launches` failed on fresh machines (first run lands on
  the setup wizard, not Home): shared `CompleteSetupWizardIfPresent`
  helper, tolerated by the launch test. Proved by `ui-tests` CI.

## [0.0.6-beta] - 2026-09-15

### Added

- Dual-track distribution (plan `docs/DUAL-TRACK-PLAN.md`): one
  `portable + velopack` binary serves GitHub and the Store. Packaged
  runs take the slim update status surface via the new
  `AppFeatures.IsExternallyManaged` seam (scaffold modes unchanged);
  `release.yml` packs the Store upload on every tag when
  `DEVTEM_MSIX_PUBLISHER` is set, plus a `distribution-dual.md` guide.

## [0.0.4-beta] — 2026-09-14

Template flexibility overhaul (plan `docs/FLEXIBILITY-PLAN.md`,
Phases 0–4): vendor lock-ins become scaffold-time choices with smooth
defaults. No app behavior change on default scaffolds.

### Added

- `updates` template choice: `velopack` (default), `basic`
  (zero-dependency GitHub-releases checker behind the same
  `IUpdateService` seam), or `none` (`--updates basic`).
- `logging` template choice: `serilog` (default), `mel` (debugger +
  Event Log, no files), or `none` (`--logging mel`). App code logs
  through the backend-agnostic `AppLog` facade; the diagnostics buffer
  is backend-neutral (`LogEntry`).
- `crash`, `localization`, `tests` template flags: Sentry behind a
  no-op veneer when dropped, English-only UI (picker hides itself),
  and an empty test-project shell (`--crash false --localization false
  --tests false`).
- Per-value feature guides (`updates-basic.md`, `logging-mel.md`) and
  value-aware `docs/FEATURES.md` / `template-features.json`.

## [0.0.3-beta] — 2026-09-14

Diagnostics overhaul (plan `docs/DIAGNOSTICS-PLAN.md`, Phases 0–4), a
one-click dev loop, and a new `health` template flag.

### Fixed

- Diagnostics page logs not showing: view models now resolve before
  `InitializeComponent` so compiled bindings evaluate against the real
  instance (same latent fix in Settings/About).

### Added

- Diagnostics status: startup time, effective log level, log files
  (count + size), buffered events, packaged state, pending update,
  database size; Serilog `SelfLog`; startup-crash dialog with log path.
- Structured pipeline: in-memory ring buffer, enrichment (context,
  version, thread), runtime verbose toggle, optional JSON sidecar.
- Diagnostics page UX: live tail with pause-on-scroll, source/exception/
  regex filters, event detail flyout, save-view + bundle-zip export,
  virtualized list.
- Metrics, startup traces, Sentry breadcrumbs + status context, explicit
  crash-report opt-in, opt-in Windows Event Log sink.
- `health` template flag (`--health false` drops the diagnostics surface;
  pipeline stays), `nodiag` matrix combo, `docs/diagnostics.md` guide.
- `DevTem-WinUI 3 (Dev)` desktop shortcut: build-if-needed + launch,
  no terminal typing.

## [0.0.2-beta] — 2026-09-14

Profile-aware template documentation, feature composition, reusable page
generation, branding configuration, and validation improvements.

### Added

- Generated feature summaries and conditional feature guides.
- Stable template customization contract and documented profile presets.
- Safe page generation with routes, icons, localization snippets, and rollback.
- Product/deployment configuration seams and validated branding pipeline.
- Expanded scaffold matrix, parity checks, and generated-page UI validation.

### Changed

- Optional tray, updates, database, HTTP, and attribution features are
  documented and validated independently.

## [0.0.5-beta] — 2026-09-15

Distribution + installer release (plan `docs/DISTRIBUTION-PLAN.md`,
Phases 0–4): the template gains a `distribution` dimension, native
MSIX update values, and a first-run setup wizard. Defaults stay
`portable` + `velopack`, so default scaffolds behave as before.

### Added

- `distribution` template choice: `portable` (default, unpackaged) or
  `msix` (packaged, Store or sideload). One runtime-adaptive binary:
  data root, autostart (`StartupTask`), protocol, and singleton fork
  on `AppInfo.IsPackaged` — no compile-time forks.
- `updates` values `appinstaller` / `store` (msix-only): native
  `.appinstaller` feed support (`build-msix.ps1 -AppInstaller`,
  schema 2021) and Store submission path (`-StoreUpload`,
  Partner Center checklist). Slim status surface in Settings +
  Update Center when updates are externally owned.
- `setup` template flag (portable-only): first-run setup wizard
  (`SetupWizardPage`, PipsPager steps, persisted don't-show-again).
- Update Center page: check → download (live progress) → install
  with release notes; `basic` shares the same visual language
  through `IUpdateService`. Store preset:
  `--distribution msix --updates store --setup false`.
- Template package MINOR bump (`0.2.0` → `0.3.0`) per the standing
  version policy.

### Changed

Internal restructure. No behavior changes on default scaffolds — same features, cleaner
layers, all proven by the full verification tier.

- Localization: per-language files, XAML bindings (`{loc:Loc}`), instant
  switch everywhere, no hardcoded versions in strings.
- Settings update flow moved to the ViewModel (tested: 9 new tests);
  unit total 116 → 131.
- Window chrome, tray, and app orchestration decomposed into focused
  services (`WindowChromeService`, `WindowActivator`, `FirstRunDialogService`,
  `AutoStartService`, `BackgroundUpdateService`, `Services/Helpers/`).
- Restart and update prompts fully translated (last i18n gap closed).
- Toast cards are a XAML `NotificationCard` control now.

## [0.0.4-beta] — unreleased

Template-correctness and CI release. No app behavior changes.

### Added

- Mirror-parity check (`Scripts/test-mirror-parity.ps1`, CI-gated):
  app vs template file sets, verbatim hashes, csproj version agreement,
  nested page copy.
- Matrix rename-engine proofs: per-combo manifest/`ProtocolPrefix`
  assertions plus an `init-template` scratch re-brand.
- `register-protocol.ps1` dry run (`-WhatIf`) + CI step.
- Asset-mapping tables in both template guides; VS troubleshooting notes.

### Fixed

- `ui-tests` CI: sln mapped test projects to Any CPU, so `--no-build`
  never found the DLL — x64 mappings in both slns (same latent trap
  fixed in the template).
- `register-protocol.ps1` PS 5.1 parse error (`"$Scheme://"`).
- `schemeName` generator emitted a bare scheme (invalid manifest XML);
  now emits the full `Name="…"` attribute.

## [0.0.3-beta] — unreleased

### Added

- App icon pipeline: `Scripts/set-app-icon.ps1` regenerates `app.ico`
  (multi-entry 16–256) + `Logo*.png` + theme-aware `app-light/dark.ico`
  from one square source PNG, with backups and read-back verification.
- Page scaffolding upgrade: `devtem-page --title/--icon` params and
  one-command `Scripts/add-page.ps1` (strings, DI, route, nav, tests).
- Deep links end-to-end: `devtem://` parsing, per-user (HKCU)
  self-registration on first run, single-instance handoff, MSIX protocol
  extension, `Scripts/register-protocol.ps1`.
- Settings backup: reset-to-defaults + JSON export/import (theme,
  channel, tray, auto-check, language) with file pickers.
- In-app diagnostics page: status (version, channel, theme, language,
  crash reporting) + rolling-log viewer with refresh and open-folder.
- FlaUI smoke tests (`UI/` + `ui-tests.yml`): launch, navigate all pages,
  theme + language switch — hardened for occupied desktops (foreground +
  topmost staging, cursor confinement, click retries).

### Changed

- Tray and title-bar icons follow the app theme (light/dark variants).
- Smoke harness re-foregrounds the app window before every test.

### Fixed

- Smoke suite no longer fails when another window covers the app
  (clicks previously landed on the occluding window).
- `TestResults/` (MSTest + FlaUI output) is git-ignored.
