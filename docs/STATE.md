# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** v0.6.0-beta SHIPPED (`v0.6.0-beta` + `templates-v0.6.0` tagged 2026-10-10, `beta` moved; work order `docs/V0.6.0-ADVANCED-PLAN.md` closed — joint meet, matched from now on). CI owns the releases (`release.yml` beta channel; `templates-publish` → NuGet 0.6.0). Open manuals: E5 VS dialog shots + `vs-dialog.png`/`scaffold-help.png` (VS 2026 18.10 on box, no scripted capture); kit proofs stay pending-kit. Next: v0.7.0 planning from `docs/ROADMAP.md` §4 in a new work-order file. Note: remote URL is SSH (`git@github.com:…`) — HTTPS egress is blocked from this machine, port 22 works (flaky, retry loops; this time first-try).
- **Version:** app `0.6.0-beta` (commit `87b2269`, tag `v0.6.0-beta` pushed — CI release.yml building the beta-channel GitHub release; `beta` branch moved to `v0.6.0-beta`). Template package `0.6.0` (tag `templates-v0.6.0` pushed — CI templates-publish pushing to NuGet; joint meet per DECISIONS; nupkg audit passed locally: 6 templates resolve, `--slnx` in help, slnx on/off probes build 0/0, uninstalled after).
- **Tags (pushed):** `v0.0.28-beta` (release.yml -> beta channel, GitHub release published with phase notes + assets); `templates-v0.4.0` (templates-publish -> NuGet 0.4.0 live, GitHub release with template notes); `beta` branch moved to `v0.0.28-beta`.
- **CI health (release):** Release workflow success, templates-publish success, tag-triggered full matrix success (17m40s) on the release commit.
- **Tags (pushed):** `v0.0.27-beta` (release.yml -> beta channel, CI building); `templates-v0.3.12` (templates-publish -> NuGet, CI publishing); `beta` branch moved to `v0.0.27-beta`.
- **Branches:** `main` (to push); `beta` tracks v0.0.26-beta until release moves it.
- **CI health (this session, local, v0.6.0):** build 0/0, tests 380+1 (372 + 8 new TemplateSurfaceTests), parity OK (240),
  FULL 22-combo matrix PASSED (21 + slnx; scratch + 3 Validate negatives green),
  smoke 9+1 on retry (first run: 2 transient fails on first-launch UX — same known flake class),
  presets 3/3 scaffold + build (`full` preset fixed: was invalid velopack+msix),
  `build-msix -Validate -Publisher CN=Store-Test` PASSED + Velopack-increasing (`0.6.0.0` > `0.5.0.0`),
  nupkg 0.6.0 audit (6 resolve, slnx probes build 0/0, uninstalled, `dotnet new update` clean; pre-existing path install untouched),
  weight 273.8 MB carries over (no publish delta — ceilings untouched by design).
  WACK/Store-kit still pending-kit; E5 VS dialog shots pending-manual.
- **Cold flame (v0.6.0, Debug dev box 2026-10-10, applog-measured, warm machine):**
  splash 303/312/292 / window 508/518/501 post-first-run (3 timed launches + 1 quiet repeat, zero budget breaches; first launch 1130 on first-run dialogs, same class every version).
  Consistent with the v0.5.0 warm floor (~300/~500); v0.6.0 changes zero hot-path code.
- **CI health (this session, local, v0.4.0):** build 0/0, tests 372+1, parity OK (238),
  FULL 21-combo matrix PASSED (allon `--auth true` + 20 auth-off combos; FEATURES auth row + guide asserts),
  smoke 9+1 on retry (first run: 1 transient failure under load, name not retained — same known flake class as v0.2.0/v0.3.0),
  `build-msix -Validate` stops at placeholder-publisher guard (by design), nupkg 0.4.4 audit (6 resolve, `--auth` in help,
  auth-on 361+1 / auth-off 359+1 green, uninstalled after). Screenshots captured via throwaway VSTest rig (deleted after):
  `auth.png` (Settings ACCOUNT section, needs-client-id state), `notification.png` (same + live in-app card),
  EN dark maximized, mirrored to the template side. Weight 273.8 MB (+2.1% vs 268.2 MB baseline; MSAL + Broker + DPAPI).
- **Cold flame (v0.4.0, Debug dev box 2026-10-08, applog-measured, warm machine):**
  splash 305/293/293 / window 508/503/513 (3 launches, zero budget breaches).
  Consistent with the v0.3.0 warm floor (~318/~548); auth init rides deferred past the first frame.
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
