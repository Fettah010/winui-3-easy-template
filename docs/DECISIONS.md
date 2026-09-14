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
  `docs/TEMPLATE-REUSABILITY-ROADMAP.md`, decisions live here.
- **2026-09-13 — Agents never commit/push/tag unasked.** Releases are
  prepared fully, then handed over as exact commands for the human.
- **2026-09-13 — v0.0.4-beta is a pipeline release.** No app behavior
  changes since 0.0.3-beta (one P1/P2 commit); the what's-new dialog
  says so honestly instead of repeating 0.0.3 highlights.
- **2026-09-13 — Never blind-copy app→template for conditioned files.**
  Phase 1 wiped the `#if (tray|updates)` guards in the template's
  `MainWindow`/`SettingsPage` (parity normalizes `#if` regions, so only
  the matrix caught it). Copy, then restore guards, then matrix.
- **2026-09-13 — Keep diagnostics and localization baseline-only in Phase 2.**
  Logging remains included because global exception paths depend on it.
  Crash reporting remains DSN-gated and inactive by default. Localization
  remains included with `en-US`, `es-ES`, and `fr-FR`; no off switch is
  exposed until resource and binding removal is proven safe.
- **2026-09-13 — Profiles are documented presets over one composable template.**
  The public template keeps independent boolean feature switches. Minimal,
  Desktop, and Production are stable command presets, tested through the
  same scaffold matrix. A `--profile` symbol was intentionally deferred
  because the template engine cannot both derive boolean defaults from a
  profile and reliably preserve explicit per-feature overrides.
- **2026-09-13 — Deployment configuration is generated, secret-free, and layered.**
  `ProductConfiguration.cs` owns checked-in product defaults and
  `DeploymentConfiguration` applies `DEVTEM_*` environment overrides for
  developer and CI use. Runtime preferences remain in `SettingsService`;
  template-time identity remains in template symbols. No JSON configuration
  package or committed secret is required.
- **2026-09-14 — Branding uses one validated asset pipeline.**
  `ProductConfiguration.cs` owns product-facing color and deployment
  placeholders, while `AppMetadata` owns runtime identity. A single square
  PNG is the input to `set-app-icon.ps1`; the script validates extension,
  dimensions, aspect ratio, and generated output before replacing assets.
  ICO/logo variants then feed the app, tray, shortcut, splash, About, MSIX,
  and package surfaces.
- **2026-09-14 — Flexibility plan supersedes diagnostics plan.**
  `docs/DIAGNOSTICS-PLAN.md` shipped (Phases 0–4 in v0.0.3-beta) and is
  deleted from both trees; the live engineering plan is
  `docs/FLEXIBILITY-PLAN.md` (mirrored verbatim). `DISCOVERY-PLAN.md`
  and `TEMPLATE-REUSABILITY-ROADMAP.md` are kept: discovery still has
  open phases (A3/A4, B, C, D) and the roadmap has 85 open boxes that
  need triage before any retirement.
- **2026-09-14 — Phase 0 logging seam: AppLog over MEL, Serilog stays.**
  Call sites move from static Serilog (`LoggingService.Log`,
  `Serilog.Log`) to a new `Services/AppLog.cs` static facade backed by
  `Microsoft.Extensions.Logging.ILogger` via the official
  `Serilog.Extensions.Logging` bridge. `LevelSwitch`/`MinimumLevel`/
  `EventBuffer` stay Serilog-typed until Phase 2; the MEL
  `InMemoryLogSinkLoggerProvider` ships implemented + tested but unwired
  (wiring now would double-emit into the buffer).
- **2026-09-14 — Opinion packages move to Exists-guarded props.**
  `Sentry` → `Build/Features.Crash.props`,
  `System.Diagnostics.EventLog` → `Build/Features.Logging.props`
  (mirrored byte-identical), preparing the `crash`/`logging` flags.
  Toolkit/MEDI stay in the csproj (deferred axes).
