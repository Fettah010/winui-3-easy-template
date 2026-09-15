# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** dual-track shipped. Commit `6cf0cf8` pushed; `v0.0.6-beta` + `beta` + `templates-v0.3.1` live.
- **Version:** app `0.0.6-beta`; template package `0.3.1`.
- **Tags:** `v0.0.6-beta` pushed (release.yml CI: Velopack release; MSIX leg skips — secret unset); `templates-v0.3.1` pushed (templates-publish.yml → NuGet).
- **Branches:** `main` @ `6cf0cf8` (pushed); `beta` tracks `v0.0.6-beta` (force-pushed, verified).
- **Tree:** clean.
- **CI health (last known):** local build 0/0, tests 215+4 skipped, parity OK (146 files), FULL matrix PASSED (21 combos) + init-template scratch, Pack DryRun staging valid. Packaged proof + WACK = manual checklist (kit machine).
- **Next:** set `DEVTEM_MSIX_PUBLISHER` secret to light the MSIX leg; packaged proof on a kit machine. Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
