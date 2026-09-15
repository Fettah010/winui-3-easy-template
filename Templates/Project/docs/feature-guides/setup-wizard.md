# Setup wizard

This scaffold includes the first-run setup wizard (portable only):
a native Windows 11 WinUI 3 walkthrough — welcome, install location,
shortcuts, launch options — instead of a bare first launch.

## Steps

`Pages/SetupWizardPage` with a stock `PipsPager` indicator:

1. **Welcome** — what the wizard sets up, skippable any time.
2. **Location** — the data folder (default `%LocalAppData%\<Name>`,
   outside the app folder so Velopack updates never orphan it).
   `Browse` opens a folder picker; the choice is recorded and created
   on Finish.
3. **Shortcuts** — Desktop + Start menu toggles (same WScript.Shell
   shape as `Scripts/create-shortcut.ps1`, applied in-app).
4. **Launch** — start-with-Windows toggle (registry Run unpackaged,
   `StartupTask` consent prompt packaged) plus the deep-link note
   (per-user HKCU registration, no admin).
5. **Done** — static success glyph (no new dependencies) + what
   happens next.

State lives in `ViewModels/SetupWizardViewModel` (step math +
persisted choices, headless-tested); OS side effects live in
`Services/SetupWizardService` (never-throw, untested headless by
house rule — proven by smoke instead).

## Orchestration

`FirstRunDialogService` routes first runs to `setupwizard` when all
of these hold: unpackaged, `AppFeatures.SetupWizard`, wizard not yet
completed. Otherwise first-run shows today's welcome dialog.
Completion (or Skip) persists `SetupWizardCompleted` ("don't show
again") and marks first-run shown, then returns to Home.

`--setup false` drops the pages, the ViewModel, the service, its
tests, and this guide. `--distribution msix` ignores setup (the
build warns): Windows owns install location and shortcuts there —
nothing to set up.

## Customization

- Reorder/rename steps in the ViewModel (`StepCount` + visibility
  props); the PipsPager follows `SelectedStepIndex`.
- Per-step toggles: add a bool + persisted key, same pattern as
  the shortcut toggles (one bool now by flag-count discipline —
  see the Distribution Plan §6).
- Animated success (Lottie) is deferred: the Done step uses a
  static glyph so the template gains zero new dependencies
  (same rule as the Velopack-headless updater choice). If demand
  appears, wire `AnimatedVisualPlayer` + an embedded Lottie source
  behind the same visibility prop.

## Verification

- `dotnet test` — `SetupWizardViewModelTests` (bounds, last-step
  buttons, persist/skip).
- Smoke — `SetupWizard_Complete_Path` drives Next→Finish when the
  wizard is present (first run), then asserts Home; repeat runs
  pass vacuously (wizard already completed).
- Matrix — `nosetup` asserts the wizard files are gone; every other
  combo asserts they ship.
