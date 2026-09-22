# Removes the optional sample-content blocks from the app (the inverse of
# Scripts/add-page.ps1): cuts the XAML ranges, their code-behind companions,
# and the localization keys in all three dictionaries, then builds + tests.
# Either everything lands green or the tree is rolled back to where it started.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\remove-sample-content.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\remove-sample-content.ps1 -Blocks home-features,about-links
#   powershell -File Scripts\remove-sample-content.ps1 -WhatIf   # preview only
#
# -Blocks  Sample blocks to remove (default: home-features, about-tech,
#          about-links). Valid values mirror docs/TEMPLATE-GUIDE.md section 2d:
#          home-features  Home feature-cards grid (+ FeatureCol1/FeatureCard2-4
#                         handling in HomePage.xaml.cs + HomeFeat* loc keys).
#          about-tech     About technology panel: two XAML ranges (column
#                         definition + panel, so the app-info column survives
#                         as a valid single-column grid) + TechCol/TechPanel
#                         lines in AboutPage.xaml.cs + AboutCardUpdates/
#                         Logging/Mvvm/Ui* keys.
#          about-links    About links panel: two XAML ranges (column
#                         definition + panel, so the license column survives)
#                         + LinksCol/LinksPanel/links-button lines in
#                         AboutPage.xaml.cs + AboutCardSource/Releases/
#                         Issues* keys.
#
# Safety: refuses to run on a dirty git tree; snapshots every edited file and
# restores the tree if any step fails. Outside a git repo only the snapshot
# rollback applies. -RepoRoot exists for the template matrix; normal use omits it.

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string[]]$Blocks = @("home-features", "about-tech", "about-links"),

    # NOTE: $PSScriptRoot is NOT visible in parameter defaults (they evaluate
    # in the caller's scope), so path defaults resolve in the body below.
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

$validBlocks = @("home-features", "about-tech", "about-links")
foreach ($b in $Blocks) {
    if ($validBlocks -notcontains $b) {
        throw "Unknown block '$b'. Valid values: $($validBlocks -join ', ')."
    }
}
$Blocks = @($Blocks | Select-Object -Unique)

if ($RepoRoot -eq "") { $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path }
$RepoRoot = (Resolve-Path $RepoRoot).Path

if ($WhatIfPreference) {
    Write-Host "WhatIf: would remove sample blocks ($($Blocks -join ', ')) from $RepoRoot."
    Write-Host "WhatIf: would cut XAML ranges, code-behind companions, and loc keys (3 languages), then build + test."
    return
}

