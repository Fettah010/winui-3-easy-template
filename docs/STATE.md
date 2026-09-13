# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/RELEASE-PLAN.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only *current facts*.

- **Goal:** ship v0.0.3-beta; then DevTem.Templates 0.1.2; then P1 template-correctness items.
- **Version:** csproj `0.0.3` / `InformationalVersion 0.0.3-beta` (app + template).
- **Tags:** `v0.0.3-beta` (pushed, CI Release triggered — status must be
  checked in Actions, `gh` is not authenticated here), `templates-v0.1.1`
  (last template package).
- **Branches:** `main` = latest; `beta` → `v0.0.3-beta`; `stable` → `v0.0.2`.
- **Tree:** clean (everything below committed).
- **CI health (last known):** unit 116/116, matrix PASSED (identity
  asserts + new sln), MSIX DryRun valid, FlaUI 4/4 (incl. exact CI
  sequence), parity OK (81 files). v0.0.3-beta Release green;
  ui-tests sln-mapping fix verified locally, pending CI proof on push.
- **Next:** push → watch ui-tests + templates jobs → tag
  `templates-v0.1.2` → P3 debt.
- **Open questions:** none blocking.
- **Open questions:** none blocking. (Beta default channel is intended:
  fresh 0.0.3-beta installs track beta.)
- **Last session (2026-09-13):** P0 release prep done (bump, CHANGELOG,
  release guard+notes, what's-new dialog fix); v0.0.3-beta pushed.
