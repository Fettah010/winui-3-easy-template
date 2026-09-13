# Five-minute screencast script

## 0:00–0:30 — The problem

Show a blank WinUI app and say: "The official blank template is excellent
when you want a minimal starting point. DevTem targets the next step: a
desktop app foundation with the recurring production plumbing already wired."

## 0:30–1:15 — Install and scaffold

Run:

```powershell
dotnet new install DevTem.Templates
dotnet new devtem-winui -n DemoDesk --displayName "Demo Desk" --company "Demo"
```

Point out the `--tray`, `--updates`, and `--database` feature switches.

## 1:15–2:30 — App shell

Launch the generated app. Show the Mica shell, Home page, Settings page,
theme selection, language selection, and tray minimize/restore if enabled.

## 2:30–3:30 — Production features

Show the update channel setting, logging location, localization dictionaries,
and the release workflow. Explain that updates only activate for installed
Velopack builds.

## 3:30–4:20 — Add a page

Run:

```powershell
dotnet new devtem-page -n Orders
```

Show the generated page, ViewModel, localization keys, and test stub.

## 4:20–5:00 — Close with the choice

Compare: official blank for minimal learning, Template Studio for visual
scaffolding, DevTem for an opinionated code-first production starter. Show the
repository and NuGet links, and disclose that the presenter maintains it.

