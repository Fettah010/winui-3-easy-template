# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** all green. `v0.0.10-beta` + `templates-v0.3.4` shipped; every CI leg success (incl. a smoke-fix follow-up).
- **Version:** app `0.0.10-beta`; template package `0.3.4`.
- **Tags:** `v0.0.10-beta` (release.yml success, assets live); `templates-v0.3.4` (templates-publish success → NuGet 0.3.4 indexed).
- **Branches:** `main` pushed (incl. `8d880ab` smoke Invoke fix); `beta` tracks `v0.0.10-beta` (verified).
- **Tree:** clean (pending this STATE refresh).
- **CI health (last known):** Release success, templates-publish success, templates matrix success (17m), msix SUCCESS, ui-tests SUCCESS (after Invoke fix). Local: build 0/0, tests 267+1, parity OK (155), smoke 6+1, full 21-combo matrix green.
- **Next:** packaged proof on a kit machine (WACK still manual).

(End of file - total 14 lines)
