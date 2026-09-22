# Crash reporting (Sentry, opt-in)

Unhandled exceptions (app-domain, task pool, UI thread) are logged locally
and, when the user opts in AND a DSN is configured, reported to Sentry
with release + channel tags. Disabled twice by default: no DSN ships in
`ProductConfiguration.SentryDsn`, and `SettingsService.CrashReportsEnabled`
is off — paste a DSN and flip the Diagnostics toggle to enable.

```csharp
// ProductConfiguration.cs (never commit a real DSN; prefer the env override)
public const string SentryDsn = "";  // or DEVTEM_SENTRY_DSN at runtime
```

## Privacy posture (pinned by tests)

- SDK `SendDefaultPii = false` (`Services/SentryCrashReporter.cs`).
- Every envelope carries only `BuildStatusContext()`: version, channel,
  theme, language, log level — the exact key set is asserted in
  `CrashReportingTests.BuildStatusContext_CarriesNoPii`, so no path,
  username, or machine id can sneak in later.
- Breadcrumbs are static strings, except navigation which crumbs the
  registered route tag only (`NavigationService` rejects unknown tags
  before navigating, so deep-link garbage never reaches Sentry).
- The DSN value itself never reaches logs (`RepoHygieneTests`
  fails the build on any secret-adjacent `AppLog` call).
- Exported diagnostic bundles scrub user paths (`DiagnosticsService`
  replaces the local-app-data root, profile root, and username with
  placeholders before zipping).

Queued reports flush on clean exit (`CrashReportingService.Shutdown`,
called from `MainWindow` close); crash paths rely on best-effort
delivery.
