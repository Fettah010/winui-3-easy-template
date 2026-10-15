# Marketplace post draft (v1.0 — DRAFT, unpublished)

> Status: draft. The maintainer posts this; nothing here auto-publishes.
> Every claim traces to a shipped gate (see `docs/DECISIONS.md`).

## Title

DevTem WinUI 3 — production-ready starter template, 1.0 stable

## Body

DevTem WinUI 3 1.0 is a starter template for real WinUI 3 desktop apps on
.NET 10 — not a blank window, but a working foundation you rename and ship:

- Mica shell (rail + TabView + MenuBar-command patterns), responsive pages
  (blank, list-details, datagrid, content-grid, tabbed), settings with theme,
  3-language runtime localization, setup wizard.
- Shipping plumbing: Velopack auto-updates (plus basic/none/AppInstaller/Store
  per distribution), system tray + autostart, single instance, `devtem://`
  deep links, SQLite data layer, logging facade + Diagnostics page, opt-in
  Sentry crash reporting, release pipeline (GitHub + Store submit).
- Proven, not promised: 390+ headless tests, FlaUI smoke suite, 22-combo
  scaffold matrix on every push, mirror-parity guard, startup + weight budgets
  enforced in CI. Template flags frozen at 1.0 (new options arrive as new
  values, never renames).

Install: `dotnet new install DevTem.Templates`, then
`dotnet new devtem-winui -n AcmeDesk`. Start at `docs/GETTING-STARTED.md`
(15 minutes to a renamed, running app). Full comparison:
`docs/why-devtem.md`. Source: `Fettah010/winui-3-easy-template` (MIT).

## Checklist before posting

- [ ] v1.0.0 tag + `stable` move pushed; NuGet 1.0.0 listing verified.
- [ ] README screenshots + tour strip current (they are, at 1.0.0).
- [ ] Links above resolve (repo, NuGet package, GETTING-STARTED).
