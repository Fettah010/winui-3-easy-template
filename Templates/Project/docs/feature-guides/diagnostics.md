# Diagnostics page

The Diagnostics page (footer navigation) shows app status, rolling Serilog
log files, a live event tail, and in-process metrics. It is enabled by
default; scaffold with `--health false` to drop it.

## What stays when the page is dropped

Only the user-facing surface is removed (the page, its view model, and the
startup-crash dialog). The pipeline stays: Serilog file logging, the
in-memory event buffer, `AppMetrics`/`AppTrace` instrumentation, Sentry
breadcrumbs, and the `DiagnosticsService` status snapshot (crash reports
keep their context).

## Page tour

- **Status**: version, channel, theme, language, startup time, log level,
  log files (count + size), buffered events, packaged state, pending
  update, database size, crash-reporting state. Selectable text.
- **File view**: per-file tail (200 lines) with text + level filtering.
- **Live view**: ring-buffer tail with source filter, exceptions-only and
  regex toggles, pause-on-scroll, click an event for full details.
- **Export**: save the filtered view (`.log`) or a diagnostic bundle zip
  (`status.json`, redacted `settings.json`, filtered + full log).
- **Toggles**: verbose logging (extra debug detail) and crash-report
  consent (requires a Sentry DSN to flow).

## Backends (all opt-in)

- Machine-readable logs: `DEVTEM_JSON_LOGS=1` adds a JSON sidecar file.
- Windows Event Log: `DEVTEM_EVENT_LOG=1` forwards Error/Fatal.
- Sentry: set `SentryDsn` (or `DEVTEM_SENTRY_DSN`) and flip the consent
  toggle on the page. Nothing leaves the machine otherwise.
