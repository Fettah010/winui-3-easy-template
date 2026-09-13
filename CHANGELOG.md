# Changelog

All notable changes to DevTem-WinUI 3. `release.yml` sources the GitHub
release notes from the matching section below (falls back to a stub when
the version is missing, and fails when the tag disagrees with the csproj).

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