$snapshots = @{}
$snapshotDir = Join-Path ([System.IO.Path]::GetTempPath()) ("remove-sample-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $snapshotDir | Out-Null

function Restore-Tree {
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
    if ($snapshots.ContainsKey($path)) { return }
    $copy = Join-Path $snapshotDir ([System.Guid]::NewGuid().ToString("N") + ".bak")
    Copy-Item -LiteralPath $path -Destination $copy
    $snapshots[$path] = $copy
}

function Read-Utf8([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function Write-Utf8NoBom([string]$path, [string]$text) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

function Remove-XamlBlock([string]$path, [string]$id) {
    # One block id may own SEVERAL ranges (about-tech/about-links each wrap
    # the column definition and the panel separately, so the surviving
    # column keeps a valid single-column grid): remove every range.
    $text = Read-Utf8 $path
    $pattern = "(?s)[ \t]*<!--[ \t]*devtem:optional:$id\b.*?<!--[ \t]*devtem:end:$id\b.*?-->[ \t]*\r?\n?"
    $count = ([regex]::Matches($text, $pattern)).Count
    if ($count -lt 1) { Fail "XAML block '$id' markers not found in $path" }
    $newText = [regex]::Replace($text, $pattern, "")
    # Collapse 3+ blank lines left by the cuts back to two.
    $newText = [regex]::Replace($newText, "(\r?\n){3,}", "`r`n`r`n")
    Write-Utf8NoBom $path $newText
    Write-Host ("  cut {0} range(s) for '{1}'" -f $count, $id)
}

function Remove-LinesMatching([string]$path, [string[]]$patterns, [string]$label) {
    $text = Read-Utf8 $path
    $lines = $text -split "\r?\n"
    $removed = 0
    $kept = @()
    foreach ($line in $lines) {
        $drop = $false
        foreach ($pat in $patterns) {
            if ($line -match $pat) { $drop = $true; break }
        }
        if ($drop) { $removed++ } else { $kept += $line }
    }
    if ($removed -eq 0) { Fail "No $label lines matched in $path" }
    Write-Utf8NoBom $path ($kept -join "`r`n")
    return $removed
}

# Refuse to touch uncommitted work: rollback restores files, not edits.
$inGitRepo = $false
try {
    Push-Location $RepoRoot
    try {
        & git rev-parse --is-inside-work-tree 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $inGitRepo = $true
            $dirty = & git status --porcelain | Where-Object { $_ -notmatch '^\?\?' }
            if ($dirty.Count -gt 0) {
                throw "Working tree is dirty. Commit or stash first; remove-sample-content only runs on a clean tree."
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
    $homeXaml = Join-Path $RepoRoot "Pages\HomePage.xaml"
    $homeCs = Join-Path $RepoRoot "Pages\HomePage.xaml.cs"
    $aboutXaml = Join-Path $RepoRoot "Pages\AboutPage.xaml"
    $aboutCs = Join-Path $RepoRoot "Pages\AboutPage.xaml.cs"
    $enPath = Join-Path $RepoRoot "Services\Localization\EnStrings.cs"
    $esPath = Join-Path $RepoRoot "Services\Localization\EsStrings.cs"
    $frPath = Join-Path $RepoRoot "Services\Localization\FrStrings.cs"

    if ($Blocks -contains "home-features") {
        Write-Host "`n==> remove home-features" -ForegroundColor Cyan
        Snapshot-File $homeXaml; Snapshot-File $homeCs
        Snapshot-File $enPath; Snapshot-File $esPath; Snapshot-File $frPath
        Remove-XamlBlock $homeXaml "home-features"
        Remove-LinesMatching $homeCs @("FeatureCol1", "FeatureCard2", "FeatureCard3", "FeatureCard4") "home-features code-behind" | Out-Null
        foreach ($loc in @($enPath, $esPath, $frPath)) {
            Remove-LinesMatching $loc @('\["HomeFeat') "HomeFeat loc" | Out-Null
        }
    }

    if ($Blocks -contains "about-tech") {
        Write-Host "`n==> remove about-tech" -ForegroundColor Cyan
        Snapshot-File $aboutXaml; Snapshot-File $aboutCs
        Snapshot-File $enPath; Snapshot-File $esPath; Snapshot-File $frPath
        Remove-XamlBlock $aboutXaml "about-tech"
        Remove-LinesMatching $aboutCs @("TechCol", "TechPanel") "about-tech code-behind" | Out-Null
        foreach ($loc in @($enPath, $esPath, $frPath)) {
            Remove-LinesMatching $loc @('\["AboutCardUpdates', '\["AboutCardLogging', '\["AboutCardMvvm', '\["AboutCardUi') "about-tech loc" | Out-Null
        }
    }

    if ($Blocks -contains "about-links") {
        Write-Host "`n==> remove about-links" -ForegroundColor Cyan
        Snapshot-File $aboutXaml; Snapshot-File $aboutCs
        Snapshot-File $enPath; Snapshot-File $esPath; Snapshot-File $frPath
        Remove-XamlBlock $aboutXaml "about-links"
        Remove-LinesMatching $aboutCs @("LinksCol", "LinksPanel", "SourceButton", "ViewReleasesButton", "OpenIssueButton") "about-links code-behind" | Out-Null
        foreach ($loc in @($enPath, $esPath, $frPath)) {
            Remove-LinesMatching $loc @('\["AboutCardSource', '\["AboutCardReleases', '\["AboutCardIssues', '\["AboutGithubShort', '\["AboutViewReleases', '\["AboutOpenIssue') "about-links loc" | Out-Null
        }
    }

    # Prove it: full build (warnings are errors) + full test suite.
    Write-Host "`n==> build (0 warnings)" -ForegroundColor Cyan
    Push-Location $RepoRoot
    try {
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
        & dotnet test $testProj.FullName -c Debug -p:Platform=x64 --nologo -v q 2>&1 | Tee-Object -Variable testOut | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host ($testOut | Select-Object -Last 12)
            Fail "dotnet test failed"
        }
    }
    finally { Pop-Location }

    Remove-Item -LiteralPath $snapshotDir -Recurse -Force
    Write-Host "`nSample blocks removed and green: $($Blocks -join ', ')." -ForegroundColor Green
}
catch {
    if ($_.Exception.Message -notmatch "Working tree is dirty") {
        if ((Test-Path -LiteralPath $snapshotDir) -and ($snapshots.Count -gt 0)) {
            Write-Host "Rolling back." -ForegroundColor Red
            Restore-Tree
        }
        elseif (Test-Path -LiteralPath $snapshotDir) {
            Remove-Item -LiteralPath $snapshotDir -Recurse -Force
        }
    }
    throw
}
