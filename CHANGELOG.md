# Changelog

All notable changes to DevTem-WinUI 3. `release.yml` sources the GitHub
release notes from the matching section below (falls back to a stub when
the version is missing, and fails when the tag disagrees with the csproj).

## [0.0.5-beta] — unreleased

Internal restructure release. No behavior changes — same features, cleaner
layers, all proven by the full verification tier.

### Changed

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
