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

# 2b. Prove the identity is consistent (metadata vs csproj vs manifest):
.\Scripts\init-template.ps1 -Validate

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

### Starter profiles

Same template, coherent presets (override any flag individually):

```powershell
# Minimal: shell, settings, English UI
dotnet new devtem-winui -n MyApp --tray false --updates none --database false --http false --health false --logging none --crash false --localization false --tests false --attribution false

# Desktop: tray and notifications, no data services
dotnet new devtem-winui -n MyApp --updates none --database false --http false

# Production: everything on (the default)
dotnet new devtem-winui -n MyApp

# Store: packaged MSIX for Microsoft Store submission
dotnet new devtem-winui -n MyApp --distribution msix --updates store --setup false
```

Presets are documentation, not a `--profile` parameter: explicit flags
always win. Full flag reference: `dotnet new devtem-winui --help`.

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
