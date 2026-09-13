# Discovery measurement

Measure one change at a time and record the date, query, surface, and result.
Do not optimize for fabricated numbers or claim a position without recording
the search context.

## Weekly checks

- `dotnet new search winui`
- `dotnet new search --tag MVVM`
- NuGet searches for `winui template` and `winui starter`
- Google searches for `best winui 3 template`
- GitHub search for `winui template`
- GitHub traffic, NuGet downloads, and repository stars

Record the package version and date with each observation. Search results vary
by region, account, index freshness, and personalization.

## Release and content cadence

- Keep the README and NuGet description aligned with shipped behavior.
- Publish release notes with every app or template release.
- Re-check links and the comparison table after meaningful feature changes.
- Review this measurement list monthly; do not change multiple discovery
  surfaces in the same experiment.

## Query-language notes

Use the phrases people actually search for in natural, useful sentences:
`WinUI 3 template`, `WinUI 3 starter`, `MVVM WinUI template`, and
`Windows desktop template`. Prefer precise feature descriptions over repeated
keywords, and never imply that optional tray, update, or database features are
mandatory.
