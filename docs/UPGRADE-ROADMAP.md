# Upgrade Roadmap — WinUI 3 Desktop Template (.NET 10)

Goal: the best WinUI 3 desktop starting template for Windows 11 — modern,
fast, and verified. Every item below ships only with 0 warnings, 0 errors,
green tests, and (for UI-affecting changes) a live run + screenshot.

Status: `[ ]` todo · `[~]` in progress · `[x]` done.

Done so far (history): P0 hygiene + P1 identity + P2 DI-first (commit
`247905f`), P3 `devtem-page` scaffolder (`edfccd1`), P4 `devtem-winui`
project template with `--tray/--updates/--database` flags (`7f6932e`).

## 1. Ship the templates properly (highest leverage) `[x]`

Verified 2026-09-13: `DevTem.Templates.0.1.0.nupkg` packs warning-free (94
entries, both `template.json`, `.gitignore` included); nupkg install →
scaffold all-off → build 0 warnings → 70/70 tests; `test-templates.ps1`
MATRIX PASSED (4 combos + page probe, 0 warnings each).

- [x] `docs/UPGRADE-ROADMAP.md` (this file) tracks the work.
- [x] NuGet-pack both templates as `DevTem.Templates` (versioned,
  `PackageType=Template`, content from `Templates/`), so users install with
  `dotnet new install DevTem.Templates::<version>` — no git clone needed.
- [x] CI publishes the package on tag push (`templates-v*` → version from
  tag → `dotnet pack` + `dotnet nuget push`, `NUGET_API_KEY` secret).
- [x] CI template matrix: `Scripts/test-templates.ps1` + workflow job that
  scaffolds all-on / all-off / no-tray / no-updates (+ a `devtem-page`
  sample) on every push/PR and builds + tests each.
- [x] Dependabot for NuGet (`.github/dependabot.yml`, weekly).

## 2. Fill the real app gaps `[x]`

- [x] Periodic update checks while trayed (6h timer honoring auto-check +
  installed guards; `UpdateService.PeriodicCheckInterval` + tests; live log
  proof).
- [x] Crash reporting (Sentry 6.11.0, DSN-gated off by default;
  `CrashReportingService` + hooks + tests; live disabled-path proof).
- [x] Code-signing guidance (TEMPLATE-GUIDE §5: `--signParams`,
  `VPK_*` env secrets, self-signed testing, EV/OV reputation notes).
- [x] MSIX packaging: `Packaging/Msix/Package.appxmanifest` +
  `Scripts/build-msix.ps1` (publish, tile art from `Logo.png`, version
  sync, makeappx pack, optional signtool sign, `-DryRun`) + `msix.yml` CI
  (ephemeral self-signed cert, uploads package + cert). Packaged runs
  disable the registry autostart toggle themselves (`AppInfo.IsPackaged`).
  Verified: DryRun green (staging + manifest sync inspected), matrix green,
  Settings screenshot. Full makeappx/sign proof happens in CI (no SDK here).

- [ ] Periodic update checks while trayed (timer honoring the auto-check
  setting). Small, completes the tray story. Today: startup + manual only.
- [ ] Crash reporting (Sentry) — biggest diagnostics gap left; Serilog files
  only help if users send them. (Was ROADMAP #3.)
- [ ] MSIX/packaged option (Store distribution, clean uninstall) — biggest
  capability unlock; possibly a `--packaging` flag. Large effort.
- [ ] Code-signing guidance (SmartScreen flags unsigned Velopack
  installers): docs + script hooks for cert signing. Cheap, unblocks real
  releases.

## 3. Professional polish `[x]`

- [x] `.sln` (classic; slnx cannot express the x86/x64/ARM64 mapping) +
  init-template rename handling; rename-proof scratch builds 0 warnings.
- [x] Accessibility audit: the 6 unnamed controls (Settings toggles,
  combos, segmented) now get localized `AutomationProperties.Name`;
  buttons/nav items already announce via text Content; zero hardcoded
  colors (all theme brushes, high-contrast safe).
- [x] Bars enforced: `Directory.Build.props` (TWAE + Recommended
  analyzers + justified CA1822/CA1707 suppressions), `.editorconfig`;
  fixed real findings (ThrowIf helpers, InvariantCulture, cached
  JsonSerializerOptions, P/Invoke marshaling, UpdateService IDisposable).
  Verified: solution build 0 warnings, 93/93, matrix green, live runs.

- [ ] Add a `.sln` (+ rename handling in `init-template.ps1`) — VS opens
  solutions, not csprojs.
- [ ] Accessibility audit: automation properties on nav/buttons,
  screen-reader pass, high-contrast check.
- [ ] Enforce the bars in-repo: `TreatWarningsAsErrors`, NetAnalyzers,
  EditorConfig — "0 warnings" is tribal knowledge today; make the build
  enforce it.

## 4. Later / heavier `[ ]`

- [ ] VSIX extension (File → New Project UI) after NuGet distribution
  proves demand.
- [ ] FlaUI smoke test in CI (launch → navigate → screenshot). Valuable but
  flaky; after the template matrix.

## Conventions for all work

- `dotnet build -c Debug -p:Platform=x64` must show 0 warnings, 0 errors.
- Template changes: re-verify the scaffold matrix, never just the default.
- Commit locally with conventional messages; push/release only when asked.
- Windows 11 WinUI 3 look, native methods, no breaking changes without need.
