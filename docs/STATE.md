# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** all green. `v0.0.20-beta` (invisible-toast root cause + identity log, dark setup) then `v0.0.21-beta` (update-test target) + `templates-v0.3.8`, back-to-back.
- **Version:** app `0.0.21-beta`; template package `0.3.8`.
- **Tags:** `v0.0.20-beta`, `v0.0.21-beta` (release.yml → beta channel); `templates-v0.3.8` (templates-publish → NuGet).
- **Branches:** `main` pushed; `beta` tracks latest (to verify).
- **Toast post-mortem:** the card Border had `Opacity="0"` while motion targets the card — every toast invisible since introduction (found via installed-app log forensics: checks ran, toasts dropped silently). Fixed + smoke test. "Not installed" lines in the shared log were dev-binary runs; identity line now disambiguates.
- **CI health (last known):** Local: build 0/0, tests 269+1, parity OK (158), MSIX `-Validate` pass, template matrix green (alloff/noupd/updbasic/msixnone/msixapp/allon + init-template), smoke launch + update-check pass live in isolation. Release asset diet: Portable.zip no longer uploaded (~340MB/28min → ~230MB/~18min expected).
- **Check-now fix (v0.0.14):** per-service deferred-init guards (one throw can't half-wire the app), busy check button (disabled + "Checking…" until the flow resolves), AppLog breadcrumbs on every update stage + dropped-toast/host warnings. If a check is ever silent again, `Logs/applog-*.log` names the cause.
- **Tree:** clean (pending this STATE refresh).
- **CI health (last known):** Local: build 0/0, tests 268+1, parity OK (157), MSIX `-Validate` + `-DryRun` (+AppInstaller feed) pass, template matrix green (21 combos incl. init-template scratch), smoke launch + update-check pass live in isolation (full suite flaky on shared desktop — known fragile point).
- **Release pipeline:** `vpk` packs, `Scripts/Invoke-GithubParallelUpload.ps1` uploads (`gh`, one job per asset, 3x retry, markers). Serial `vpk upload github` topped ~1h at ~2-3 Mbps and tripped timeouts; parallel lands ~11 min. See DECISIONS + AGENTS gotcha 5.
- **Update UX (v0.0.12):** `UpdateDialogService` = toasts (checking/up-to-date/not-installed/error-via-dialog) + native `ContentDialog` (available/download-progress/ready); 30s check timeout + unpackaged short-circuit (no infinite checking); legacy `UpdatePopup` overlay kept wired but hidden. First-run wizard never auto-opens (installer owns setup); `ShouldShowSetupWizard()` retired to false; parity guard manifest dropped to `@()` for that file.
- **Next:** packaged proof on a kit machine (WACK still manual).

(End of file - total 14 lines)
