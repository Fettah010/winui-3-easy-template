# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/FLEXIBILITY-PLAN.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** flexibility plan complete — commit Phases 0–4 (on request), then release whenever ready.
- **Version:** app `0.0.3-beta`; template package `0.2.0` (bumped, unpublished).
- **Tags:** `v0.0.3-beta` and `templates-v0.1.6` shipped and CI-green.
- **Branches:** `main` with uncommitted Phase 0–4 work (see tree); `beta` tracks `v0.0.3-beta`.
- **Tree:** ALL PHASES DONE, uncommitted — composable template (updates/logging choices, crash/i18n/tests flags), polished docs, `DevTem.Templates` 0.2.0 packed OK.
- **CI health (last known):** local build 0 warnings/0 errors, unit tests 196/196, mirror parity OK (129 files), full scaffold matrix PASSED (15 combos + init-template scratch), `dotnet new -h` renders choices cleanly, 0.2.0 nupkg packs.
- **Next:** commit (on request) → release as `templates-v0.2.0` (+ app beta whenever behavior warrants). Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
