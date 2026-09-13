# Verifies the Templates/Project mirror matches app sources.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\test-mirror-parity.ps1
#
# Fails on: app files missing from the template (and not repo-only),
# template files missing from the app (and not template-only), and content
# diffs in files expected verbatim. Hand-conditioned files (feature `#if`
# blocks, engine-specific identifiers, per-audience guides) are allowlisted
# by path below — everything else must be byte-identical.
# Also checks the nested page template (Templates/Page vs
# Templates/Project/Templates/Page), modulo its dormant-config mechanism.
# Same check runs in CI (.github/workflows/templates.yml).

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$appRoot = $repoRoot
$templateRoot = Join-Path $repoRoot "Templates\Project"

$skipDirs = @("bin", "obj", ".git", ".vs", "TestResults", "Logs", "nupkgs", "Releases", ".icon-backup")

# App paths with no template counterpart (repo harness / repo docs).
# Scaffolded apps intentionally ship release CI only.
$repoOnly = @(
    "UI/**",
    "Packaging/DevTem.Templates/**",
    "Templates/Page/**",
    ".github/dependabot.yml",
    ".github/FUNDING.yml",
    ".github/ISSUE_TEMPLATE/**",
    ".github/workflows/msix.yml",
    ".github/workflows/templates.yml",
    ".github/workflows/templates-publish.yml",
    ".github/workflows/ui-tests.yml",
    "Scripts/test-templates.ps1",
    "Scripts/test-mirror-parity.ps1",
    "CHANGELOG.md",
    "LICENSE",
    "CONTRIBUTING.md",
    "SECURITY.md",
    "docs/RELEASE-PLAN.md",
    "docs/STATE.md",
    "docs/WORKFLOW.md",
    "docs/DECISIONS.md",
    "docs/why-devtem.md",
    "docs/template-contract.md",
    "docs/TEMPLATE-REUSABILITY-ROADMAP.md",
    "docs/template-features.json",
    "docs/discovery/**",
    "CITATION.cff",
    "social-preview.png"
)

# Template paths with no app counterpart (template-only machinery).
$templateOnly = @(
    ".template.config/**",
    "Services/AppFeatures.cs",
    "DevTemAttribution.cs",
    "Build/**",
    "Templates/**",
    "docs/FEATURES.md",
    "docs/feature-guides/**"
)

# Files allowed to differ (hand-conditioned): feature `#if` blocks,
# engine-specific naming (DevTemWinUi3Tray_ vs DevTemTray_), per-audience
# guides (repo vs scaffolded), and the project files (template uses
# Exists-guarded feature imports and excludes UI — versions still checked
# separately below). Everything else must match.
$conditioned = @(
    "AGENTS.md",
    "App.xaml.cs",
    "DevTemWinUi3.csproj",
    "DevTemWinUi3.sln",
    "Directory.Build.props",
    "MainWindow.xaml",
    "MainWindow.xaml.cs",
    "Program.cs",
    "README.md",
    "Pages\AboutPage.xaml",
    "Pages\AboutPage.xaml.cs",
    "Pages\HomePage.xaml",
    "Pages\HomePage.xaml.cs",
    "Pages\SettingsPage.xaml",
    "Pages\SettingsPage.xaml.cs",
    "Scripts\init-template.ps1",
    "Services\BackgroundUpdateService.cs",
    "Services\LocalizationService.cs",
    "Services\ServiceLocator.cs",
    "Services\SystemTrayService.cs",
    "Tests\Services\ServiceLocatorTests.cs",
    "ViewModels\SettingsPageViewModel.cs",
    "docs\TEMPLATE-GUIDE.md"
)

# File renames between the trees (content must still match).
$renames = @{
    "DevTemWinUi3.sln"                = "__SafeName__.sln"
    "DevTemWinUi3.csproj"             = "__SafeName__.csproj"
    "Tests\DevTemWinUi3.Tests.csproj" = "Tests\__SafeName__.Tests.csproj"
}

