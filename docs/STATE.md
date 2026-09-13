# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/RELEASE-PLAN.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only *current facts*.

- **Goal:** release v0.0.4-beta on GitHub (pipeline release, no app
  behavior changes).
- **Version:** csproj `0.0.4` / `InformationalVersion 0.0.4-beta` (app + template).
- **Tags:** `v0.0.3-beta` (shipped), `templates-v0.1.1` (last package).
- **Branches:** `main` = latest; `beta` → `v0.0.3-beta`; `stable` → `v0.0.2`.
- **Tree:** v0.0.4-beta prep done, uncommitted (bump everywhere,
  CHANGELOG section, honest what's-new, plan/state updates).
- **CI health (last known):** unit 116/116, matrix PASSED, MSIX DryRun
  valid, FlaUI 4/4, parity OK (81 files), guard passes for 0.0.4-beta.
- **Next:** commit → push main → tag `v0.0.4-beta` → move `beta` →
  verify Release CI → then `templates-v0.1.2` still pending.
- **Open questions:** none blocking.
