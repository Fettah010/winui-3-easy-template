# Workflow — definition of done per change type

Pick the row for your change. Tiers build on each other; never skip a row
that applies. Time costs are local-machine figures.

## Verification tiers

| Tier | Commands | Cost | When |
| --- | --- | --- | --- |
| Fast | `dotnet build DevTemWinUi3.csproj -c Debug -p:Platform=x64` (0 warn/0 err) + `dotnet test Tests/` (currently 116/116) | ~1 min | Every change, no exceptions |
| Matrix | `powershell -File Scripts/test-templates.ps1` (4 combos build 0/0 + tests) | ~10 min | Any `Templates/**` change (incl. mirror edits) |
| Pack | `powershell -File Scripts/build-msix.ps1 -DryRun` (staging valid) | ~3 min | Manifest, csproj, Assets changes |
| Live UI | `dotnet test UI/…` 4/4 + screenshot in the summary | ~1 min | Any XAML / navigation / theme / Settings change |
| Workflow | `pwsh` parse + logic test with sample values (pass + reject) | ~1 min | Any `.github/workflows` change (CI only runs on tag/push) |

Always pass `-p:Platform=x64` (XAML compiler fails without it).

## Definition of done

- **Service (+ unit tests):** pure logic covered headless; OS-touching code
  (registry, pickers, toasts) stays untested behind never-throw guards,
  following `SystemTrayService` precedent. Analyzer-clean (analyzers are
  errors here: no constant arrays to methods, `Assert.Contains`, no
  nullable derefs — check `Directory.Build.props`).
- **UI/XAML:** Live UI tier. New nav items get `AutomationId`s; new strings
  go in all 3 dictionaries + `LocalizationCoverageTests`.
- **Template mirror:** every app source change lands in
  `Templates/Project/` in the same commit: verbatim where unconditioned,
  `#if (tray|updates|database)` + `DevTemWinUi3`-prefixed identifiers
  where flagged. Then `Scripts/test-mirror-parity.ps1`, then Matrix tier.
- **New test files:** scheme/identity-agnostic (use
  `ProtocolService.Scheme`, never hardcode `devtem://`) — they ship
  verbatim into renamed scaffolds; the matrix proves it with `acme://`.
- **Scripts:** `SupportsShouldProcess` for anything that writes outside
  the repo (`set-app-icon.ps1` is the model); header documents usage.
- **Workflows:** Workflow tier. Never push a tag to "test CI".
- **Docs-only:** no build needed — say so in the summary.
- **Plan files:** update the relevant roadmap checkboxes in the same commit as
  the work; `docs/STATE.md` refreshes at session end.

## Release runbook (maintainer, after green main)

```powershell
git tag v0.0.5-beta
git push origin v0.0.5-beta
# CI builds, packs, publishes to GitHub Releases (beta channel)
git branch -f beta v0.0.5-beta
git push origin beta --force
# NuGet template package (separate tag → templates-publish.yml):
git tag templates-v0.1.3
git push origin templates-v0.1.3
```

Tag name determines channel (`v*-beta` → beta, plain `v*` → stable).
Velopack versions must keep increasing — each beta bumps the patch.
Never push a tag to "test CI" (Workflow tier above covers it).

## Known fragile points (do not "fix" by retrying blindly)

- Smoke tests need a quiet desktop: UIA reads work occluded, mouse clicks
  don't. Screenshots in `TestResults/` show what the click actually hit.
- A version bump triggers the what's-new dialog (modal, blocks nav) —
  the harness dismisses it; expect it after any csproj bump.
- `dotnet test` and matrix runs must not overlap the app running
  (single-instance guard makes launches attach to the wrong window).
