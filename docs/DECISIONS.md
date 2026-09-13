# Decisions — why things are the way they are

Append-only. One entry per decision: date, decision, reason. Full context
in git history; this file saves the next agent the archaeology.

- **2026-09-13 — No VSIX.** VS's New Project dialog runs the template
  engine, so the `DevTem.Templates` NuGet package IS the VS channel.
  (`icon.png` + `displayName` + `tags` ship for it.)
- **2026-09-13 — Hand-conditioned template mirror.** `Templates/Project/`
  carries `#if (tray|updates|database)` blocks and `DevTemWinUi3`-prefixed
  identifiers (e.g. `DevTemWinUi3Tray_`) so `dotnet new` renames them;
  `init-template.ps1` handles the short forms. Proven by
  `Scripts/test-mirror-parity.ps1` + the scaffold matrix.
- **2026-09-13 — Scheme spelled WITH `://` in source.**
  `AppMetadata.ProtocolPrefix = "devtem://"` lets both rename engines
  retarget the scheme with zero extra rules; bare form derives from it.
- **2026-09-13 — No `BlockInput` in smoke tests.** It swallows synthetic
  input too, killing FlaUI's own clicks. Occupied-desktop robustness comes
  from foreground + topmost staging, cursor confinement, and click
  retries instead.
- **2026-09-13 — Smoke harness dismisses startup dialogs.** First-run AND
  what's-new (a version bump triggers the modal one). Located by title,
  launch-only, to protect the 60s budget.
- **2026-09-13 — Tests ship verbatim into scaffolds.** Hence
  scheme/identity-agnostic assertions (matrix scaffolds `acme://` etc.).
- **2026-09-13 — `TestResults/` git-ignored.** MSTest trx + FlaUI
  screenshots are CI artifacts, not sources.
- **2026-09-13 — Next beta is 0.0.3-beta, not 0.0.2.** Tags `v0.0.2-beta`
  and `v0.0.2` exist; Velopack versions must increase.
- **2026-09-13 — Deleted plan files stay deleted.** `FLAUI-PLAN.md`,
  `UPGRADE-ROADMAP.md`, root `ROADMAP.md` were all done; the live plan is
  `docs/RESTRUCTURE-PLAN.md`, decisions live here.
- **2026-09-13 — Agents never commit/push/tag unasked.** Releases are
  prepared fully, then handed over as exact commands for the human.
- **2026-09-13 — v0.0.4-beta is a pipeline release.** No app behavior
  changes since 0.0.3-beta (one P1/P2 commit); the what's-new dialog
  says so honestly instead of repeating 0.0.3 highlights.
- **2026-09-13 — Never blind-copy app→template for conditioned files.**
  Phase 1 wiped the `#if (tray|updates)` guards in the template's
  `MainWindow`/`SettingsPage` (parity normalizes `#if` regions, so only
  the matrix caught it). Copy, then restore guards, then matrix.
