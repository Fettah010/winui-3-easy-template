# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** distribution plan P0–P4 shipped. Commit `39dd1cd` pushed; `v0.0.5-beta` + `beta` live.
- **Version:** app `0.0.5-beta`; template package `0.3.0`.
- **Tags:** `v0.0.5-beta` pushed (release.yml CI builds GitHub Release); `templates-v0.3.0` pushed (templates-publish.yml packs + pushes NuGet).
- **Branches:** `main` @ `39dd1cd` (pushed); `beta` tracks `v0.0.5-beta` (force-pushed, verified).
- **Tree:** P4 uncommitted — FEATURES manifest (setup files, UpdateCenter-always rule), profiles + Store preset (TEMPLATE-GUIDE, both READMEs, NuGet README), AGENTS layout rows + symbols, WORKFLOW tiers/runbook, versions (app 0.0.5-beta, template 0.3.0, CITATION, CHANGELOG dated, what's-new text).
- **CI health (last known):** local build 0/0, tests 212+4 skipped, parity OK (141 files), FULL matrix PASSED (21 combos) + init-template scratch, Pack DryRun staging valid. Packaged-runtime + feed round-trip + WACK = manual checklists (no makeappx/WACK locally). Matrix fits the 30min CI timeout — no job split.
- **Next:** commit P0-P4 on request → tag `v0.0.5-beta` + `templates-v0.3.0` (human runs). Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
