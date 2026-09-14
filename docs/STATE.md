# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/DISCOVERY-PLAN.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** verify v0.0.3-beta + templates-v0.1.6 CI runs, then continue discovery-plan work.
- **Version:** app `0.0.3-beta`; template package `0.1.6`.
- **Tags:** `v0.0.3-beta` and `templates-v0.1.6` pushed (CI triggered); `beta` branch moved to `v0.0.3-beta`.
- **Branches:** `main` @ release commit `8b2eca9`; `beta` tracks `v0.0.3-beta`; `stable` tracks the older stable release.
- **Tree:** clean — release commit pushed; diagnostics plan complete (Phases 0–4).
- **CI health (last known):** local build 0 warnings/0 errors, unit tests 181/181, mirror parity OK (121 files), scaffold matrix passed (9 combos), smoke 4 passed + 1 skipped, pack dry-run valid. GitHub Release (Velopack, beta channel) + NuGet publish (DevTem.Templates 0.1.6) running in Actions — confirm both green there.
- **Next:** confirm the GitHub release + NuGet package published, then discovery-plan work.
- **Open questions:** none blocking.
