# Initializes a new app from this template: rewrites the display name,
# identifier-safe name, company, and repo URL across code, XAML, scripts,
# CI workflow, and docs — then verifies zero template leftovers.
#
#   .\Scripts\init-template.ps1 -AppName "Acme Desk" -Company "Acme" `
#       -RepoUrl "https://github.com/acme/desk-app" -Scheme "acme://"
#
# Code surfaces (mutex, registry, folders, feeds) read Services/Helpers/AppMetadata.cs,
# whose defaults this script rewrites too. Repo/license URL surfaces resolve
# at runtime from Services/Configuration/ProductConfiguration.cs, whose
# defaults this script rewrites (-RepoUrl always; -LicenseName/-LicenseUrl
# when your license differs from MIT). Manual steps afterwards (see
# docs/TEMPLATE-GUIDE.md): replace Assets art, re-run create-shortcut.ps1.
#
#   .\Scripts\init-template.ps1 -Validate   # no renames: checks the current
#       tree's identity consistency (metadata vs csproj vs manifest) and
#       fails itemizing problems. The scaffold matrix runs this per combo.
param(
    [string]$AppName = "",
    [string]$Company = "",
    [string]$RepoUrl = "",
    [string]$SafeName = "",
    [string]$Scheme = "",
    [string]$LicenseName = "MIT License",
    [string]$LicenseUrl = "",
    [switch]$Validate
)

$ErrorActionPreference = "Stop"

if ($Validate) {
    $root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $problems = @()
    $metaPath = Join-Path $root "Services\Helpers\AppMetadata.cs"
    $prodPath = Join-Path $root "Services\Configuration\ProductConfiguration.cs"
    if (-not (Test-Path -LiteralPath $metaPath)) { $problems += "missing Services/Helpers/AppMetadata.cs" }
    if (-not (Test-Path -LiteralPath $prodPath)) { $problems += "missing Services/Configuration/ProductConfiguration.cs" }
    if ($problems.Count -eq 0) {
        $meta = [System.IO.File]::ReadAllText($metaPath, [System.Text.Encoding]::UTF8)
        $prod = [System.IO.File]::ReadAllText($prodPath, [System.Text.Encoding]::UTF8)
        $appName = ([regex]::Match($meta, 'AppName\s*=\s*"([^"]+)"')).Groups[1].Value
        $safe = ([regex]::Match($meta, 'SafeName\s*=\s*"([^"]+)"')).Groups[1].Value
        $company = ([regex]::Match($meta, 'Company\s*=\s*"([^"]+)"')).Groups[1].Value
        $prefix = ([regex]::Match($meta, 'ProtocolPrefix\s*=\s*"([^"]+)"')).Groups[1].Value
        $repo = ([regex]::Match($prod, 'RepoUrl\s*=\s*"([^"]+)"')).Groups[1].Value
        $license = ([regex]::Match($prod, 'LicenseName\s*=\s*"([^"]+)"')).Groups[1].Value
        $licenseUrl = ([regex]::Match($prod, 'LicenseUrl\s*=\s*"([^"]*)"')).Groups[1].Value
        if ([string]::IsNullOrWhiteSpace($appName)) { $problems += "AppMetadata.AppName is empty" }
        if ($safe -notmatch "^[A-Za-z_][A-Za-z0-9_]*$") { $problems += "AppMetadata.SafeName '$safe' is not identifier-safe" }
        if ([string]::IsNullOrWhiteSpace($company)) { $problems += "AppMetadata.Company is empty" }
        if ($prefix -notmatch "^[a-z][a-z0-9+.-]*://$") { $problems += "AppMetadata.ProtocolPrefix '$prefix' is not a lowercase scheme:// URI" }
        if ($repo -notmatch "^https://github\.com/[^/]+/[^/]+$") { $problems += "ProductConfiguration.RepoUrl '$repo' is not https://github.com/org/name" }
        if ([string]::IsNullOrWhiteSpace($license)) { $problems += "ProductConfiguration.LicenseName is empty" }
        if (-not [string]::IsNullOrWhiteSpace($licenseUrl) -and -not [System.Uri]::IsWellFormedUriString($licenseUrl, [System.UriKind]::Absolute)) {
            $problems += "ProductConfiguration.LicenseUrl '$licenseUrl' is not an absolute URI"
        }

        $csprojs = @(Get-ChildItem -LiteralPath $root -Filter "*.csproj" -File |
            Where-Object { $_.FullName -notlike "*\Tests\*" -and $_.FullName -notlike "*\Packaging\*" })
        if ($csprojs.Count -ne 1) { $problems += "expected exactly one app csproj, found $($csprojs.Count)" }
        else {
            $csproj = [System.IO.File]::ReadAllText($csprojs[0].FullName, [System.Text.Encoding]::UTF8)
            $ns = ([regex]::Match($csproj, "<RootNamespace>([^<]+)</RootNamespace>")).Groups[1].Value
            $asm = ([regex]::Match($csproj, "<AssemblyName>([^<]+)</AssemblyName>")).Groups[1].Value
            if ($ns -ne $safe) { $problems += "csproj RootNamespace '$ns' disagrees with SafeName '$safe'" }
            if ($asm -ne $safe) { $problems += "csproj AssemblyName '$asm' disagrees with SafeName '$safe'" }
            $slnName = "$safe.sln"
            if (-not (Test-Path -LiteralPath (Join-Path $root $slnName))) { $problems += "missing solution $slnName" }
        }

        $manifest = Join-Path $root "Packaging\Msix\Package.appxmanifest"
        if (Test-Path -LiteralPath $manifest) {
            $manifestText = [System.IO.File]::ReadAllText($manifest, [System.Text.Encoding]::UTF8)
            $schemeName = $prefix.TrimEnd('/').TrimEnd(':')
            if ($manifestText -notmatch [regex]::Escape('Name="' + $schemeName + '"')) {
                $problems += "manifest protocol Name does not match scheme '$schemeName'"
            }
        }

        if (($meta | Select-String -Pattern "DevTemWinUi3" -SimpleMatch) -and ($safe -ne "DevTemWinUi3")) {
            $problems += "AppMetadata.cs still mentions DevTemWinUi3 after rename to '$safe'"
        }
    }

    if ($problems.Count -gt 0) {
        Write-Host "VALIDATE FAILED ($($problems.Count)):" -ForegroundColor Red
        $problems | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        throw "init-template -Validate found $($problems.Count) identity problem(s)."
    }

    Write-Host "VALID: identity consistent ($safe / $appName / $prefix)." -ForegroundColor Green
    return
}

