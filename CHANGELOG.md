# Changelog

All notable changes to DevTem-WinUI 3. `release.yml` sources the GitHub
release notes from the matching section below (falls back to a stub when
the version is missing, and fails when the tag disagrees with the csproj).


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
