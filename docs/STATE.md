# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** v0.3.0-beta IMPLEMENTED uncommitted (pages + data; work order `docs/V0.3.0-ADVANCED-PLAN.md` done, WB-09 handover pending). v0.2.0-beta SHIPPED before it. Released 0.0.28-beta (app, Phases A–F) + 0.4.0 (templates) before that.
- **Version:** app `0.3.0-beta` (uncommitted: csproj quartet + CITATION + filled CHANGELOG `0.3.0-beta`; Velopack-increasing over `0.2.0`). Template package `0.4.3` planned (tag `templates-v0.4.3` NOT pushed yet; convergence PATCH toward joint `0.5.0` per DECISIONS; nupkg audit passed locally: 6 templates resolve, package scaffold builds 0/0).
- **Tags (pushed):** `v0.0.28-beta` (release.yml -> beta channel, GitHub release published with phase notes + assets); `templates-v0.4.0` (templates-publish -> NuGet 0.4.0 live, GitHub release with template notes); `beta` branch moved to `v0.0.28-beta`.
- **CI health (release):** Release workflow success, templates-publish success, tag-triggered full matrix success (17m40s) on the release commit.
- **Tags (pushed):** `v0.0.27-beta` (release.yml -> beta channel, CI building); `templates-v0.3.12` (templates-publish -> NuGet, CI publishing); `beta` branch moved to `v0.0.27-beta`.
- **Branches:** `main` (to push); `beta` tracks v0.0.26-beta until release moves it.
- **CI health (this session, local, v0.3.0):** build 0/0, tests 357+1, parity OK (228),
  FULL 21-combo matrix PASSED (allon incl. all 6 page kinds + duplicate-route rejection;
  init-template scratch), smoke 9+1 on retry (first run: 2 nav failures under the post-bump
  whats-new modal — same known flake as v0.2.0, both-attempts evidence kept), `build-msix
  -Validate` stops at placeholder-publisher guard (by design), nupkg 0.4.3 audit (6 resolve,
  scaffold builds 0/0, uninstalled after). Screenshots captured via scratch scaffold +
  throwaway VSTest rig (deleted after): `list-details.png`, `datagrid.png`, `contentgrid.png`
  (EN dark, pane expanded, protocol cold-start landing), mirrored to the template side.
  OBSERVATION: datagrid shot shows an empty-looking black header row in dark theme —
  pre-existing F1 code, filed as contrast follow-up for v0.7.0/v0.9.0, not fixed here.
- **Cold flame (v0.3.0, Debug dev box 2026-10-06, applog-measured, warm machine):**
  splash 318/319/308 / window 548/536/524 (3 launches, zero budget breaches).
  Consistent with the v0.2.0 warm floor (~300/~495); v0.3.0 changes zero hot-path
  code (templates + service + guides only). Weight 267.9 MB (-0.1% vs 268.2 MB baseline).
- **Cold flame (v0.2.0, Debug dev box 2026-10-05, applog-measured, warm machine):**
  splash ~300 / window ~495ms (3 launches: 314/509, 292/491, 299/492).
  Budgets hold (splash 1600, window 2500 — zero breaches). No cold-of-day this
  session (machine ran the app all day); smoke-run first launch 802/1145 still
  inside budgets. Supersedes nothing — consistent with the v0.1.0 warm floor
  (~280/~475); v0.2.0 changes zero hot-path code (XAML-only + templates).
- **Cold flame (v0.1.0, Debug dev box 2026-10-04, applog-measured):** cold (first of
  day) splash 839 / window 1196ms; warm splash ~280 / window ~475ms (2 runs).
  Budgets hold (splash 1600, window 2500 — zero breaches). Services phase is not
  log-surfaced; P0-4 holds trivially (v0.1.0 changes zero product code). Supersedes
  the 2026-09-23 flame (1451/1715/1964 cold).
