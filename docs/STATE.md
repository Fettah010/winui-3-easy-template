# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/DISCOVERY-PLAN.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** diagnostics page fix + usability (logs not showing) with template mirror.
- **Version:** app `0.0.2-beta`; template package `0.1.5`.
- **Tags:** `v0.0.2-beta` and `templates-v0.1.5` shipped.
- **Branches:** `main` contains the published template-reusability release plus the restored discovery work and the diagnostics fix (unpushed until verified).
- **Tree:** diagnostics fix + usability improvements + tests, mirrored into `Templates/Project/`, all tiers green.
- **CI health (last known):** local build 0 warnings/0 errors, unit tests 138/138, mirror parity OK (106 files), scaffold matrix passed, smoke 4 passed + 1 skipped (generated-page inconclusive); release workflows for v0.0.2-beta and templates-v0.1.5 passed.
- **Next:** continue the checked discovery-plan phase, then verify and release the next version.
- **Open questions:** none blocking.
