# Diagnostics Plan — make diagnostics a reason to choose DevTem

Goal: the template where logging, crash reporting, and the in-app
diagnostics page work out of the box, are trivially customizable, and
teach best practices by example. Template-first: every capability must
survive `dotnet new` under all feature-flag combos, degrade gracefully
with no backend configured, never throw, and never exfiltrate PII by
default.

Status: `[ ]` todo · `[~]` in progress · `[x]` done.
Checkboxes update in the same commit as the work (per `docs/WORKFLOW.md`).

## Principles

- **Template-first, not app-first.** Scheme/identity-agnostic, survives all
  `--tray/--updates/--database/--http` combos; unconfigured backends are
  silent no-ops (the `CrashReportingService` DSN-gated precedent).
- **Never-throw observability.** Diagnostics code must never crash the app
  it observes (`DiagnosticsService` never-throw contract covers all new code).
- **Three pillars, desktop-sized.** File logs + in-process metrics +
  startup-scoped traces. Platform APIs (`Meter`, `ActivitySource`) over
  heavy SDKs: no OpenTelemetry SDK in the template.
- **Privacy by default.** Nothing leaves the machine unless the developer
  configures a backend (`SendDefaultPii = false`, secret-free
  `ProductConfiguration`, `DEVTEM_*` env overrides).
- **VM stays UI-free; page stays thin.** Logic in testable services/VMs;
  the page only renders localized composed text.

## Research grounding

- Microsoft .NET diagnostics docs: `ILogger` default API,
  `System.Diagnostics.Metrics.Meter` for metrics, `ActivitySource` for
  tracing; OpenTelemetry as optional export, not required API.
- OpenTelemetry .NET best practices: structured logging over
  interpolation, `LoggerMessage` source generation on hot paths, singleton
  `ActivitySource`, log/trace correlation.
- Serilog file-sink docs: `shared: true` multi-process behavior,
  `CompactJsonFormatter` for machine-readable files, `buffered: true` /
  `Serilog.Sinks.Async` for write latency, `SelfLog` for pipeline
  self-diagnostics.
- Highest-leverage move: an **in-memory ring-buffer sink** (bounded, last
  ~1 000 events with level + timestamp + properties) — one component that
  unlocks live-tail, structured filtering, and export.

## Phase 0 — Quick wins [x] DONE

- [x] 0.1 Startup time on the page: `Program.StartupStopwatch` →
  `DiagnosticsStatus.StartupElapsedMs`, localized `DiagnosticsStartup` line.
- [x] 0.2 SelfLog wiring: `Serilog.Debugging.SelfLog` to debugger output
  in `LoggingService.Initialize` (file escalation deferred).
- [x] 0.3 Effective log-level display in status (`LoggingService.MinimumLevel`
  → `DiagnosticsStatus.LogLevel`, localized `DiagnosticsLogLevel` line).