if ([string]::IsNullOrWhiteSpace($AppName)) { throw "Missing -AppName (or pass -Validate to only check the tree)." }
if ([string]::IsNullOrWhiteSpace($Company)) { throw "Missing -Company (or pass -Validate to only check the tree)." }
if ([string]::IsNullOrWhiteSpace($RepoUrl)) { throw "Missing -RepoUrl (or pass -Validate to only check the tree)." }

if ([string]::IsNullOrWhiteSpace($SafeName)) {
    $SafeName = $AppName -replace "[^A-Za-z0-9_]", "_"
    if ($SafeName -notmatch "^[A-Za-z_]") { $SafeName = "_" + $SafeName }
    $SafeName = $SafeName -replace "^_[0-9]+", "_"
}
if ($SafeName -notmatch "^[A-Za-z_][A-Za-z0-9_]*$") {
    throw "SafeName '$SafeName' must start with a letter or underscore and contain only letters, digits, or underscores."
}
$RepoPath = ($RepoUrl -replace "^https://github\.com/", "").TrimEnd("/")
if ($RepoPath -notmatch "^[^/]+/[^/]+$") {
    throw "RepoUrl must be a GitHub URL or org/name path, for example https://github.com/acme/desk-app."
}
$RepoSlug = Split-Path $RepoPath -Leaf
if ([string]::IsNullOrWhiteSpace($Scheme)) { $Scheme = $SafeName.ToLowerInvariant() + "://" }
if ($Scheme -notmatch "^[a-z][a-z0-9+.-]*://$") {
    throw "Scheme '$Scheme' must use a lowercase URI scheme followed by ://, for example acme://."
}
$SchemeName = $Scheme.TrimEnd('/').TrimEnd(':')

