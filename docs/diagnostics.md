# Diagnostics — developer guide

> DevTem diagnostics is structured Serilog logging with an in-app viewer:
> file + in-memory sinks by default, metrics and startup traces built in,
> and every backend opt-in. Nothing leaves the machine unless you configure
> it.

## Pipeline

```
your code → Serilog (LoggingService.Initialize, Program.Run)
              ├─ Console (debugger, InvariantCulture template)
              ├─ File Logs/applog-YYYYMMDD.log (daily roll, 14 days, shared)
              ├─ InMemoryLogSink (newest ~1 000 events: live tail, export)
              ├─ JSON sidecar (opt-in: DEVTEM_JSON_LOGS=1, 7 days)
              └─ Event Log (opt-in: DEVTEM_EVENT_LOG=1, Error/Fatal only)
Unhandled paths → Serilog Fatal + Sentry (Program.Main catch shows a native
box with the log-folder path when the diagnostics feature is on)
```

`Serilog.Debugging.SelfLog` routes pipeline misconfiguration to the
debugger output. Minimum level is runtime-controlled (`LoggingLevelSwitch`,
persisted `VerboseLogging`: on in Debug, off in release).

## How to log

Message templates with named properties — never interpolation:

```csharp
// Good: structured, filterable, cheap.
Log.Information("Downloaded {Version} in {ElapsedMs}ms", version, elapsedMs);

// Bad: unparseable string, allocates on every call.
Log.Information($"Downloaded {version} in {elapsedMs}ms");
```

Prefer `Log.ForContext<T>()` (or the class logger) so `SourceContext`
flows into the live view, detail flyout, and export. Enrichment adds
`AppVersion` and `ThreadId` to every event automatically.

## Metrics and traces

```csharp
AppMetrics.RecordNavigation("orders");          // counters per page
AppMetrics.RecordUpdateCheck(elapsedMs);        // histogram + last value
AppMetrics.RecordException();                   // counted even unsent
AppMetrics.RecordStartupPhase("loading", ms);   // Metrics card breakdown

using var span = AppTrace.StartPhase("loading"); // null without a listener
```

The diagnostics page renders `AppMetrics.GetSnapshot()`; no listener or
exporter is required. The `Meter` (`DevTem.Diagnostics`) and
`ActivitySource` (`DevTem.Startup`) are OTLP-ready if you ever wire an
exporter.

## Crash reporting (Sentry)

1. Set the DSN (`ProductConfiguration.SentryDsn` or `DEVTEM_SENTRY_DSN`).
2. Flip the consent toggle on the diagnostics page (explicit opt-in;
   a DSN alone never sends; `IsEnabled` reflects actual flow).
3. Reports carry breadcrumbs (navigation, update checks, settings,
   language) plus an `app-status` context (version, channel, theme,
   language, log level). Add your own:
   `CrashReportingService.AddBreadcrumb("Order placed", "orders")`.

## Export and support flows

- Save the filtered view (`.log`) or a bundle zip (`status.json`,
  redacted `settings.json`, filtered + full log) from the Export card.
- The bundle's settings come from `SettingsBackupService.Capture()`,
  which only includes non-secret preferences.

## Redaction rules

- Never log tokens, passwords, or personal data; keep `SendDefaultPii`
  off.
- `ProductConfiguration` stays secret-free; CI-only values use
  `DEVTEM_*` environment overrides.
- New user-facing strings go in all three dictionaries plus
  `LocalizationCoverageTests` (see `docs/WORKFLOW.md`).

## Per-flag file map (`--health false`)

Dropped: `Pages/DiagnosticsPage.*`, `ViewModels/DiagnosticsPageViewModel.*`,
`Services/Native/CrashDialogNative.cs`, the VM test, and the feature guide.
Kept by design: the logging pipeline, buffer, metrics, traces, breadcrumbs,
and status snapshot (crash reports keep their context). UI references are
`#if (health)`-guarded in `MainWindow.xaml.cs` (route + nav item),
`ServiceLocator.cs` (VM registration), and `Program.cs` (crash dialog);
the nav item collapses at runtime since XAML carries no engine markers.
