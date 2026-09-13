# Adds a page in one command: scaffolds devtem-page, wires strings, DI,
# route + nav, then builds and tests. Either everything lands green or the
# tree is rolled back to where it started.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\add-page.ps1 -Name Orders -Title "Order History" -Icon Shop
#
# -Name   PascalCase page name (single word recommended: Orders, not OrderHistory).
# -Title  Page title + nav label. Defaults to -Name. Drives the template's
#         --title (XAML fallback + en-US strings; es/fr land as TODO-translate).
# -Icon   Nav icon, a WinUI Symbol member (matches the template's --icon
#         choice list; keep the two in sync).
#
# Safety: refuses to run on a dirty git tree; snapshots every edited file and
# deletes every created file if any step fails. Outside a git repo (e.g. the
# template-matrix temp scaffolds) only the snapshot rollback applies.
# -RepoRoot/-TemplateSource exist for the matrix; normal use omits them.

param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
    [string]$Name,

    [string]$Title = "",

    [ValidateSet("Home", "Document", "Shop", "Mail", "Calendar", "People", "Globe", "Pictures", "Video", "Camera", "Map", "Phone")]
    [string]$Icon = "Document",

    # NOTE: $PSScriptRoot is NOT visible in parameter defaults (they evaluate
    # in the caller's scope), so path defaults resolve in the body below.
    [string]$RepoRoot = "",

    [string]$TemplateSource = ""
)

$ErrorActionPreference = "Stop"

if ($RepoRoot -eq "") { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path }

if ($Title -eq "") { $Title = $Name }
$tag = $Name.ToLowerInvariant()
if ($TemplateSource -eq "") { $TemplateSource = Join-Path $RepoRoot "Templates\Page" }
$RepoRoot = (Resolve-Path $RepoRoot).Path

