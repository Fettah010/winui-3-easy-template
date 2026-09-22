# Adds a page in one command: scaffolds devtem-page (or devtem-list-details
# with -Kind list), wires strings, DI, route + nav, then builds and tests.
# Either everything lands green or the tree is rolled back to where it started.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\add-page.ps1 -Name Orders -Route orders -Title "Order History" -Icon Shop
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\add-page.ps1 -Kind list -Name Products -Route products -Title "Products" -Icon Shop
#
# -Kind  page (hero-card content) or list (list/details with selection +
#         empty state). Default page.
# -Name   PascalCase page name (single word recommended: Orders, not OrderHistory).
# -Title  Page title + nav label. Defaults to -Name. Seeds the en-US strings
#         (XAML binds live via {loc:Loc}; es/fr land as TODO-translate).
# -Route  Lowercase route segment; defaults to the lowercased page name.
# -Icon   Nav icon, a WinUI Symbol member (matches the template's --icon
#         choice list; keep the two in sync).
#
# Safety: refuses to run on a dirty git tree; snapshots every edited file and
# deletes every created file if any step fails. Outside a git repo (e.g. the
# template-matrix temp scaffolds) only the snapshot rollback applies.
# -RepoRoot/-TemplateSource exist for the matrix; normal use omits them.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
    [string]$Name,

    [string]$Title = "",

    [ValidatePattern('^[a-z][a-z0-9-]*$')]
    [string]$Route = "",

    [ValidateSet("Home", "Document", "Shop", "Mail", "Calendar", "People", "Globe", "Pictures", "Video", "Camera", "Map", "Phone")]
    [string]$Icon = "Document",

    [ValidateSet("page", "list")]
    [string]$Kind = "page",

    # NOTE: $PSScriptRoot is NOT visible in parameter defaults (they evaluate
    # in the caller's scope), so path defaults resolve in the body below.
    [string]$RepoRoot = "",

    [string]$TemplateSource = ""
)

$ErrorActionPreference = "Stop"

if ($RepoRoot -eq "") { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path }

if ($Title -eq "") { $Title = $Name }
$tag = $Name.ToLowerInvariant()
if ($Route -eq "") { $Route = $tag }
$shortName = if ($Kind -eq "list") { "devtem-list-details" } else { "devtem-page" }
$defaultTemplateDir = if ($Kind -eq "list") { "Templates\ListDetails" } else { "Templates\Page" }
if ($TemplateSource -eq "") { $TemplateSource = Join-Path $RepoRoot $defaultTemplateDir }
$RepoRoot = (Resolve-Path $RepoRoot).Path

if ($WhatIfPreference) {
    Write-Host "WhatIf: would add $Kind page '$Name' with route '$Route' and icon '$Icon' to $RepoRoot."
    Write-Host "WhatIf: would create page, ViewModel, test, localization entries, DI registration, route, and navigation item."
    return
}

# Root namespace: scaffolded apps were renamed, so generated DevTemWinUi3.*
# references are rewritten to whatever the csproj declares.
$appCsproj = Get-ChildItem -LiteralPath $RepoRoot -Filter "*.csproj" | Select-Object -First 1
if ($null -eq $appCsproj) { throw "No .csproj found in $RepoRoot." }
$csprojText = [System.IO.File]::ReadAllText($appCsproj.FullName, [System.Text.Encoding]::UTF8)
$nsMatch = [regex]::Match($csprojText, "<RootNamespace>([^<]+)</RootNamespace>")
$rootNs = if ($nsMatch.Success) { $nsMatch.Groups[1].Value } else { [System.IO.Path]::GetFileNameWithoutExtension($appCsproj.Name) }
Write-Host "Root namespace: $rootNs"

