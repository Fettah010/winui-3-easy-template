# awesome-winui PR draft (v1.0 — DRAFT, not submitted)

> Status: draft. The maintainer submits this with `gh`; nothing here
> auto-submits. Target: the awesome-winui (or awesome-windows) list —
> adjust the section to the list's taxonomy before sending.

## Proposed title

`Add DevTem WinUI 3 starter template`

## Proposed body

Adds DevTem WinUI 3 (`Fettah010/winui-3-easy-template`, MIT), a
production-ready WinUI 3 starter template for .NET 10, 1.0 stable:

- `dotnet new install DevTem.Templates` → `dotnet new devtem-winui`
- MVVM shell (rail/TabView), pages (list-details/datagrid/content-grid/tabbed),
  tray, Velopack updates, localization (en/es/fr), SQLite, diagnostics,
  crash reporting, release pipeline — all behind scaffold flags.
- Quality proof: 390+ headless tests, FlaUI smoke, 22-combo scaffold matrix,
  startup/weight budgets in CI. Flags frozen at 1.0.

## Suggested list entry (one line)

- [DevTem WinUI 3](https://github.com/Fettah010/winui-3-easy-template) — production-ready WinUI 3 starter template (.NET 10): MVVM shell, tray, auto-updates, localization, diagnostics, release pipeline. `dotnet new install DevTem.Templates`.

## Checklist before submitting

- [ ] v1.0.0 released on GitHub; NuGet 1.0.0 live (verify listing).
- [ ] Entry follows the target list's format (section, casing, no duplicates).
- [ ] `gh pr create` from a fork branch; link the release notes.
