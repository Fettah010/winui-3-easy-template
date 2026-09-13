# Article drafts

## 1. dev.to: WinUI 3 starter template: DevTem vs a blank app

**Query-led title:** `WinUI 3 starter template: when a blank app is not enough`

Structure:

1. State the choice: official blank app for learning/minimal apps; DevTem for
   a production-oriented desktop baseline.
2. Show the install command and a generated project.
3. Compare the two approaches in a table: shell, updates, tray, localization,
   diagnostics, persistence, and release automation.
4. Explain feature flags so optional services are not presented as mandatory.
5. Link to `docs/why-devtem.md`, the package, and the repository.
6. End with limitations and contribution links.

## 2. Hashnode: Add Velopack updates to a WinUI 3 desktop app

**Query-led title:** `WinUI 3 auto-updates with Velopack and GitHub Releases`

Structure:

1. Explain why unpackaged WinUI desktop apps need an explicit update strategy.
2. Show the release tag flow and the GitHub Actions entry point.
3. Explain channels, beta suffixes, version monotonicity, and restart behavior.
4. Show the relevant `dotnet new devtem-winui` feature flag.
5. Link back to the starter comparison and the release script.
6. Include a clear note that the article uses DevTem as the example project.

**Cross-links:** each article should link to the other, the canonical README,
the NuGet package, and `docs/why-devtem.md`.