$createdFiles = @()
$snapshots = @{}
$snapshotDir = Join-Path ([System.IO.Path]::GetTempPath()) ("add-page-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $snapshotDir | Out-Null

function Restore-Tree {
    foreach ($path in $createdFiles) {
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
    foreach ($entry in $snapshots.GetEnumerator()) {
        Copy-Item -LiteralPath $entry.Value -Destination $entry.Key -Force
    }
    if (Test-Path -LiteralPath $snapshotDir) { Remove-Item -LiteralPath $snapshotDir -Recurse -Force }
}

function Fail([string]$message) {
    Write-Host "FAILED: $message -- rolling back." -ForegroundColor Red
    Restore-Tree
    throw $message
}

function Snapshot-File([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Anchor file missing: $path" }
    $copy = Join-Path $snapshotDir ([System.Guid]::NewGuid().ToString("N") + ".bak")
    Copy-Item -LiteralPath $path -Destination $copy
    $snapshots[$path] = $copy
}

function Insert-Unique([string]$path, [string]$anchor, [string]$insert, [switch]$Before) {
    # .NET UTF-8 IO (not Get/Set-Content): the 5.1 cmdlets round-trip
    # through the system codepage and corrupt non-ASCII (see
    # bump-version.ps1: the same disease grew a csproj to 148MB).
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $count = ([regex]::Matches($text, [regex]::Escape($anchor))).Count
    if ($count -ne 1) { Fail "Anchor found $count times (expected once) in $path : $anchor" }
    if ($Before) {
        $text = $text.Replace($anchor, $insert + $anchor)
    }
    else {
        $text = $text.Replace($anchor, $anchor + $insert)
    }
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

function Assert-RouteAvailable([string]$path, [string]$route) {
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    if (($text -match [regex]::Escape("RegisterRoute(`"$route`"")) -or ($text -match [regex]::Escape("Tag=`"$route`""))) {
        Fail "Route '$route' is already registered in $path. Choose a unique route; no duplicate registration was made."
    }
}

function Escape-CSharp([string]$value) {
    return $value.Replace("\", "\\").Replace('"', '\"')
}

function Escape-Xml([string]$value) {
    return $value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace('"', "&quot;")
}

# Refuse to touch uncommitted work: rollback restores files, not edits.
$inGitRepo = $false
try {
    Push-Location $RepoRoot
    try {
        & git rev-parse --is-inside-work-tree 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $inGitRepo = $true
            # Untracked files cannot collide with rollback (created targets
            # fail fast on "already exists"), so only tracked edits block.
            $dirty = & git status --porcelain | Where-Object { $_ -notmatch '^\?\?' }
            if ($dirty.Count -gt 0) {
                throw "Working tree is dirty. Commit or stash first; add-page only runs on a clean tree."
            }
        }
        else {
            Write-Host "Not a git repo: snapshot rollback only (no clean-tree guard)." -ForegroundColor Yellow
        }
    }
    finally { Pop-Location }
}
catch {
    if ($inGitRepo) { throw }
    Write-Host "Git unavailable: snapshot rollback only (no clean-tree guard)." -ForegroundColor Yellow
}

try {
    $activationDir = ""
    Write-Host "`n==> install devtem-page template" -ForegroundColor Cyan
    # Scaffolded apps have no Templates\Page on disk (only the dormant nested
    # copy): skip the local install and use whatever devtem-page is installed
    # (e.g. from the DevTem.Templates NuGet package). The scaffold below fails
    # loudly if no devtem-page is installed at all.
    if (Test-Path -LiteralPath $TemplateSource) {
        # The nested page template ships dormant in scaffolded apps
        # (Templates\Page\config.hold\template.json.hold, so installing the
        # project package does not register a stale global devtem-page).
        # Activating it in place would dirty the tree the guard above just
        # approved, so activate a temp copy and install that instead —
        # the repo stays clean.
        $activeConfig = Join-Path $TemplateSource ".template.config\template.json"
        $dormantConfig = Join-Path $TemplateSource "config.hold\template.json.hold"
        $installSource = $TemplateSource
        if ((-not (Test-Path -LiteralPath $activeConfig)) -and (Test-Path -LiteralPath $dormantConfig)) {
            $activationDir = Join-Path ([System.IO.Path]::GetTempPath()) ("add-page-template-" + [System.Guid]::NewGuid().ToString("N"))
            Copy-Item -LiteralPath $TemplateSource -Destination $activationDir -Recurse -Force
            $holdDir = Join-Path $activationDir "config.hold"
            $liveDir = Join-Path $activationDir ".template.config"
            Move-Item -LiteralPath $holdDir -Destination $liveDir -Force
            Move-Item -LiteralPath (Join-Path $liveDir "template.json.hold") -Destination (Join-Path $liveDir "template.json") -Force
            $installSource = $activationDir
            Write-Host "Activated dormant page template via temp copy." -ForegroundColor Yellow
        }
        # Reinstall for hermeticity (an older item template may be installed).
        # Uninstalls fail when nothing is installed - expected, ignored.
        # NOTE: uninstall/install by path only touches that path's entry. A
        # stale item template installed from ANOTHER path (older checkout,
        # previous NuGet) keeps shadowing short-name resolution and fails
        # the scaffold below with "Invalid option(s)" (e.g. no --route) —
        # the probe after install catches exactly that.
        try { & dotnet new uninstall "$installSource" 2>&1 | Out-Null } catch { }
        & dotnet new install "$installSource" 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { Fail "dotnet new install failed for $installSource" }

        # Probe the RESOLVED template (not just the installed path): the
        # scaffold must accept --route. A stale global install from another
        # path shadows the fresh one here — fail loudly with remediation
        # instead of dying at the scaffold step.
        $helpText = (& dotnet new $shortName --help 2>&1 | Out-String)
        if ($helpText -notmatch "--route") {
            Fail "The resolved $shortName has no --route option (a stale install from another path is shadowing $installSource). Run 'dotnet new uninstall' with no args to list installs, uninstall every stale $shortName entry, then retry."
        }
        # The temp activation served its purpose at install time (the engine
        # snapshots content on install): uninstall it now so no dangling
        # entry points at a deleted temp dir, then delete the dir.
        if ($activationDir -ne "") {
            try { & dotnet new uninstall "$activationDir" 2>&1 | Out-Null } catch { }
            Remove-Item -LiteralPath $activationDir -Recurse -Force
            $activationDir = ""
        }
    }
    else {
        Write-Host "No template at $TemplateSource; using the installed devtem-page." -ForegroundColor Yellow
    }

    $scaffoldDir = Join-Path ([System.IO.Path]::GetTempPath()) ("add-page-scaffold-" + [System.Guid]::NewGuid().ToString("N"))
    Write-Host "`n==> scaffold $Name ($Kind, $Title, $Icon)" -ForegroundColor Cyan
    & dotnet new $shortName -n $Name --route $Route --title $Title --icon $Icon --output $scaffoldDir 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "dotnet new $shortName failed" }

    # Consume the strings snippet: the single source of truth for the three
    # dictionaries (en-US real, es/fr TODO-translate).
    $snippetPath = Join-Path $scaffoldDir "$($Name)Page.strings.md"
    if (-not (Test-Path -LiteralPath $snippetPath)) { Fail "Strings snippet missing: $snippetPath" }
    $snippet = [System.IO.File]::ReadAllText($snippetPath, [System.Text.Encoding]::UTF8)
    $sections = @{}
    foreach ($m in [regex]::Matches($snippet, "## (en-US|es-ES|fr-FR)\r?\n((?:\[.*\r?\n)+)")) {
        $pairs = @{}
        foreach ($p in [regex]::Matches($m.Groups[2].Value, '\["(\w+)"\] = "(.*)",')) {
            $pairs[$p.Groups[1].Value] = $p.Groups[2].Value
        }
        $sections[$m.Groups[1].Value] = $pairs
    }
    $wantKeys = @("Nav$Name", "$($Name)Title", "$($Name)Description")
    if ($Kind -eq "list") {
        # List/details pages carry their empty-state + select-prompt labels.
        $wantKeys += @("$($Name)EmptyTitle", "$($Name)EmptyMessage", "$($Name)SelectPrompt")
    }
    foreach ($lang in @("en-US", "es-ES", "fr-FR")) {
        if (-not $sections.ContainsKey($lang)) { Fail "Snippet has no $lang section" }
        foreach ($key in $wantKeys) {
            if (-not $sections[$lang].ContainsKey($key)) { Fail "Snippet lacks $key in $lang" }
        }
    }

    # Copy the four generated files, rewriting the template namespace for
    # scaffolded (renamed) apps. No-op on this repo's own namespace.
    Write-Host "`n==> copy generated files" -ForegroundColor Cyan
    $fileMap = @{
        "Pages\$($Name)Page.xaml" = "Pages\$($Name)Page.xaml"
        "Pages\$($Name)Page.xaml.cs" = "Pages\$($Name)Page.xaml.cs"
        "ViewModels\$($Name)PageViewModel.cs" = "ViewModels\$($Name)PageViewModel.cs"
        "Tests\ViewModels\$($Name)PageViewModelTests.cs" = "Tests\ViewModels\$($Name)PageViewModelTests.cs"
    }
    foreach ($entry in $fileMap.GetEnumerator()) {
        $from = Join-Path $scaffoldDir $entry.Key
        $to = Join-Path $RepoRoot $entry.Value
        if (-not (Test-Path -LiteralPath $from)) { Fail "Scaffolded file missing: $($entry.Key)" }
        if (Test-Path -LiteralPath $to) { Fail "Target already exists: $($entry.Value) (page already added?)" }
        $dir = Split-Path $to -Parent
        if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
        $content = [System.IO.File]::ReadAllText($from, [System.Text.Encoding]::UTF8)
        if ($rootNs -ne "DevTemWinUi3") {
            $content = $content.Replace("DevTemWinUi3", $rootNs)
        }
        [System.IO.File]::WriteAllText($to, $content, (New-Object System.Text.UTF8Encoding($false)))
        $createdFiles += $to
    }
    Remove-Item -LiteralPath $scaffoldDir -Recurse -Force

    # Strings: one insert per language file, right after the NavSettings line
    # (Services/Localization/{En,Es,Fr}Strings.cs, in that order). .NET
    # UTF-8 IO (no BOM): PowerShell 5.1 Get/Set-Content would round-trip
    # through the system codepage and only survive by accident.
    Write-Host "`n==> insert strings (3 languages)" -ForegroundColor Cyan
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $locFiles = @(
        @{ Path = Join-Path $RepoRoot "Services\Localization\EnStrings.cs"; Lang = "en-US" },
        @{ Path = Join-Path $RepoRoot "Services\Localization\EsStrings.cs"; Lang = "es-ES" },
        @{ Path = Join-Path $RepoRoot "Services\Localization\FrStrings.cs"; Lang = "fr-FR" }
    )
    foreach ($entry in $locFiles) {
        Snapshot-File $entry.Path
        $text = [System.IO.File]::ReadAllText($entry.Path, [System.Text.Encoding]::UTF8)
        $m = [regex]::Match($text, '(?m)^(\s*)\["NavSettings"\] = "[^"]*",\s*$')
        if (-not $m.Success) { Fail "NavSettings anchor missing in $($entry.Path)" }
        $indent = $m.Groups[1].Value
        $lang = $entry.Lang
        $insertLines = foreach ($key in $wantKeys) {
            '{0}["{1}"] = "{2}",' -f $indent, $key, (Escape-CSharp $sections[$lang][$key])
        }
        $insert = "`r`n" + ($insertLines -join "`r`n")
        $anchorLine = $m.Value
        $idx = $text.IndexOf($anchorLine, [System.StringComparison]::Ordinal)
        $newText = $text.Substring(0, $idx + $anchorLine.Length) + $insert + $text.Substring($idx + $anchorLine.Length)
        [System.IO.File]::WriteAllText($entry.Path, $newText, $utf8NoBom)
    }

    # DI registration.
    Write-Host "`n==> register ViewModel" -ForegroundColor Cyan
    $slPath = Join-Path $RepoRoot "Services\ServiceLocator.cs"
    Snapshot-File $slPath
    Insert-Unique $slPath "services.AddTransient<ViewModels.SettingsPageViewModel>();" "`r`n            services.AddTransient<ViewModels.$($Name)PageViewModel>();"

    # Route + nav item. The label needs no code: nav items bind
    # (Content="{loc:Loc Key=Nav<Name>}"), so only the XAML item is added.
    Write-Host "`n==> wire route + nav" -ForegroundColor Cyan
    $mwPath = Join-Path $RepoRoot "MainWindow.xaml.cs"
    Snapshot-File $mwPath
    Assert-RouteAvailable $mwPath $Route
    Insert-Unique $mwPath '_nav.RegisterRoute("home", typeof(HomePage));' "`r`n        _nav.RegisterRoute(`"$Route`", typeof($($Name)Page));"

    $xamlPath = Join-Path $RepoRoot "MainWindow.xaml"
    Snapshot-File $xamlPath
    $navItem = "`r`n                <NavigationViewItem AutomationProperties.AutomationId=`"Nav$($Name)Item`" Content=`"{loc:Loc Key=Nav$Name}`" Tag=`"$Route`">`r`n                    <NavigationViewItem.Icon>`r`n                        <SymbolIcon Symbol=`"$Icon`"/>`r`n                    </NavigationViewItem.Icon>`r`n                </NavigationViewItem>`r`n            "
    # Stock shell anchor. Restyled shells (custom rail, no NavigationView)
    # fall back to the documented <devtem:nav-items> end marker: after any
    # shell restyle, keep one of the two anchors, or add-page cannot wire
    # navigation (see docs/TEMPLATE-GUIDE.md, nav shell contract).
    $xamlText = [System.IO.File]::ReadAllText($xamlPath, [System.Text.Encoding]::UTF8)
    $menuAnchorCount = ([regex]::Matches($xamlText, [regex]::Escape("</NavigationView.MenuItems>"))).Count
    if ($menuAnchorCount -eq 1) {
        Insert-Unique $xamlPath "</NavigationView.MenuItems>" $navItem -Before
    }
    elseif (([regex]::Matches($xamlText, [regex]::Escape("<!-- </devtem:nav-items> -->"))).Count -eq 1) {
        Insert-Unique $xamlPath "<!-- </devtem:nav-items> -->" $navItem -Before
    }
    else {
        Fail "No nav anchor found in $xamlPath (expected </NavigationView.MenuItems> or <!-- </devtem:nav-items> --> exactly once). Restyle kept neither anchor."
    }

    # Prove it: full build (warnings are errors) + full test suite.
    Write-Host "`n==> build (0 warnings)" -ForegroundColor Cyan
    Push-Location $RepoRoot
    try {
        # Bare `dotnet build` in a multi-project root only builds one project;
        # the solution (whatever the scaffold named it) builds everything.
        $sln = Get-ChildItem -LiteralPath $RepoRoot -Filter "*.sln" | Select-Object -First 1
        if ($null -eq $sln) { Fail "No .sln found in $RepoRoot" }
        & dotnet build $sln.FullName -c Debug -p:Platform=x64 2>&1 | Tee-Object -Variable buildOut | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host ($buildOut | Select-Object -Last 12)
            Fail "dotnet build failed"
        }
        Write-Host "`n==> test" -ForegroundColor Cyan
        $testProj = Get-ChildItem -LiteralPath (Join-Path $RepoRoot "Tests") -Filter "*.Tests.csproj" | Select-Object -First 1
        if ($null -eq $testProj) { Fail "No Tests/*.Tests.csproj found" }
        # No --no-build: the solution build places the test dll under
        # bin\Debug (Any CPU) while `dotnet test -p:Platform=x64` looks under
        # bin\x64; letting test build itself always runs a fresh binary.
        & dotnet test $testProj.FullName -c Debug -p:Platform=x64 --nologo -v q 2>&1 | Tee-Object -Variable testOut | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host ($testOut | Select-Object -Last 12)
            Fail "dotnet test failed"
        }
    }
    finally { Pop-Location }

    Remove-Item -LiteralPath $snapshotDir -Recurse -Force
    Write-Host "`nPage $Name added and green." -ForegroundColor Green
    Write-Host "Next: replace the TODO-translate markers (grep TODO-translate), run the app, check the new nav item."
}
catch {
    if ($_.Exception.Message -notmatch "Working tree is dirty") {
        # A temp activation that never reached its uninstall leaves a
        # dangling engine entry: remove it before rolling back files.
        try {
            if (($activationDir -ne "") -and (Test-Path -LiteralPath $activationDir)) {
                & dotnet new uninstall "$activationDir" 2>&1 | Out-Null
                Remove-Item -LiteralPath $activationDir -Recurse -Force
            }
        }
        catch { }
        # Fail() already rolled back; anything else (guard, dotnet) leaves
        # either nothing (pre-copy) or snapshots behind - restore + rethrow.
        if ((Test-Path -LiteralPath $snapshotDir) -and ($createdFiles.Count -gt 0 -or $snapshots.Count -gt 0)) {
            Write-Host "Rolling back." -ForegroundColor Red
            Restore-Tree
        }
        elseif (Test-Path -LiteralPath $snapshotDir) {
            Remove-Item -LiteralPath $snapshotDir -Recurse -Force
        }
    }
    throw
}
