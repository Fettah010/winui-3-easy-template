# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** all green. Fix `346b1f3` pushed (msix zip-wrap + smoke wizard); CI proves both.
- **Version:** app `0.0.6-beta`; template package `0.3.1`.
- **Tags:** `v0.0.6-beta` (release.yml success: Velopack assets + notes live); `templates-v0.3.1` (NuGet push confirmed in publish log).
- **Branches:** `main` @ `346b1f3` (pushed); `beta` tracks `v0.0.6-beta` @ `6cf0cf8` (fix rides the next release, tags unmoved).
- **Tree:** clean.
- **CI health (last known):** Release success, templates-publish success, msix SUCCESS (fix proven), ui-tests SUCCESS (fix proven), templates matrix running. Packaged proof + WACK = manual checklist (kit machine).
- **Next:** set `DEVTEM_MSIX_PUBLISHER` secret to light the MSIX leg; packaged proof on a kit machine. Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
