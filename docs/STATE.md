# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** all green. `v0.0.7-beta` shipped (CI fixes); templates matrix on the release commit running.
- **Version:** app `0.0.7-beta`; template package `0.3.1`.
- **Tags:** `v0.0.7-beta` pushed (release.yml success, 5 assets live); `beta` tracks `v0.0.7-beta` (verified).
- **Branches:** `main` @ `4d0397c` (pushed); `beta` tracks `v0.0.7-beta`.
- **Tree:** clean.
- **CI health (last known):** Release success, msix SUCCESS, ui-tests SUCCESS, prior templates matrix success (16m). Packaged proof + WACK = manual checklist (kit machine).
- **Next:** set `DEVTEM_MSIX_PUBLISHER` secret to light the MSIX leg; packaged proof on a kit machine. Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
