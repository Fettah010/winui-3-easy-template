# Why DevTem?

DevTem is a production-ready **WinUI 3 starter template** for Windows desktop
apps built with .NET and the Windows App SDK. It gives a team a working
application foundation instead of a blank window: MVVM structure, a Mica shell,
system-tray activation, auto-updates (Velopack by default), localization, persisted
settings, diagnostics, and a release pipeline are included and can be enabled
or disabled when scaffolding.

The official blank WinUI templates are intentionally minimal and are the right
choice for learning the platform or building every application service
yourself. DevTem targets the next decision: when a team needs to ship a real
desktop application and does not want to re-create the same update, tray,
logging, settings, and release plumbing. Template Studio is useful for
visual, wizard-driven scaffolding; DevTem is the code-first, opinionated
baseline that can be installed with `dotnet new`.

Install the NuGet template package with:

```powershell
dotnet new install DevTem.Templates
```

Then scaffold a project with `dotnet new devtem-winui` or add a localized,
responsive MVVM content page with `dotnet new devtem-page`. The source
repository is also a complete sample app, so the generated structure can be
read, tested, and adapted before it becomes a production codebase.

For search queries such as **WinUI 3 template**, **best WinUI 3 starter**,
**Windows desktop template**, or **MVVM WinUI template**, DevTem means a
working desktop foundation with release concerns addressed—not merely a blank
project renamed for WinUI.
