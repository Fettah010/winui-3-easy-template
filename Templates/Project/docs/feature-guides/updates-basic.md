# Basic auto-updates (GitHub-releases checker)

This scaffold checks `https://api.github.com/repos/<owner>/<repo>/releases`
on startup and every 6 hours while running — no update SDK, no installer
framework. When a newer tag exists, the app downloads the attached Setup
`.exe` with a live progress bar, then launches it and exits.

## Release convention

Tags look like `v0.0.4` (stable) or `v0.0.4-beta` (prerelease). Attach the
installer as a **`.exe` asset** to the GitHub release — a release without
one is skipped with a log line. `stable` channel ignores prereleases;
`beta` includes them. Same-number stable supersedes prerelease
(`0.0.4` beats `0.0.4-beta`).

```powershell
# 1. Build + pack your installer however you like, then attach it:
gh release create v0.0.4 Setup.exe --title "v0.0.4"
```

## Publishing without the release pipeline

`--updates basic` scaffolds no `release.yml` and no `vpk` scripts on
purpose: you own distribution. Any static host works, as long as the
`.exe` ends up attached to the GitHub release above.

## Limits (by design)

- No delta downloads, no background apply, no install detection: checks
  run even from a dev build (handy for testing the flow end to end).
- Unauthenticated GitHub API calls are rate-limited (60/hour/IP); set
  `GITHUB_TOKEN` in the environment for 5,000/hour.
- Want full install/update automation later? Re-scaffold with
  `--updates velopack` and copy your app code over.