- **Toast post-mortem:** the card Border had `Opacity="0"` while motion targets the card — every toast invisible since introduction (found via installed-app log forensics: checks ran, toasts dropped silently). Fixed + smoke test. "Not installed" lines in the shared log were dev-binary runs; identity line now disambiguates.
- **CI health (last known):** Local: build 0/0, tests 269+1, parity OK (158), MSIX `-Validate` pass, template matrix green (alloff/noupd/updbasic/msixnone/msixapp/allon + init-template), smoke launch + update-check pass live in isolation. Release asset diet: Portable.zip no longer uploaded (~340MB/28min → ~230MB/~18min expected).
- **Check-now fix (v0.0.14):** per-service deferred-init guards (one throw can't half-wire the app), busy check button (disabled + "Checking…" until the flow resolves), AppLog breadcrumbs on every update stage + dropped-toast/host warnings. If a check is ever silent again, `Logs/applog-*.log` names the cause.
- **Tree (v0.2.0, uncommitted):** version-bump M (csproj pair + CITATION + filled
  CHANGELOG `0.2.0-beta`), new `Templates/TabView/` + dormant
  `Templates/Project/Templates/TabView/` (parity nested +TabView), `add-page -Kind
  tab` + matrix tab smoke (both script mirrors byte-identical), packaging Content
  Include, Settings beauty (1024px + BodyStrong, mirrored), TEMPLATE-GUIDE tab +
  Shells + menubar recipe (mirrored), parity exclusion +1 (both parity mirrors),
  new `docs/V0.2.0-ADVANCED-PLAN.md` (repo-only) + DECISIONS + this STATE refresh +
  README `What's new in 0.2.0-beta` + Shells table. Nothing committed/pushed/tagged
  (AGENTS.md rule). Weight 267.8 MB (-0.1%).
- **CI health (this session, local, v0.2.0):** build 0/0, tests 350+1 (twice),
  parity OK (217), FULL 21-combo matrix PASSED (tab page added and green in
  renamed allon scaffold; duplicate-route rejection intact), smoke 9+1 on retry
  (first run: 2 nav failures under the post-bump whats-new modal — stale 0.0.10
  bullets in the dialog, fixed by filling the CHANGELOG entry; screenshot
  evidence kept), `build-msix -Validate` reads 0.2.0.0 then stops at
  placeholder-publisher guard (by design), nupkg 0.4.2 audit (5 resolve, scaffold
  builds 0/0, uninstalled after). Screenshots captured post-release via
  in-box UI Automation (commit `a5e55b3`): `shell-tabs.png` (Workspace TabView
  live in the app) + `settings-cards.png` (rail closed) + `settings.png`
  re-captured at the 1024px beauty pass (pane expanded), EN dark 1904px,
  mirrored to the template side; README Shells table carries the tab shot.
- **Tree:** version-bump M (csproj pair + CITATION + CHANGELOG `0.1.0-beta`), ROADMAP mirrored to `Templates/Project/docs/`, parity exclusion +1 line in both `test-mirror-parity.ps1` copies, new `docs/V0.1.0-ADVANCED-PLAN.md` (repo-only) + this STATE refresh; staged deletions (ADVANCEMENT pair + discovery drafts) still uncommitted. Nothing committed/pushed/tagged (AGENTS.md rule).
- **CI health (this session, local):** build 0/0, tests 350+1, parity OK (210),
  Matrix-Fast PASSED, smoke 9+1 full-green on re-run (first run: 1 nav failure from
  Start-menu occlusion + whats-new modal — environmental, both-attempts evidence kept;
  see plan log), `build-msix -Validate` reads 0.1.0.0 then stops at
  placeholder-publisher guard (by design). Screenshots re-captured at 0.1.0
  (home/settings/toast + new about.png, 1920px, EN, fresh defaults, pane expanded);
  S1/S2 (200% + HC) stay manual (OS-level display changes, unsafe to automate here).
- **CI health (last known):** Local: build 0/0, tests 268+1, parity OK (157), MSIX `-Validate` + `-DryRun` (+AppInstaller feed) pass, template matrix green (21 combos incl. init-template scratch), smoke launch + update-check pass live in isolation (full suite flaky on shared desktop — known fragile point).
- **Release pipeline:** `vpk` packs, `Scripts/Invoke-GithubParallelUpload.ps1` uploads (`gh`, one job per asset, 3x retry, markers). Serial `vpk upload github` topped ~1h at ~2-3 Mbps and tripped timeouts; parallel lands ~11 min. See DECISIONS + AGENTS gotcha 5.
- **Update UX (v0.0.12):** `UpdateDialogService` = toasts (checking/up-to-date/not-installed/error-via-dialog) + native `ContentDialog` (available/download-progress/ready); 30s check timeout + unpackaged short-circuit (no infinite checking); legacy `UpdatePopup` overlay kept wired but hidden. First-run wizard never auto-opens (installer owns setup); `ShouldShowSetupWizard()` retired to false; parity guard manifest dropped to `@()` for that file.
- **Next:** packaged proof on a kit machine (WACK still manual).
- **Nav fix, round 5 — overlay, RELEASING as 0.0.26-beta:** frame captures proved the inline
  push desyncs (page laid out half-growth off mid-slide, snap + Mica gap after).
  `MainWindow` now `LeftCompact` + closed rail: toggles resize nothing, content
  stays centered AND static. Dead compact-pane API deleted; nav is a documented
  extension surface (`<devtem:nav-items>`); pane-toggle smoke test rewritten for
  rail-first startup and PASSES live. Open note: drawer-over-Mica background left
  at default (NavigationViewExpandedPaneBackground is the knob if it reads wrong).

(End of file - total 14 lines)
