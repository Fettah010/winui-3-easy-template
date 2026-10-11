# Accessibility

> v0.7.0 Quality + accessibility RC. The contract below is enforced headlessly
> by `Tests/Services/A11yAuditTests.cs` (source-scan, same approach as the
> text-scaling audit); Narrator runs, high-contrast and 200% scaling remain
> Live-UI manual evidence, recorded honestly in the release PR.

## Audit summary (WB-02)

| Area | Verdict |
| --- | --- |
| Button/link names | Every `Button`/`HyperlinkButton` carries `Content=`, `AutomationProperties.Name=`, or code-assigned `Content` (update-popup buttons, diagnostics new-events pill — verified by scanning the matching code-behind). The tray button and its tooltip are loc-bound (`SettingsMinimizeToTray`); the toast close button is loc-bound (`UpdateClose`). |
| Input names | All 21 inputs named: toggles/combos/segmented/pips/list reuse their section or card loc keys (no new keys except `DiagnosticsViewLabel`); filter boxes reuse their placeholder keys. |
| Decorative icons | All `FontIcon`/`Image` elements are `AccessibilityView="Raw"` or named: hero/title logos, status ring + glyph (state is carried by the adjacent status *text*), feature-card icons, SettingsCard header icons, wizard done glyph, toast icon, tray-button glyph, splash logo. |
| Headings | Page titles `Level1`; section captions, feature-card titles and the About identity name `Level2`. |
| No color-only status | Home status changes text (`HomeStatusOk`/`HomeStatusError`) + glyph + brush together; the ring is decorative. |
| Splash exclusion | `SplashScreen.xaml` is out of the scan: the transient pre-localization window carries hardcoded strings by design (localization is not initialized yet; code-behind sets version/status immediately). |

## Live-region contract

| Element | Setting | Verified by |
| --- | --- | --- |
| Toast host (`NotificationHost`) | Polite | `A11yAuditTests.LiveRegions_AreWired` |
| Update status (`SettingsPage`, `UpdatePopup`) | Polite | same |
| Account status (`SettingsPage`) | Polite | same |
| Diagnostics log count | Polite | same |
| Wizard location error | Assertive | same |
| Diagnostics live-empty (invalid pattern → Assertive, empty → Polite, set in code) | Dynamic | same (code scan) |
| Toast error cards | Assertive (code, `ApplyTypeVisuals`) | same (code scan) |
| Applog tail / log viewer text | Silent by design | `TextScalingAuditTests` + viewer is selection-enabled text, not a live region |

## Keyboard map

Tab order follows declaration order on every surface (no custom `TabIndex`
anywhere — verified by scan: no `TabIndex=` in app XAML). Consequences:

- Wizard: Back is disabled on step 0 (skipped by Tab), Next/Complete are never
  both visible; validation keeps focus on the location step with the Assertive
  error rather than moving it.
- Dialogs (`ContentDialog` update flow, file pickers): native focus trap, no
  custom handling.
- NavigationView pane toggle, SettingsCards, Segmented, PipsPager: stock
  keyboard behavior, no overrides in code-behind.
- App accelerators (`Services/KeyboardShortcuts.cs`, headless-tested): Alt+Left
  / Alt+Right back-forward, Ctrl+, Settings, Escape dismisses the update flow.

## Narrator protocol (manual — deferred with reason)

No Narrator run exists yet: it needs an interactive desktop plus a human ear,
neither available to automation here. Protocol when run: default voice, scan
mode off, walk Home → Settings → Wizard → Diagnostics → About → update popup
→ tray menu; record per-surface notes (toast Polite arrival, error Assertive
interruption, heading navigation, toggle/combo naming) in `docs/DECISIONS.md`.
This section stays open until that run is filed — the static contract above is
not presented as a substitute.

## Empty / error / loading gallery (WB-04 — verified existing)

| State | Where | Proof |
| --- | --- | --- |
| Home update-error | `Pages/HomePage.xaml.cs` status block (text + glyph + brush) | code + matrix |
| Settings failure-retains-last-good | `SettingsPageViewModel.ImportFrom` refreshes UI only when import succeeds; failures toast and keep state | code + unit tests |
| Wizard validation | `SetupWizardViewModel.GoNext`/`Complete` block on invalid folder; error live-clears when fixed | code + unit tests |
| Update re-arm | `UpdateCenterViewModel.LastCheckFailed` + Retry + `CanCheck` restore | code + unit tests |
| Diagnostics empty states | `EmptyStateText` (no files / no match), `LiveEmptyText` (filter error / empty), new-events pill | code + matrix |
| Check-now busy | Settings/Home check buttons busy-disable with "Checking…" until the flow resolves | code (v0.0.14) |

## Screenshot matrix (WB-03 — B5 closure)

Required: `hc-light.png`, `hc-dark.png`, `scaling-200.png`, `empty-state.png`,
`error-state.png` at frozen `0.7.0-beta`, plus the B5 matrix record (200% × 4 HC
themes × EN/ES × 900/1920px — land what a quiet desktop allows, mark the rest
`manual`). Filed in the release PR, not claimed here.
