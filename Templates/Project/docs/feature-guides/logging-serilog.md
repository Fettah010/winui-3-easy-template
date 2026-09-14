# Serilog logging (`--logging serilog`, the default)

The scaffold writes to the debugger console and to daily-rolling files
under `Logs/` next to the executable (`applog-YYYYMMDD.log`, 14 days
kept). Every event also lands in the in-memory buffer behind the
diagnostics live tail. Opt-ins: `DEVTEM_JSON_LOGS=1` for a
machine-readable sidecar, `DEVTEM_EVENT_LOG=1` for the Windows
Application log (Error/Fatal only). App code logs through the `AppLog`
facade (`Microsoft.Extensions.Logging`), so swapping backends later
touches only `Services/LoggingService.cs`.

Lighter or no logging instead? Re-scaffold with `--logging mel`
(debugger + Event Log only, no files — see `logging-mel.md`) or
`--logging none` (no logging packages at all).
