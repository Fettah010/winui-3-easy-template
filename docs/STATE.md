# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** all green. `v0.0.8-beta` + `templates-v0.3.2` shipped; every CI leg success.
- **Version:** app `0.0.8-beta`; template package `0.3.2`.
- **Tags:** `v0.0.8-beta` (release.yml success, assets live); `templates-v0.3.2` (templates-publish success → NuGet).
- **Branches:** `main` pushed; `beta` tracks `v0.0.8-beta` (verified).
- **Tree:** clean (pending this STATE refresh).
- **CI health (last known):** Release success, templates-publish success, templates matrix success (18m48s), msix SUCCESS, ui-tests SUCCESS. Local: build 0/0, tests 220+4, parity OK (146), smoke 5/6. Packaged proof + WACK = manual checklist (kit machine).
- **Next:** set `DEVTEM_MSIX_PUBLISHER` secret to light the MSIX leg; packaged proof on a kit machine. Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