function Test-Glob([string]$rel, [string[]]$patterns) {
    $relNorm = $rel -replace '/', '\'
    foreach ($pat in $patterns) {
        $patNorm = $pat -replace '/', '\'
        if ($patNorm.EndsWith("\**")) {
            $prefix = $patNorm.Substring(0, $patNorm.Length - 3)
            if ($relNorm -eq $prefix -or $relNorm.StartsWith($prefix + "\")) {
                return $true
            }
        }
        elseif ($relNorm -like $patNorm) {
            return $true
        }
    }
    return $false
}

function Test-Skipped([string]$rel) {
    # Frozen reference code is documentation, not product.
    if ($rel -eq "docs\archive" -or $rel.StartsWith("docs\archive\")) {
        return $true
    }
    # Any path SEGMENT may be a skipped dir (bin/obj/TestResults nest deep).
    foreach ($seg in ($rel -split '[\\/]')) {
        if ($skipDirs -contains $seg) {
            return $true
        }
    }
    return $false
}

function Get-TreeFiles([string]$root) {
    $files = @{}
    Get-ChildItem -LiteralPath $root -Recurse -File -Force | ForEach-Object {
        $rel = $_.FullName.Substring($root.Length + 1)
        if (-not (Test-Skipped $rel)) { $files[$rel] = $_.FullName }
    }
    return $files
}

$failures = @()
$checked = 0

$appFiles = Get-TreeFiles $appRoot
# Never compare the template against itself.
$templateFiles = Get-TreeFiles $templateRoot

foreach ($rel in ($appFiles.Keys | Sort-Object)) {
    # Skip the whole Templates dir on the app side (nested copy checked separately).
    if ($rel -eq "Templates" -or $rel.StartsWith("Templates\") -or $rel.StartsWith("Templates/")) {
        continue
    }
    $templateRel = $rel
    if ($renames.ContainsKey($rel)) { $templateRel = $renames[$rel] }
    if (-not $templateFiles.ContainsKey($templateRel)) {
        if (-not (Test-Glob $rel $repoOnly)) {
            $failures += "missing from template: $rel"
        }
        continue
    }
    if ($conditioned -contains $rel) { continue }
    $checked++
    $a = (Get-FileHash -LiteralPath $appFiles[$rel]).Hash
    $b = (Get-FileHash -LiteralPath $templateFiles[$templateRel]).Hash
    if ($a -ne $b) {
        $failures += "content differs (not allowlisted): $rel"
    }
}

foreach ($rel in ($templateFiles.Keys | Sort-Object)) {
    $appRel = $rel
    foreach ($entry in $renames.GetEnumerator()) {
        if ($entry.Value -eq $rel) { $appRel = $entry.Key }
    }
    if (Test-Glob $rel $templateOnly) { continue }
    if (-not $appFiles.ContainsKey($appRel)) {
        $failures += "template-only file (not allowlisted): $rel"
    }
}

# Nested page template: Templates/Page/X <-> Templates/Project/Templates/Page/X,
# except the dormant config (config.hold vs .template.config).
$pageSrc = Join-Path $repoRoot "Templates\Page"
$pageDst = Join-Path $templateRoot "Templates\Page"
$pageSrcFiles = Get-TreeFiles $pageSrc
$pageDstFiles = Get-TreeFiles $pageDst
foreach ($rel in ($pageSrcFiles.Keys | Sort-Object)) {
    if ($rel -eq ".template.config\template.json") {
        if (-not $pageDstFiles.ContainsKey("config.hold\template.json.hold")) {
            $failures += "nested page template missing dormant config: config.hold/template.json.hold"
        }
        continue
    }
    $dstRel = $rel
    if ($rel -eq ".template.config\icon.png") {
        $dstRel = "config.hold\icon.png"
    }
    if (-not $pageDstFiles.ContainsKey($dstRel)) {
        $failures += "nested page template missing: $rel"
        continue
    }
    $checked++
    if ((Get-FileHash -LiteralPath $pageSrcFiles[$rel]).Hash -ne (Get-FileHash -LiteralPath $pageDstFiles[$dstRel]).Hash) {
        $failures += "nested page template differs: $rel"
    }
}
foreach ($rel in ($pageDstFiles.Keys | Sort-Object)) {
    if ($rel.StartsWith("config.hold\")) { continue }
    if (-not $pageSrcFiles.ContainsKey($rel)) {
        $failures += "nested page template has extra file: $rel"
    }
}

# Version consistency: the csproj pair is structurally conditioned, but the
# four version properties must always agree (a bump in one and not the
# other ships mismatched assemblies).
try {
    $versionProps = @("Version", "AssemblyVersion", "FileVersion", "InformationalVersion")
    foreach ($prop in $versionProps) {
        $a = ([xml](Get-Content -LiteralPath (Join-Path $appRoot "DevTemWinUi3.csproj"))).Project.PropertyGroup |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_.$prop) } |
            Select-Object -First 1 -ExpandProperty $prop
        $b = ([xml](Get-Content -LiteralPath (Join-Path $templateRoot "__SafeName__.csproj"))).Project.PropertyGroup |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_.$prop) } |
            Select-Object -First 1 -ExpandProperty $prop
        if ($a -ne $b) {
            $failures += "version mismatch <$prop>: app=$a template=$b"
        }
    }
}
catch {
    $failures += "version check failed to run: $($_.Exception.Message)"
}

if ($failures.Count -gt 0) {
    Write-Host "MIRROR PARITY FAILED ($($failures.Count)):" -ForegroundColor Red
    $failures | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
Write-Host "MIRROR PARITY OK ($checked files compared)." -ForegroundColor Green
