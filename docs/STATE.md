# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/RESTRUCTURE-PLAN.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only *current facts*.

- **Goal:** codebase restructure per `docs/RESTRUCTURE-PLAN.md` (Phase 0–7
  done). Release v0.0.5-beta + DevTem.Templates 0.1.3.
- **Version:** csproj `0.0.5` / `InformationalVersion 0.0.5-beta` (app + template).
- **Tags:** `v0.0.4-beta` (shipped), `templates-v0.1.2` (shipped).
- **Branches:** `main` = latest; `beta` → `v0.0.4-beta`; `stable` → `v0.0.2`.
- **Tree:** release prep done, uncommitted (Phase 7 + 0.0.5 bump + CHANGELOG).
- **CI health (last known):** unit 131/131, matrix PASSED, parity OK (103),
  MSIX DryRun valid, FlaUI 4/4, guard passes for 0.0.5-beta.
- **Next:** commit → push main → tag `v0.0.5-beta` → move `beta` →
  tag `templates-v0.1.3` (NuGet via CI).
- **Open questions:** none blocking.
