# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** all green. `v0.0.11-beta` + `templates-v0.3.5` shipped; release pipeline fixed to parallel uploads.
- **Version:** app `0.0.11-beta`; template package `0.3.5`.
- **Tags:** `v0.0.11-beta` (release.yml success on re-run, assets live); `templates-v0.3.5` (templates-publish success → NuGet).
- **Branches:** `main` pushed; `beta` tracks `v0.0.11-beta` (verified).
- **Tree:** clean (pending this STATE refresh).
- **CI health (last known):** Release success (dispatch re-run, 11m12s), templates-publish success, templates matrix running, msix/ui-tests pending on latest main. Local: build 0/0, tests 267+1, parity OK (156).
- **Release pipeline:** `vpk` packs, `Scripts/Invoke-GithubParallelUpload.ps1` uploads (`gh`, one job per asset, 3x retry, markers). Serial `vpk upload github` topped ~1h at ~2-3 Mbps and tripped timeouts; parallel lands ~11 min. See DECISIONS + AGENTS gotcha 5.
- **Next:** packaged proof on a kit machine (WACK still manual).

(End of file - total 14 lines)
