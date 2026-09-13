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

## 4. Later / heavier `[~]`

- [x] Visual Studio distribution — DECISION, no separate VSIX: VS's New
  Project dialog runs the same template engine, so the `DevTem.Templates`
  NuGet package IS the VS channel (bool params render as checkboxes, text
  params as fields). Shipped: `icon.png` in both `.template.config` dirs,
  `displayName` on all 7 symbols, `tags {language,type}` on the project
  template. Known limit: item templates never appear in Add → New Item
  (VS-only surface) — `devtem-page` stays CLI. Verified: `[C#]` tag,
  help output, all-on scaffold 93/93, icons inside the nupkg.
- [ ] FlaUI smoke test in CI — spec frozen in `docs/FLAUI-PLAN.md`
  (tiny suite: launch, navigate pages, theme + language switch; needs
  AutomationIds on nav items first).

## Conventions for all work

- `dotnet build -c Debug -p:Platform=x64` must show 0 warnings, 0 errors.
- Template changes: re-verify the scaffold matrix, never just the default.
- Commit locally with conventional messages; push/release only when asked.
- Windows 11 WinUI 3 look, native methods, no breaking changes without need.
