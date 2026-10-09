# Nightly per-combo publish ceilings (v0.5.0, executes deferred D2).
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\measure-combo-weights.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\measure-combo-weights.ps1 -Combos allon,minimal,msixapp
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\measure-combo-weights.ps1 -JsonOut C:\temp\weights.json
#
# Scaffolds every measurable matrix combo from Templates/Project, publishes
# Release win-x64 self-contained (same config as measure-publish-weight.ps1),
# and compares each against docs/publish-weight-ceilings.json. Exits 1 on any
# breach. Combos that fail the build by design (badupd/badupd2 guards) are
# skipped with a note. Without a ceilings file the script only reports
# measurements (seeding mode for the first ceilings commit).
# Nightly-only cost (~4 min per combo): CI runs this on schedule, never on PRs.

[CmdletBinding()]
param(
    [string[]]$Combos = @("allon", "alloff", "notray", "noupd", "updbasic", "nohttp", "nodiag", "nocrash", "noloc", "notests", "logmel", "lognone", "minimal", "desktop", "production", "nosetup", "msixapp", "msixstore", "msixnone"),
    [string]$JsonOut = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$templateSource = Join-Path $repoRoot "Templates\Project"
$ceilingsPath = Join-Path $repoRoot "docs\publish-weight-ceilings.json"
$Combos = @($Combos | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })

# Flag sets mirror Scripts/test-templates.ps1 combo definitions. If a combo
# gains flags there, add them here (parity does not check this file).
$comboFlags = @{
    "allon"      = @("--auth", "true")
    "alloff"     = @("--tray", "false", "--updates", "none", "--database", "false")
    "notray"     = @("--tray", "false")
    "noupd"      = @("--updates", "none")
    "updbasic"   = @("--updates", "basic")
    "nohttp"     = @("--http", "false")
    "nodiag"     = @("--health", "false")
    "nocrash"    = @("--crash", "false")
    "noloc"      = @("--localization", "false")
    "notests"    = @("--tests", "false")
    "logmel"     = @("--logging", "mel")
    "lognone"    = @("--logging", "none")
    "minimal"    = @("--tray", "false", "--updates", "none", "--database", "false", "--http", "false", "--health", "false", "--logging", "none", "--crash", "false", "--localization", "false", "--tests", "false", "--attribution", "false")
    "desktop"    = @("--updates", "none", "--database", "false", "--http", "false")
    "production" = @()
    "nosetup"    = @("--setup", "false")
    "msixapp"    = @("--distribution", "msix", "--updates", "appinstaller", "--setup", "false")
    "msixstore"  = @("--distribution", "msix", "--updates", "store", "--setup", "false", "--publisher", "CN=Store-Test")
    "msixnone"   = @("--distribution", "msix", "--updates", "none", "--setup", "false")
}

$ceilings = $null
if (Test-Path -LiteralPath $ceilingsPath) {
    $ceilings = (Get-Content -LiteralPath $ceilingsPath -Raw) | ConvertFrom-Json
}
else {
    Write-Host "No ceilings at docs/publish-weight-ceilings.json; reporting measurements only (seeding mode)." -ForegroundColor Yellow
}

$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("devtem-comboweights-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
$results = @()
$failed = 0

try {
    try { dotnet new uninstall $templateSource 2>&1 | Out-Null } catch { }
    dotnet new install $templateSource 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Template install failed (exit $LASTEXITCODE)." }

    foreach ($combo in $Combos) {
        if (-not $comboFlags.ContainsKey($combo)) { throw "Unknown combo '$combo'." }
        $name = "W" + $combo
        $outDir = Join-Path $tempRoot $combo
        Write-Host "==> $combo"
        dotnet new devtem-winui -n $name -o $outDir @($comboFlags[$combo]) 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Scaffold failed for $combo (exit $LASTEXITCODE)." }
        $csproj = Get-ChildItem -LiteralPath $outDir -Filter "*.csproj" -Recurse |
            Where-Object { $_.FullName -notmatch "\\Tests\\" } | Select-Object -First 1
        if (-not $csproj) { throw "No app csproj found for $combo." }
        $pubDir = Join-Path $outDir "pub-out"
        dotnet publish $csproj.FullName -c Release -p:Platform=x64 -r win-x64 --self-contained -o $pubDir 2>&1 |
            Select-Object -Last 2 | Out-String | Write-Host
        if ($LASTEXITCODE -ne 0) { throw "Publish failed for $combo (exit $LASTEXITCODE)." }
        $bytes = (Get-ChildItem -LiteralPath $pubDir -Recurse -File |
            Measure-Object -Property Length -Sum).Sum
        $mb = [math]::Round($bytes / 1MB, 1)
        $entry = [ordered]@{ combo = $combo; mb = $mb; bytes = $bytes }
        if ($ceilings -and $ceilings.combos.$combo) {
            $ceiling = [double]$ceilings.combos.$combo
            $entry["ceilingMb"] = $ceiling
            if ($mb -gt $ceiling) {
                $entry["status"] = "BREACH"
                Write-Host "$combo : $mb MB OVER ceiling $ceiling MB" -ForegroundColor Red
                $failed++
            }
            else {
                $entry["status"] = "OK"
                Write-Host "$combo : $mb MB (ceiling $ceiling MB)" -ForegroundColor Green
            }
        }
        else {
            $entry["status"] = "measured"
            Write-Host "$combo : $mb MB (no ceiling; seeding mode)"
        }
        $results += $entry
    }
}
finally {
    try { dotnet new uninstall $templateSource 2>&1 | Out-Null } catch { }
    try { Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}

if ($JsonOut) {
    ($results | ConvertTo-Json -Depth 4) | Set-Content -LiteralPath $JsonOut
    Write-Host "Results written to $JsonOut"
}
if ($failed -gt 0) {
    Write-Host "$failed combo(s) breached their ceilings." -ForegroundColor Red
    exit 1
}
Write-Host "All measured combos within ceilings (or seeding mode)." -ForegroundColor Green