- [x] 0.4 Startup-crash dialog: best-effort localized native message box
  with the log-folder path in `Program.Main`'s catch
  (`Services/Native/CrashDialogNative.cs` + `Program.BuildStartupCrashMessage`,
  English fallback when localization isn't initialized).

## Phase 1 — Structured logging pipeline [x] DONE

- [x] 1.1 In-memory ring-buffer sink (`Services/Diagnostics/…`):
  `ILogEventSink`, capacity 1 000, timestamp/level/message/properties;
  `DiagnosticsService.GetBufferedEvents()` (newest-first, never throws).
- [x] 1.2 Enrichment: `FromLogContext`, app version, thread-id
  (dependency-free `ThreadIdEnricher`; no new packages).
- [x] 1.3 `LoggingLevelSwitch` persisted in settings + `VerboseLogging`
  toggle on the diagnostics page (on in Debug, off in release).
- [x] 1.4 Optional Compact-JSON sidecar file, off by default
  (`DEVTEM_JSON_LOGS=1`, core `JsonFormatter`, 7-day retention); viewer
  keeps reading the text file.

- [ ] 1.1 In-memory ring-buffer sink (`Services/Diagnostics/…`):
  `ILogEventSink`, capacity 1 000, timestamp/level/message/properties;
  `DiagnosticsService.GetBufferedEvents()` (newest-first, never throws).
- [ ] 1.2 Enrichment: `FromLogContext`, app version, thread-id.
- [ ] 1.3 `LoggingLevelSwitch` persisted in settings + `VerboseLogging`
  toggle (default off in release) for on-demand verbose capture.
- [ ] 1.4 Optional Compact-JSON sidecar file, off by default
  (`DEVTEM_JSON_LOGS=1`); viewer keeps reading the text file.

## Phase 2 — Diagnostics page UX [x] DONE

- [x] 2.1 Live tail mode from the ring buffer (1 s `DispatcherTimer`,
  pause-on-scroll with "N new" resume button); file stays the history source.
- [x] 2.2 Structured filter bar: source filter, exceptions-only toggle,
  regex on the shared search box (invalid pattern shows a hint); file
  text/level search unchanged as the simple tier.
- [x] 2.3 Event detail flyout: timestamp, level, source, properties table,
  message, exception + full-detail copy (message selectable in place).
- [x] 2.4 Export center: save filtered view (`.log`) + diagnostic bundle
  zip (`status.json`, redacted `settings.json`, filtered + full log).
  Picker extended with file-type parameter (default `.json`, old calls
  unchanged).
- [x] 2.5 Status upgrade: log files (count + KB), buffered events, packaged
  yes/no, pending update, database size (`-1` = none, path by convention so
  no-database scaffolds keep building); status text selectable.
- [x] 2.6 Virtualized event list (`ListView`; file `TextBlock` kept for the
  200-line tail).

- [ ] 2.1 Live tail mode from the ring buffer (1 s `DispatcherTimer`,
  pause-on-scroll, "N new lines" jump); file stays the history source.
- [ ] 2.2 Structured filter bar: property filter, exception-only toggle,
  optional regex — current text/level search stays the simple tier.
- [ ] 2.3 Event detail flyout: timestamp, level, source, properties table,
  formatted exception, per-field copy.
- [ ] 2.4 Export center: save filtered view + **diagnostic bundle zip**
  (filtered log, current log file, redacted settings, status JSON).
- [ ] 2.5 Status upgrade: startup time, log level, log-dir size, buffer
  usage, packaged state, Velopack/pending-update state, DB size when the
  database feature is on. Every row copyable.
- [ ] 2.6 Virtualized event list (cap rendered lines, "latest N" notice)
  when the single-`TextBlock` approach proves insufficient.

## Phase 3 — Metrics, traces, crash context [x] DONE

- [x] 3.1 `System.Diagnostics.Metrics` (`Services/Diagnostics/AppMetrics.cs`,
  singleton `Meter` + last-value store): startup phases, navigations,
  update checks, exceptions + in-process Metrics card (selectable).
- [x] 3.2 Startup trace (`Services/Diagnostics/AppTrace.cs`, reused
  `ActivitySource` singleton): splash → services → window spans in
  `App.OnLaunched`; no-ops without a listener, breakdown on the Metrics
  card via `AppMetrics` regardless.
- [x] 3.3 Sentry breadcrumbs at navigation, update start/finish, settings
  reset/export/import, language switch + status snapshot as scope context
  (`CrashReportingService.BuildStatusContext`, tested).
- [x] 3.4 Crash-report opt-in toggle on the page (explicit opt-in, off by
  default; DSN alone never enables sending; `IsEnabled` reflects actual
  flow).
- [x] 3.5 Windows Event Log sink for Error/Fatal, opt-in via
  `DEVTEM_EVENT_LOG=1` (one-time write-permission probe, never throws).
  Requires the `System.Diagnostics.EventLog` package (10.0.0, cached).

- [ ] 3.1 `System.Diagnostics.Metrics` (`Services/Diagnostics/AppMetrics.cs`,
  singleton `Meter`): startup-time histogram, update-check duration,
  navigation counts, exception count + in-process "Metrics" card.
- [ ] 3.2 Startup trace (`ActivitySource`, reused singleton): splash →
  services → window → first render breakdown on the Metrics card.
- [ ] 3.3 Sentry breadcrumbs at key transitions + status snapshot as scope
  context in `CaptureException`.
- [ ] 3.4 Crash-report opt-in toggle on the page (controls *sending*;
  DSN still required from config).
- [ ] 3.5 Optional Windows Event Log sink for Error/Fatal when installed,
  behind a config flag (off by default).

## Phase 4 — Template packaging [x] DONE

- [x] 4.1 `health` bool flag (default on; `--health false` drops
  the page, VM, crash dialog + tests). Engine note: the flag is named
  `health`, not `diagnostics` — `dotnet new` derives `--param:X` fallback
  names for `di*`-prefixed symbols (`-di` belongs to `--displayName`).
  Design note: the flag drops the user-facing *surface*, not the pipeline
  (buffer, metrics, traces, breadcrumbs, status stay — they're core
  plumbing like Serilog itself).
  Guards only where excluded types are referenced (`MainWindow` route +
  nav collapse, `ServiceLocator` VM registration, `Program` crash-dialog
  call); template symbol + modifiers + `AppFeatures.Health` +
  `FEATURES.md` row + `template-features.json` + `nodiag` matrix combo.
- [x] 4.2 `docs/diagnostics.md` developer guide (pipeline, logging style,
  metrics/traces, Sentry, export, redaction, flag file map) with a citable
  "DevTem diagnostics is …" passage.
- [x] 4.3 `devtem-page` item template: commented `ForContext` +
  breadcrumb examples in the VM stub.

- [ ] 4.1 Feature-flag granularity (`basic` vs `full` diagnostics) with
  `#if` guards in the existing `(tray|updates|database)` style;
  `docs/template-features.json`, `TEMPLATE-GUIDE.md` §2c, matrix update.
- [ ] 4.2 `docs/diagnostics.md` developer guide: pipeline diagram, sink
  catalog, message-template style guide, backend how-tos, redaction rules,
  per-flag file map + citable "DevTem diagnostics is …" passage.
- [ ] 4.3 `devtem-page` item template: commented `ForContext` +
  breadcrumb examples so scaffolded code is observable from day one.

## Verification (per `docs/WORKFLOW.md`)

- Fast tier every commit (build 0/0 + `dotnet test Tests/ -p:Platform=x64`).
- New headless-proof rule: every diagnostics feature ships a unit test
  that passes without the app running (only timer ticks stay code-behind).
- Matrix + parity tiers for any `Templates/**` touch; verbatim mirror by
  default, `conditioned` allowlist only with justification.
- Live UI tier for page changes: extend smoke (e.g. type in
  `DiagnosticsSearchBox`, assert `DiagnosticsLogCountText` changes) +
  screenshot in the summary.
- Loc discipline: batch user-facing strings per phase (one loc commit per
  phase), all 3 languages + coverage tests, no hardcoded versions.

## Non-goals (do not build)

- No OpenTelemetry SDK dependency in the template.
- No database log sink by default (couples diagnostics to the optional
  database feature).
- No auto-upload of anything; all export is user-initiated.
- Perf guardrails: bounded buffer + drop-oldest, `shared: true` file sink,
  capped rendered lines, sampled hot-path instrumentation.
