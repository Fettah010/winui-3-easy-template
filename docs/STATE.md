# Session State — mutable, update every session

> Agents: read `AGENTS.md` → this file → `docs/WORKFLOW.md`, then run
the bootstrap checklist in `AGENTS.md`. Refresh this file before ending
the session (goal, tree, CI, next). Conventions live in `AGENTS.md`;
this file holds only **current facts**.

- **Goal:** store automation 1–6 done uncommitted (plan `docs/STORE-AUTOMATION-PLAN.md` all ticked) — commit on request.
- **Version:** app `0.0.5-beta`; template package `0.3.0`.
- **Tags:** `v0.0.5-beta` and `templates-v0.3.0` shipped and CI-green.
- **Branches:** `main` @ `5ab75ea` (+ uncommitted store automation 1–6); `beta` tracks `v0.0.5-beta`.
- **Tree:** uncommitted — items 1–4 (prior session) + item 6 `new-store-listing.ps1` (README-derived drafts, git-ignored `Store/`) + item 5 `submit-store.ps1` (submission API, env creds) + manual `store-submit.yml` (msix-only exclude) + matrix presence asserts + templates.yml CI paths + guide/AGENTS rows.
- **CI health (last known):** local build 0/0, tests 212+4 skipped, parity OK (145 files), matrix subset PASSED (msixstore, allon, nosetup, msixapp) + init-template scratch, `-Validate`/submit-WhatIf/listing generation proven, YAML parses. Live Store round-trip still needs a real tenant (first real submission is the proof).
- **Next:** commit on request. Open: `DISCOVERY-PLAN.md` A3/A4+B/C/D, roadmap triage (85 boxes), add-page for en-only scaffolds.
