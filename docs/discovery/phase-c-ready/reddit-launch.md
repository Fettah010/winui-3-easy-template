# Reddit launch draft

## Title

I built DevTem, a production-ready WinUI 3 starter template for Windows desktop apps

## Body

I kept rebuilding the same desktop-app plumbing around WinUI 3, so I packaged
it into [DevTem WinUI 3](https://github.com/Fettah010/winui-3-easy-template).
It is a code-first starter for teams that need more than the official blank
WinUI app:

- MVVM with CommunityToolkit.Mvvm
- Mica shell and native title-bar behavior
- optional system tray support
- Velopack auto-updates over GitHub Releases
- runtime localization
- Serilog logging and optional Sentry diagnostics
- optional SQLite and typed HTTP services
- release automation and scaffold-time feature flags

Install it with:

```powershell
dotnet new install DevTem.Templates
dotnet new devtem-winui -n AcmeDesk --displayName "Acme Desk" --company "Acme"
```

The official blank template is still the right choice for learning WinUI or
starting from zero. DevTem is for the point where a real desktop app needs
the recurring update, settings, diagnostics, and release work already wired.

This is my own project, so I am disclosing that affiliation. Feedback on the
defaults, feature flags, and WinUI 3 integration is welcome.

## Before posting

- Add the current release/package version if useful.
- Replace the example project name only if showing a real sample.
- Follow the target subreddit self-promotion and flair rules.
- Do not claim download, star, or user numbers without current evidence.

