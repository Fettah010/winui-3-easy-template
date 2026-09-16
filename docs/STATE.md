# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
> the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
> the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
> this file holds only **current facts**.

- **Goal:** performance plan implemented. P0-P3 landed in the working
  tree, all tiers runnable green except full-matrix flake watch.
- **Version:** app `0.0.8-beta`; template package `0.3.2`.
- **Tags:** `v0.0.8-beta` (release.yml success, assets live); `templates-v0.3.2` (templates-publish success → NuGet).
- **Branches:** `main` @ `7916e03` + uncommitted perf work (held per rule).
- **Tree:** dirty (perf plan: ~60 modified + ~18 new app/template files).
  Nothing committed, pushed, tagged, or released.
- **CI health (this session):** local build 0/0; `dotnet test Tests/`
  267 passed + 4 skipped (pre-existing skips); parity OK (159);
  FlaUI smoke 5 passed + 1 skipped (59 s); scaffold matrix 20/21 per
  full run (single-test load flakes in rotating combos, all green solo;
  subset-7 + rebrand green); publish weight 268.2 MB baseline recorded
  (`docs/publish-weight-baseline.json`).
- **Next:** commit on request (review the 103-path diff first); then the
  release runbook in `docs/WORKFLOW.md`. Open: `DISCOVERY-PLAN.md`
  A3/A4+B/C/D, roadmap triage (85 boxes), full-matrix re-run on a quiet
  machine if a clean 21/21 is wanted for the record.

(End of file - total 14 lines)
