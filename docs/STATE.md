# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** Advancement Plan Phase B (UX/a11y/UI consistency) — B1 one responsive mechanism (VSM deleted), B2 keyboard accelerators + reservation list, B3 live regions, B4 compiled Diagnostics bindings, B5 static scaling audit + guards (screenshot matrix manual), B6 devtem-list-details + add-page -Kind, B7 dialog-button rule + test, B8 capped whats-new + About recall + once-per-version, B9 state audit (sticky Home card, download retry, wizard validation).
- **Version:** app `0.0.27-beta` (unchanged — no release this pass); template package `0.3.12` (unchanged).
- **Tags (pushed):** `v0.0.27-beta` (release.yml -> beta channel, CI building); `templates-v0.3.12` (templates-publish -> NuGet, CI publishing); `beta` branch moved to `v0.0.27-beta`.
- **Branches:** `main` (to push); `beta` tracks v0.0.26-beta until release moves it.
- **CI health (this session, local):** build 0/0, tests 316+1, parity OK (189), matrix allon PASSED (incl. new -Kind list smoke, duplicate-route, msix DryRun, init-template scratch). B5/B9 per-state screenshots stay manual (Live-UI desktop needed). Full 21-combo matrix not re-run — run full before tagging.
- **Toast post-mortem:** the card Border had `Opacity="0"` while motion targets the card — every toast invisible since introduction (found via installed-app log forensics: checks ran, toasts dropped silently). Fixed + smoke test. "Not installed" lines in the shared log were dev-binary runs; identity line now disambiguates.
- **CI health (last known):** Local: build 0/0, tests 269+1, parity OK (158), MSIX `-Validate` pass, template matrix green (alloff/noupd/updbasic/msixnone/msixapp/allon + init-template), smoke launch + update-check pass live in isolation. Release asset diet: Portable.zip no longer uploaded (~340MB/28min → ~230MB/~18min expected).
- **Check-now fix (v0.0.14):** per-service deferred-init guards (one throw can't half-wire the app), busy check button (disabled + "Checking…" until the flow resolves), AppLog breadcrumbs on every update stage + dropped-toast/host warnings. If a check is ever silent again, `Logs/applog-*.log` names the cause.
- **Tree:** modified `README.md` only (uncommitted); local `wiki/` staging removed (lives in wiki.git).
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
