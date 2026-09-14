# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/DISCOVERY-PLAN.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** continue template discoverability and SEO improvements tracked in `docs/DISCOVERY-PLAN.md`.
- **Version:** app `0.0.3-beta`; template package `0.1.6`.
- **Tags:** `v0.0.3-beta` and `templates-v0.1.6` shipped and CI-green.
- **Branches:** `main` @ post-release state; `beta` tracks `v0.0.3-beta`.
- **Tree:** clean — v0.0.3-beta released (GitHub) + 0.1.6 published (NuGet).
- **CI health (last known):** local build 0 warnings/0 errors, unit tests 181/181, mirror parity OK (121 files), scaffold matrix passed (9 combos), smoke 4 passed + 1 skipped, pack dry-run valid. GitHub Release `v0.0.3-beta` published (Velopack beta channel); NuGet `DevTem.Templates` 0.1.6 live (indexing pending). `gh` authenticated on this machine — future sessions can watch CI directly.
- **Next:** discovery-plan work. Follow-up noted: packaged template README feature table doesn't mention the diagnostics page / `--health` flag yet.
- **Open questions:** none blocking.
