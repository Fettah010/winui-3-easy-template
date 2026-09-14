# MEL logging (`--logging mel`)

No Serilog, no log files. Events go to the debugger output, to the
in-memory buffer behind the diagnostics live tail, and (opt-in via
`DEVTEM_EVENT_LOG=1`) to the Windows Application log for Error/Critical.
The verbose toggle in Settings switches between Debug and Trace at
runtime by rebuilding the factory. App code logs through the `AppLog`
facade exactly like the Serilog backend, so call sites are identical.

## Tradeoffs vs Serilog (stated, not hidden)

- No rolling files: the diagnostics file view stays empty, export bundles
  carry the live buffer only, and "open log folder" has nothing to open.
- No JSON sidecar. Everything else (levels, buffer, Event Log opt-in)
  behaves the same.
