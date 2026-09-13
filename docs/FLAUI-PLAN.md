# FlaUI Smoke Tests — Plan (deferred)

Status: planned, not started. This file is the spec to implement from.
Context: all 93 unit tests run headless; nothing proves the app *runs*.
FlaUI (UI Automation) automates the eyeball check (see UPGRADE-ROADMAP §4).

## Scope (keep it tiny — smoke, not coverage)

New project `UI/DevTemWinUi3.SmokeTests/` (xunit or MSTest, net10.0-windows):
1. `App_Launches_And_ShowsHome` — start exe, wait for main window
   (title from `AppMetadata.AppName`), assert visible, kill process.
2. `Can_Navigate_All_Pages` — click nav items Home/About/Settings
   (AutomationIds — add `AutomationProperties.AutomationId` to the three
   nav items when implementing), assert each page title text appears.
3. `Theme_Switch_Applies` — Settings → theme combo → Dark → assert
   theme applied (read back combo selection at minimum).
4. `Language_Switch_Applies` — Settings → language combo → es-ES →
   assert nav Home label reads "Inicio".

Rules: every test waits with retries (app start, splash ~5s, first-run
dialog must be dismissed first — click "Get Started" if present);
screenshots on failure into `TestResults/`; total suite budget 60s;
`dotnet test` runs it like everything else. Flaky on CI runners is the
known risk — retries + generous timeouts, never `Thread.Sleep` constants
without a wait loop.

## CI shape (`.github/workflows/ui-tests.yml`, later)

`windows-latest`, after build: `dotnet test UI/...` only (unit tests stay
in the existing job). Needs a real Windows session — runners provide one.
Do NOT run UI tests on every template-matrix combo; run once against the
default app. Template flag-off runtime proof stays manual screenshots.

## Prerequisites before starting

- The §3 a11y names are in (done) — FlaUI locates controls through them.
- Add `AutomationId` to nav items + key buttons (names localize, Ids don't).
- Decide xunit vs MSTest to match Tests/ (MSTest today).
