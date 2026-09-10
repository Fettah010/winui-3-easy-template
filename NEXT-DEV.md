# NEXT-DEV — Roadmap of top improvements

Working plan for the next development round, ranked by value. Keep this list
updated as items land; tick them off when done.

## 1. Persisted settings + theme switching (also fixes a real bug)

**Fix + addition.**

- Today the channel selector on `UpdatesPage` is **not persisted** and the
  startup auto-update **ignores it** (auto-check always uses `stable`).
- Add a small `SettingsService` persisted via `Windows.Storage.ApplicationData`
  (or a JSON file next to the exe) storing at least:
  - app theme: `System | Light | Dark` → applied to `RootElement.RequestedTheme`
  - update channel (used by both the auto-check and the Updates page)
  - last update-check / pending-update state
- Resolves AGENTS.md gotcha #5 (the `alpha`-vs-`dev` channel mismatch becomes
  visible and persistent).

## 2. Auto-update UX hardening

**Fix + addition.**

- Today every launch hits the feed, and when an update exists the app silently
  **re-downloads the whole package on every start** ("Later" forgets everything).
- Make it production-grade:
  - persist `lastCheckTime` → check at most 1× per day
  - persist `pendingVersion` → skip re-download if already downloaded
  - pass a `CancellationToken` to downloads; cancel on app exit
  - honor the persisted channel from #1
- Biggest quality win for the feature this repo is about.

## 3. Test project + CI test step

**Addition.**

- No tests exist; we already burned several failed CI runs hand-debugging.
- Add a small `xunit` project covering `UpdateService` (version/feed parsing),
  `LoggingService`, `AppInfo`, and one integration test that parses the live
  `releases.stable.json`.
- Wire `dotnet test` into `.github/workflows/release.yml` **before** `vpk pack`.

## 4. In-app Diagnostics / Logs page + richer log context

**Addition.**

- Logging exists but there's no way to see or export logs from inside the app.
- Add a Diagnostics page that shows the last ~500 log lines live (Serilog
  memory ring-buffer sink) with "Open Logs folder" and "Copy to clipboard".
- Enrich every log with OS/build/arch/session via `Enrich.WithProperty(...)`.
- This is the classic support workflow: user says "it broke" → you get a log export.

## 5. MVVM foundation (CommunityToolkit.Mvvm)

**Addition / refactor.**

- The repo is pure code-behind; every real app built from this template will
  immediately need `ObservableProperty` / `RelayCommand` and a DI pattern.
- Retrofit one page (Updates) as the reference MVVM implementation and add a
  lightweight service-locator / DI setup in `App`.
- Highest long-term leverage for template consumers.

---

## Honorable mentions (not dev-day, keep on the radar)

- **Authenticode signing** for the installer (`vpk --signParams` /
  `--signTemplate`, or Azure Trusted Signing).
- **Single-instance enforcement** to avoid update / state races.
- **CI smoke test** of the installed app before publishing the release.