- **2026-09-14 — Parity script owns a guard manifest.**
  Hash comparison cannot catch blind-copy `#if` wipes (Phase-1
  incident), so `test-mirror-parity.ps1` carries `$requiredGuards`
  (ordered `#if` conditions per conditioned file) and fails on mismatch.
  Update the manifest whenever guards intentionally change.
- **2026-09-14 — Phase 1: no NullUpdateService.**
  `updates=none` keeps the status-quo null registration (the VM is
  null-tolerant by design), so a Null impl would be dead weight. Basic
  owns its `HttpClient` (stays orthogonal to the `http` flag), reports
  `IsInstalled=true` (no installer to detect; checks run even in dev,
  which makes the flow testable), and needs a `.exe` release asset.
  `BackgroundUpdateService` serves both backends through guarded
  concrete `Updater`/`CheckInterval` properties (CA1859 forbids the
  interface-typed version); its tests stay velopack-only (basic would
  hit the network). `--updates false` is now `--updates none` across
  presets, guides, and the matrix (10 combos).
- **2026-09-14 — Phase 2: LogEntry is the buffer currency, no file gate.**
  Serilog `LogEvent` and MEL records both convert to a backend-agnostic
  `LogEntry` (MEL `LogLevel` canonical), so the diagnostics page, export,
  and tests compile for every backend with zero `#if`. `MinimumLevel`
  went MEL-typed everywhere; `LevelSwitch` and `LoggingService.Log` are
  deleted (zero references). File APIs needed no gate (no files exist
  without the serilog backend); the VM only auto-selects the live tail
  and hides the file view via `HasFileSink`. `none` still buffers when
  health is on (buffer provider gated on `#if (health)` in every
  backend). `mel` tradeoff stated in its guide: no files, no JSON
  sidecar. `BackendName` rides `__LOGGING__` replaces (no extra `#if`).
- **2026-09-14 — Phase 3: crash is a veneer, not guards.**
  Instead of guarding ~16 call sites, `CrashReportingService` became an
  SDK-free veneer over `ICrashReporter` (one `#if (crash)` at creation;
  `SentryCrashReporter` + props excluded when off). All callers compile
  unchanged and no-op; `CrashReportingTests` run everywhere (they assert
  the inert path). The diagnostics crash toggle hides via a `#if`ed VM
  visibility property (XAML binds unconditionally). CA1859 on the seam
  field is suppressed by design.
- **2026-09-14 — Phase 3: tests=false keeps a shell project.**
  `Tests/**/*.cs` is excluded but the csproj + sln entry stay, so
  `dotnet build` (solution) works and `dotnet test` passes vacuously;
  the matrix skips the test step and asserts the absence instead.
  Translation-asserting tests are handled per kind: coverage +
  service-loc + toast-strings excluded/guarded for en-only, while the
  settings round-trip test adapts at runtime (no `#if`).
- **2026-09-14 — Phase 4: deferred axes, with reasons.**
  Documented, not forgotten: (a) MVVM-toolkit choice — would fork every
  VM file while Toolkit remains the community standard (cost ≫ value);
  (b) test-framework choice — doubles test maintenance for a preference
  MSTest already satisfies as the VS default; (c) `packaging: msix` at
  scaffold time — needs identity, signing, and CI redesign against the
  unpackaged core, deserves its own plan; (d) interactive init wizard —
  script post-actions need user approval, worse UX than flags; revisit
  only past ~12 flags. Each gets reconsidered on user demand, not on
  principle.
- **2026-09-14 — Version policy for flexibility work.**
  New template surface (params, values) = template package MINOR bump
  (0.1.6 → 0.2.0); app follows patch/beta as usual (Velopack versions
  must keep increasing, one version per channel). Param names and choice
  values are stable from 0.2.0: renames would silently break user
  scripts, so new options arrive as new values, never renames.
- **2026-09-14 — Generated documentation is profile-aware by composition.**
  `docs/FEATURES.md` reports the selected switches and next steps, while
  feature-specific guides are excluded at scaffold time when their feature is
  disabled. The base README remains stable and points to the generated
  summary instead of pretending every optional service exists.
