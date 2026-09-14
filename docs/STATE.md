# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/DISCOVERY-PLAN.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** continue template discoverability and SEO improvements tracked in `docs/DISCOVERY-PLAN.md`.
- **Version:** app `0.0.2-beta`; template package `0.1.5`.
- **Tags:** `v0.0.2-beta` and `templates-v0.1.5` shipped.
- **Branches:** `main` contains the published template-reusability release plus the restored discovery work, the diagnostics fix, the desktop-shortcut work, the diagnostics plan doc, and Phases 0–4 (uncommitted).
- **Tree:** diagnostics plan complete (Phases 0–4 done + mirrored): pipeline, page UX, metrics/traces/crash context, `health` template flag + `docs/diagnostics.md` + item-template examples. All tiers green.
- **CI health (last known):** local build 0 warnings/0 errors, unit tests 181/181, mirror parity OK (121 files), scaffold matrix passed (9 combos incl. nodiag); release workflows for v0.0.2-beta and templates-v0.1.5 passed.
- **Next:** discovery-plan work; release whenever ready (no version bump/tags pushed).
- **Open questions:** none blocking.
