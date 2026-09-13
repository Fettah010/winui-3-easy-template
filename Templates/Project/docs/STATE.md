# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/RESTRUCTURE-PLAN.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only *current facts*.

- **Goal:** codebase restructure per `docs/RESTRUCTURE-PLAN.md` (Phase 0–7
  done and released).
- **Version:** csproj `0.0.5` / `InformationalVersion 0.0.5-beta` (app + template).
- **Tags:** `v0.0.5-beta` (pushed, CI Release running — verify in Actions),
  `templates-v0.1.3` (pushed, CI publish to NuGet running).
- **Branches:** `main` = feb76ac; `beta` → `v0.0.5-beta`; `stable` → `v0.0.2`.
- **Tree:** clean, everything pushed.
- **CI health (last known):** unit 131/131, matrix PASSED, parity OK (103),
  MSIX DryRun valid, FlaUI 4/4, guard passes for 0.0.5-beta.
- **Next:** verify Release + templates-publish CI runs → P3/carried debt
  (Dependabot github-actions, smoke-harness decision) or next feature.
- **Open questions:** none blocking.
