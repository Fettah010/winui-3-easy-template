# Flexibility Plan — make DevTem composable without losing its smoothness

Supersedes `docs/DIAGNOSTICS-PLAN.md` (shipped in v0.0.3-beta, deleted).
Coexists with `docs/DISCOVERY-PLAN.md` (discovery/SEO, still open) and
`docs/TEMPLATE-REUSABILITY-ROADMAP.md` (partially open, needs triage).

Status: `[ ]` todo · `[~]` in progress · `[x]` done.
Mirror: this file ships verbatim into scaffolds
(`Templates/Project/docs/FLEXIBILITY-PLAN.md`); keep the copies identical.

## Mission

Every *vendor lock-in* becomes a scaffold-time choice with a smooth
default; everything else stays curated. A user can say "no Velopack, no
Serilog, no Sentry, English-only, no tests" and get a green 0/0 build
with zero dead code — as easily as today's `--tray false`.

Non-goals: runtime plugin systems, a `--profile` mega-parameter (the
engine cannot combine it with per-feature overrides — decided), touching
`devtem-page`, or replacing the hand-conditioned mirror.

## Principles (binding)

1. **Defaults = today's Production.** Existing users and scripts change nothing.
2. **Choices only at seams.** New `#if` allowed only in `Program.cs`,
   `ServiceLocator.cs`, `Build/Features.*.props`, and Settings UI
   sections. If a choice needs `#if` inside a service, redesign the seam.
3. **One axis = one props file per value**, `Exists`-guarded imports.
   Scaffolded apps never see the axis, only files.
4. **Representative matrix, never cartesian.** All-on, minimal, each
   non-default value once, one pairwise spot-check per phase.
5. **Generated docs describe what you got.** `FEATURES.md` /
   `template-features.json` graduate from bool tables to name→value;
   per-value guides so no scaffold documents code it doesn't have.
6. **Short-flag check.** Every new param name is verified with
   `dotnet new devtem-winui -h` (the `-di`/`--displayName` lesson that
   forced the `health` name).

## Target parameter model

Bools (unchanged): `tray`, `database`, `http`, `health`, `attribution`.
New choices: `updates: velopack|basic|none` (default `velopack`),
`logging: serilog|mel|none` (default `serilog`). New bools:
`crash` (default true), `localization` (default true), `tests`
(default true). Presets stay documented flag sets; Minimal becomes
`--tray false --updates none --database false --http false --health
false --logging none --crash false --localization false --tests false
--attribution false`.

Deliberately still opinionated: MEDI composition root, single-instance +
protocol, settings/theme/notification core, Mica + unpackaged + MSTest +
Toolkit defaults. Freedom where lock-in is real; curation where choice
is trivia.

## Phase 0 — seams, no template surface change [x] DONE

Goal: call sites stop touching vendor APIs; backends become swappable;
the mirror learns to protect `#if` guards. Zero behavior change, no new
flags, no matrix growth.

- [x] 0a. `Services/AppLog.cs`: static facade over MEL `ILogger`
  (`Debug/Information/Warning/Error/Fatal`, exception + params
  overloads, never throws). `LoggingService.Initialize` wires it via the
  official `Serilog.Extensions.Logging` bridge; Serilog stays the
  backend. Migrate all 98 `LoggingService.Log.*` + `Serilog.Log` call
  sites (13 files, + mirror). Keep `LevelSwitch`/`MinimumLevel`/
  `EventBuffer` untouched. New `Tests/Services/AppLogTests.cs` (routing
  via an injected test factory). Acceptance: grep finds no
  `LoggingService.Log.` outside `LoggingService`/`DiagnosticsService`
  internals; tests green.
- [x] 0b. `Services/Diagnostics/InMemoryLogSinkLoggerProvider.cs`: MEL
  `ILoggerProvider`→`ILogEventSink` adapter (level map, exception +
  rendered message preserved). Implemented + unit-tested, **unwired**
  until Phase 2 (wiring now would double-emit into the buffer).
  Acceptance: MEL `LogError` through the provider appears in
  `SnapshotNewestFirst` with level + exception intact.
- [x] 0c. Extract `Sentry` → `Build/Features.Crash.props`,
  `System.Diagnostics.EventLog` → `Build/Features.Logging.props`
  (`Exists`-guarded imports in both csprojs; mirrored byte-identical).
  Toolkit/MEDI stay put (deferred axes). Acceptance: build 0/0 both
  trees; parity OK.
- [x] 0d. Parity-script guard manifest: `$requiredGuards` maps each
  conditioned file to its ordered `#if` conditions; the script fails on
  mismatch (catches blind-copy guard wipes that hash comparison cannot).
  Update the manifest whenever guards intentionally change. Acceptance:
  deleting one guard in the mirror fails parity with the file named.
- [x] 0e. Verify: build 0/0, `dotnet test`, parity, full 9-combo matrix.
  No template.json change, so no new combos.

## Phase 1 — `updates: velopack|basic|none` [x] DONE

- [x] 1a. `BasicGithubUpdateService : IUpdateService` (releases API →
  semver compare → download asset → launch → quit) + `NullUpdateService`.
- [x] 1b. `Program.cs`: `#if (updates == 'velopack')` around
  `VelopackApp.Build()`; `ServiceLocator` registers per value.
- [x] 1c. `template.json`: choice symbol, `==` modifiers (scripts, CI
  job, per-value guides `updates-velopack.md`/`updates-basic.md`),
  `Features.Updates.{Velopack,Basic}.props`; README presets + help text.
- [x] 1d. Matrix: `+upd-basic`, `+upd-none` combos; manifest-value
  assertions. Acceptance: each value builds 0/0, tests pass, no
  Velopack bits in `basic`/`none` scaffolds.

## Phase 2 — `logging: serilog|mel|none` [x] DONE

- [x] 2a. `LoggingService.Initialize` switches backend: Serilog pipeline
  (unchanged) / MEL factory (Debug + EventLog-gated + 0b provider) /
  no-op + buffer-if-health. File-dependent UI degrades with a documented
  "file sink unavailable" state (`mel` has no log files — stated, not hidden).
- [x] 2b. Serilog packages → `Features.Logging.Serilog.props`;
  `template.json` choice + guide; presets updated.
- [x] 2c. Matrix: `+log-mel`, `+log-none`; acceptance as 1d (no Serilog
  bits in `mel`/`none` scaffolds; diagnostics page live in all three).

## Phase 3 — `crash`, `localization`, `tests` bools [x] DONE

- [x] `crash:false` excludes service + props + guards 7 call sites +
  crash tests. `localization:false` excludes Fr/Es strings + picker
  (keys resolve English). `tests:false` excludes `Tests/` (matrix skips
  the test step). Acceptance: `minimal` = all-off, 0/0, tests pass.

## Phase 4 — polish + version policy [x] DONE

- [x] `TEMPLATE-GUIDE.md`, README profiles, `template-features.json`
  schema (name→value), per-value guides, `DECISIONS.md` per deferred
  item (MVVM choice, test-framework choice, MSIX packaging, init
  wizard — each with its cost≫value reason). Template package minor
  bump per user-facing phase; app patch/beta as usual.

## Verification (every phase)

`dotnet build -c Debug -p:Platform=x64` 0/0 → `dotnet test` →
`test-mirror-parity.ps1` → `test-templates.ps1` (representative combos)
→ `build-msix.ps1 -DryRun` when packaging-adjacent. One phase = one
commit (commit only on direct request).
