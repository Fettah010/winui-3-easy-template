# Usage telemetry (opt-in sample, never default-on)

This template ships **zero usage telemetry**. Crash reporting (see
`crash.md`) is a separate, DSN-gated concern. If you want product analytics,
the envelope choice is **Sentry** — the SDK is already in your graph when
`--crash true` (the default; `Build/Features.Crash.props`), so the sample
below adds no package. AppCenter is not an option: it retired 2025-03-31.

## Privacy posture (same pins as crash reporting)

- `SendDefaultPii = false` (`Services/SentryCrashReporter.cs`) — keep it.
- Context allowlist only: version, channel, theme, language, log level (the
  `BuildStatusContext()` shape asserted in `CrashReportingTests`).
- No DSN in source: `ProductConfiguration.SentryDsn` stays empty; prefer the
  `DEVTEM_SENTRY_DSN` runtime override. `RepoHygieneTests` fails on
  secret-adjacent logging — telemetry calls must never carry identifiers.
- Off by default behind an explicit user toggle (pattern below). Never phone
  home on first run, never gate features on consent state.

## Drop-in sample (paste, don't rewire)

```csharp
// Services/TelemetryService.cs — sample, ships nowhere by default.
using Sentry;

public static class TelemetryService
{
    // Caution: scaffolded SettingsService has no telemetry flag — add one
    // (persisted bool, default false) with a Diagnostics toggle, mirroring
    // CrashReportsEnabled, before calling anything here.
    public static bool Enabled { get; set; }

    public static void Track(string eventName)
    {
        try
        {
            if (!Enabled)
                return;
            if (string.IsNullOrWhiteSpace(DeploymentConfiguration.SentryDsn))
                return;
            SentrySdk.CaptureMessage(eventName);
        }
        catch { }
    }

    public static void Crumb(string message, string category)
    {
        try
        {
            if (!Enabled)
                return;
            SentrySdk.AddBreadcrumb(message, category);
        }
        catch { }
    }
}
```

Rules for the sample: event names are static strings (never user content,
paths, or query strings); one call site per user action (button clicks, not
keystrokes); the toggle lives in Diagnostics next to crash reporting so both
consent switches are reviewable in one place; `--crash false` scaffolds have
no Sentry SDK — add the `Sentry` package yourself or keep telemetry out.