$replacements = @(
    # Order matters: longest/most-specific first; bare DevTem (prose) last.
    @("Fettah010/winui-3-easy-template", $RepoPath),
    @("Fettah010", $Company),
    @("DevTem-WinUI 3", $AppName),
    @("DevTemWinUi3", $SafeName),
    @("DevTemTray_", $SafeName + "Tray_"),
    @('Name="devtem"', 'Name="' + $SchemeName + '"'),
    @("devtem://", $Scheme),
    @("winui-3-easy-template", $RepoSlug),
    @('"Fettah"', "`"$Company`""),
    @("DevTem", $AppName)
)

$extensions = @("*.cs", "*.xaml", "*.ps1", "*.yml", "*.md", "*.csproj", "*.manifest", "*.vbs", "*.bat", "*.sln", "*.cff")
# docs/archive is frozen reference; everything else (incl. README/AGENTS) is rewritten.
$skipDirs = @(".git", "bin", "obj", "docs\archive")
$selfName = Split-Path $PSCommandPath -Leaf
# TEMPLATE-GUIDE.md documents the tokens themselves: never rewrite it.
$frozenNames = @($selfName, "TEMPLATE-GUIDE.md")

$files = Get-ChildItem -LiteralPath $PSScriptRoot/.. -Recurse -File -Include $extensions |
    Where-Object {
        $full = $_.FullName
        -not ($skipDirs | Where-Object { $full -like "*\$_\*" }) -and
        $_.Name -notin $frozenNames
    }

$totalEdits = 0
foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $original = $text
    foreach ($pair in $replacements) {
        $text = $text.Replace($pair[0], $pair[1])
    }
    if ($text -ne $original) {
        [System.IO.File]::WriteAllText($file.FullName, $text)
        $totalEdits++
    }
}

# License identity lives in ProductConfiguration defaults (About resolves
# them at runtime): rewrite the defaults when they differ from MIT.
$productConfig = Join-Path $PSScriptRoot "..\Services\Configuration\ProductConfiguration.cs"
if (Test-Path -LiteralPath $productConfig) {
    $pcText = [System.IO.File]::ReadAllText($productConfig)
    $pcOriginal = $pcText
    if ($LicenseName -ne "MIT License") {
        $pcText = $pcText.Replace('LicenseName = "MIT License"', 'LicenseName = "' + $LicenseName + '"')
    }
    if (-not [string]::IsNullOrWhiteSpace($LicenseUrl)) {
        $pcText = [regex]::Replace($pcText, 'LicenseUrl = ""', 'LicenseUrl = "' + $LicenseUrl + '"')
    }
    if ($pcText -ne $pcOriginal) {
        [System.IO.File]::WriteAllText($productConfig, $pcText)
        $totalEdits++
    }
}

Write-Host "Rewrote $totalEdits file(s) for '$AppName' ($SafeName) -> $RepoUrl" -ForegroundColor Green

# Rename the project files themselves so the output exe matches the new app.
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$oldCsproj = Join-Path $root "DevTemWinUi3.csproj"
$newCsproj = Join-Path $root "$SafeName.csproj"
if ((Test-Path -LiteralPath $oldCsproj) -and $SafeName -ne "DevTemWinUi3") {
    Move-Item -LiteralPath $oldCsproj -Destination $newCsproj -Force
    $testCsproj = Join-Path $root "Tests\DevTemWinUi3.Tests.csproj"
    if (Test-Path -LiteralPath $testCsproj) {
        $newTestCsproj = Join-Path $root "Tests\$SafeName.Tests.csproj"
        Move-Item -LiteralPath $testCsproj -Destination $newTestCsproj -Force
        $testText = [System.IO.File]::ReadAllText($newTestCsproj)
        $testText = $testText.Replace("..\DevTemWinUi3.csproj", "..\$SafeName.csproj")
        [System.IO.File]::WriteAllText($newTestCsproj, $testText)
    }
    Write-Host "Renamed project files to $SafeName[.Tests].csproj" -ForegroundColor Green
}

# Rename the solution alongside the projects (its project paths carry the
# SafeName, already rewritten by the pass above).
$oldSln = Join-Path $root "DevTemWinUi3.sln"
$newSln = Join-Path $root "$SafeName.sln"
if ((Test-Path -LiteralPath $oldSln) -and $SafeName -ne "DevTemWinUi3") {
    Move-Item -LiteralPath $oldSln -Destination $newSln -Force
    Write-Host "Renamed solution to $SafeName.sln" -ForegroundColor Green
}

# Verify: no template identity may remain (outside archive + this script).
# Case-sensitive on purpose: the scaffolder command `devtem-page` (lowercase)
# is stable across renames and must not match the `DevTem` prose token.
$leftovers = Get-ChildItem -LiteralPath $PSScriptRoot/.. -Recurse -File -Include $extensions |
    Where-Object {
        $full = $_.FullName
        -not ($skipDirs | Where-Object { $full -like "*\$_\*" }) -and
        $_.Name -notin $frozenNames
    } |
    Select-String -Pattern 'DevTem|devtem://|Name="devtem"|Fettah010|winui-3-easy-template' -CaseSensitive |
    Select-Object -ExpandProperty Path -Unique

if ($leftovers) {
    Write-Host "LEFTOVER template identity in:" -ForegroundColor Red
    $leftovers | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    throw "init-template failed verification."
}

Write-Host "Verification passed: no template leftovers." -ForegroundColor Green
Write-Host "Next: replace Assets art, then run Scripts/create-shortcut.ps1 (docs/TEMPLATE-GUIDE.md)." -ForegroundColor Cyan
