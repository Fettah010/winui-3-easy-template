# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** dual-track plan D0–D3 implemented uncommitted — commit + release on request.
- **Version:** app `0.0.6-beta`; template package `0.3.1`.
- **Tags:** `v0.0.5-beta` and `templates-v0.3.0` shipped and CI-green (next: `v0.0.6-beta`, `templates-v0.3.1` — NOT tagged).
- **Branches:** `main` @ `3ebf85e` (+ uncommitted dual-track); `beta` tracks `v0.0.5-beta`.
- **Tree:** uncommitted — `IsExternallyManaged` seam + 6 VM swaps + packaged-dual string ×3, `release.yml` MSIX leg (secret-gated), `distribution-dual.md`, matrix allon DryRun, Dual preset rows, versions (app 0.0.6-beta via bump-version dogfood, templates 0.3.1, CHANGELOG, what's-new).
- **CI health (last known):** local build 0/0, tests 215+4 skipped, parity OK (146 files), matrix subset PASSED (msixstore, allon+DryRun, nosetup) + init-template scratch, Pack DryRun staging valid, YAML parses. Packaged proof + WACK = manual checklist (kit machine).
- **Next:** commit on request → tag `v0.0.6-beta` + `templates-v0.3.1` (human runs) → set `DEVTEM_MSIX_PUBLISHER` secret to light the MSIX leg. Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
