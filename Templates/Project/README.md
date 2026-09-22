## DevTem-WinUI 3

A local-first Windows app built with WinUI 3 (.NET 10).

### Quick start

```powershell
dotnet build -c Debug -p:Platform=x64
dotnet run -c Debug -p:Platform=x64
```

### First steps in your copy

```powershell
# 1. Start your own version history (scaffolds inherit the template version;
#    dotnet-new runs no post-actions, so the reset is one command):
powershell -File Scripts/bump-version.ps1 -Version 0.0.1

# 2. Rebrand identity, repo links, and license:
.\Scripts\init-template.ps1 -AppName "DevTem-WinUI 3" -Company "Fettah" `
    -RepoUrl "https://github.com/Fettah010/winui-3-easy-template" -Scheme "devtem://"

# 3. Drop the sample cards you do not need (Home feature grid, About panels):
powershell -File Scripts/remove-sample-content.ps1 -WhatIf
```

What each step covers is recorded in [`docs/FEATURES.md`](docs/FEATURES.md);
page and feature workflows live in [`docs/TEMPLATE-GUIDE.md`](docs/TEMPLATE-GUIDE.md).
Template development docs (scaffold flags, release flows) stay online with
the template — this README describes your app, not the template.

### Features

- Mica window + system tray + single-instance deep links (`devtem://`)
- Auto-updates, diagnostics page, 3-language UI, persisted settings
- Logging facade, SQLite + typed HTTP data layer, crash reporting (opt-in)

### Releases

Tag a version to ship it (Velopack scaffolds):

```powershell
git tag v0.0.1-beta
git push origin v0.0.1-beta
```

### Repository layout

```
Program.cs                 # bootstrap + logging init
App.xaml(.cs)              # application entry + DI initialization
MainWindow.xaml(.cs)       # shell: title bar + navigation
Pages/                     # Home, About, Settings, Diagnostics, Setup wizard
Services/                  # updates, tray, settings, localization, logging
Controls/                  # reusable UI pieces
Tests/                     # MSTest suite
Scripts/                   # add-page, remove-sample-content, releases
```

### License

MIT — see `LICENSE`. Source: https://github.com/Fettah010/winui-3-easy-template
