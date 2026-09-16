# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** all green. `v0.0.9-beta` + `templates-v0.3.3` shipped; every CI leg success.
- **Version:** app `0.0.9-beta`; template package `0.3.3`.
- **Tags:** `v0.0.9-beta` (release.yml success, assets live); `templates-v0.3.3` (templates-publish success → NuGet 0.3.3 indexed).
- **Branches:** `main` pushed; `beta` tracks `v0.0.9-beta` (verified).
- **Tree:** clean (pending this STATE refresh).
- **CI health (last known):** Release success, templates-publish success, templates matrix success, msix SUCCESS, ui-tests SUCCESS. Local: build 0/0, tests 267+4, parity OK (159), smoke 5+1, publish baseline 268.2 MB.
- **Next:** packaged proof on a kit machine (WACK still manual). Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes).

(End of file - total 14 lines)