# Root namespace: scaffolded apps were renamed, so generated DevTemWinUi3.*
# references are rewritten to whatever the csproj declares.
$appCsproj = Get-ChildItem -LiteralPath $RepoRoot -Filter "*.csproj" | Select-Object -First 1
if ($null -eq $appCsproj) { throw "No .csproj found in $RepoRoot." }
$csprojText = Get-Content -LiteralPath $appCsproj.FullName -Raw
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
    $text = Get-Content -LiteralPath $path -Raw
    $count = ([regex]::Matches($text, [regex]::Escape($anchor))).Count
    if ($count -ne 1) { Fail "Anchor found $count times (expected once) in $path : $anchor" }
    if ($Before) {
        $text = $text.Replace($anchor, $insert + $anchor)
    }
    else {
        $text = $text.Replace($anchor, $anchor + $insert)
    }
    Set-Content -LiteralPath $path -Value $text -NoNewline
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
    Write-Host "`n==> install devtem-page template" -ForegroundColor Cyan
    # Scaffolded apps have no Templates\Page on disk (only the dormant nested
    # copy): skip the local install and use whatever devtem-page is installed
    # (e.g. from the DevTem.Templates NuGet package). The scaffold below fails
    # loudly if no devtem-page is installed at all.
    if (Test-Path -LiteralPath $TemplateSource) {
        # Reinstall for hermeticity (an older devtem-page may be installed).
        # Uninstalls fail when nothing is installed - expected, ignored.
        try { & dotnet new uninstall "$TemplateSource" 2>&1 | Out-Null } catch { }
        & dotnet new install "$TemplateSource" 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { Fail "dotnet new install failed for $TemplateSource" }
    }
    else {
        Write-Host "No template at $TemplateSource; using the installed devtem-page." -ForegroundColor Yellow
    }

    $scaffoldDir = Join-Path ([System.IO.Path]::GetTempPath()) ("add-page-scaffold-" + [System.Guid]::NewGuid().ToString("N"))
    Write-Host "`n==> scaffold $Name ($Title, $Icon)" -ForegroundColor Cyan
    & dotnet new devtem-page -n $Name --title $Title --icon $Icon --output $scaffoldDir 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "dotnet new devtem-page failed" }

    # Consume the strings snippet: the single source of truth for the three
    # dictionaries (en-US real, es/fr TODO-translate).
    $snippetPath = Join-Path $scaffoldDir "$($Name)Page.strings.md"
    if (-not (Test-Path -LiteralPath $snippetPath)) { Fail "Strings snippet missing: $snippetPath" }
    $snippet = Get-Content -LiteralPath $snippetPath -Raw
    $sections = @{}
    foreach ($m in [regex]::Matches($snippet, "## (en-US|es-ES|fr-FR)\r?\n((?:\[.*\r?\n)+)")) {
        $pairs = @{}
        foreach ($p in [regex]::Matches($m.Groups[2].Value, '\["(\w+)"\] = "(.*)",')) {
            $pairs[$p.Groups[1].Value] = $p.Groups[2].Value
        }
        $sections[$m.Groups[1].Value] = $pairs
    }
    $wantKeys = @("Nav$Name", "$($Name)Title", "$($Name)Description")
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
        $content = Get-Content -LiteralPath $from -Raw
        if ($rootNs -ne "DevTemWinUi3") {
            $content = $content.Replace("DevTemWinUi3", $rootNs)
        }
        Set-Content -LiteralPath $to -Value $content -NoNewline
        $createdFiles += $to
    }
    Remove-Item -LiteralPath $scaffoldDir -Recurse -Force

    # Strings: one insert per language dict, right after the NavSettings line
    # (dicts are declared en, es, fr - in that order).
    Write-Host "`n==> insert strings (3 languages)" -ForegroundColor Cyan
    $locPath = Join-Path $RepoRoot "Services\LocalizationService.cs"
    Snapshot-File $locPath
    $locText = Get-Content -LiteralPath $locPath -Raw
    $anchors = [regex]::Matches($locText, '(?m)^(\s*)\["NavSettings"\] = "[^"]*",\s*$')
    if ($anchors.Count -ne 3) { Fail "NavSettings anchor found $($anchors.Count) times (expected 3) in LocalizationService.cs" }
    $langs = @("en-US", "es-ES", "fr-FR")
    $locLines = $locText -split "`r?`n"
    $offset = 0
    for ($i = 0; $i -lt 3; $i++) {
        $lineIdx = -1
        $seen = -1
        for ($l = 0; $l -lt $locLines.Count; $l++) {
            if ($locLines[$l] -match '^\s*\["NavSettings"\] = "[^"]*",\s*$') {
                $seen++
                if ($seen -eq $i) { $lineIdx = $l; break }
            }
        }
        if ($lineIdx -lt 0) { Fail "NavSettings anchor #$i vanished in LocalizationService.cs" }
        $indent = $anchors[$i].Groups[1].Value
        $lang = $langs[$i]
        $insert = @(
            $indent,
            ('{0}["Nav{1}"] = "{2}",' -f $indent, $Name, (Escape-CSharp $sections[$lang]["Nav$Name"])),
            ('{0}["{1}Title"] = "{2}",' -f $indent, $Name, (Escape-CSharp $sections[$lang]["$($Name)Title"])),
            ('{0}["{1}Description"] = "{2}",' -f $indent, $Name, (Escape-CSharp $sections[$lang]["$($Name)Description"]))
        )
        $before = $locLines[0..($lineIdx + $offset)]
        $after = $locLines[(($lineIdx + $offset + 1))..($locLines.Count - 1)]
        $locLines = @($before) + @($insert) + @($after)
        $offset += 4
    }
    Set-Content -LiteralPath $locPath -Value ($locLines -join "`r`n") -NoNewline

    # DI registration.
    Write-Host "`n==> register ViewModel" -ForegroundColor Cyan
    $slPath = Join-Path $RepoRoot "Services\ServiceLocator.cs"
    Snapshot-File $slPath
    Insert-Unique $slPath "services.AddTransient<ViewModels.SettingsPageViewModel>();" "`r`n            services.AddTransient<ViewModels.$($Name)PageViewModel>();"

    # Route + nav item + nav label.
    Write-Host "`n==> wire route + nav" -ForegroundColor Cyan
    $mwPath = Join-Path $RepoRoot "MainWindow.xaml.cs"
    Snapshot-File $mwPath
    Insert-Unique $mwPath '_nav.RegisterRoute("home", typeof(HomePage));' "`r`n        _nav.RegisterRoute(`"$tag`", typeof($($Name)Page));"
    Insert-Unique $mwPath 'NavHomeItem.Content = loc.GetString("NavHome");' "`r`n            Nav$($Name)Item.Content = loc.GetString(`"Nav$Name`");"

    $xamlPath = Join-Path $RepoRoot "MainWindow.xaml"
    Snapshot-File $xamlPath
    $navItem = "`r`n                <NavigationViewItem x:Name=`"Nav$($Name)Item`" AutomationProperties.AutomationId=`"Nav$($Name)Item`" Content=`"$(Escape-Xml $Title)`" Tag=`"$tag`">`r`n                    <NavigationViewItem.Icon>`r`n                        <SymbolIcon Symbol=`"$Icon`"/>`r`n                    </NavigationViewItem.Icon>`r`n                </NavigationViewItem>`r`n            "
    Insert-Unique $xamlPath "</NavigationView.MenuItems>" $navItem -Before

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
