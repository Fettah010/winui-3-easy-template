# Scaffold matrix for the dotnet-new templates: installs both templates from
# this repo, scaffolds all-on / all-off / no-tray / no-updates (plus a
# devtem-page sample inside all-on), then builds (0 warnings, 0 errors) and
# tests each scaffold. Fails the run on the first broken combo.
#
#   powershell -File Scripts/test-templates.ps1                 # full matrix
#   powershell -File Scripts/test-templates.ps1 -Combos allon,alloff
#   powershell -File Scripts/test-templates.ps1 -KeepTemp       # inspect output
#
# Same matrix runs in CI (.github/workflows/templates.yml).

param(
    [string[]]$Combos = @("allon", "alloff", "notray", "noupd"),
    [switch]$KeepTemp
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("devtem-matrix-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

$comboDefs = @{
    "allon"  = @{ Name = "AcmeDesk";  Display = "Acme Desk";  Company = "Acme"; Repo = "acme/desk-app"; Scheme = "acme://"; Flags = @() }
    "alloff" = @{ Name = "BareApp";   Display = "Bare App";   Company = "Bare"; Repo = "bare/app";      Scheme = "bare://"; Flags = @("--tray", "false", "--updates", "false", "--database", "false") }
    "notray" = @{ Name = "NoTrayApp"; Display = "NoTray App"; Company = "Nt";   Repo = "nt/app";        Scheme = "nt://";   Flags = @("--tray", "false") }
    "noupd"  = @{ Name = "NoUpdApp";  Display = "NoUpd App";  Company = "Nu";   Repo = "nu/app";        Scheme = "nu://";   Flags = @("--updates", "false") }
}

$failed = 0

function Invoke-Step([string]$label, [scriptblock]$body) {
    Write-Host "`n==> $label" -ForegroundColor Cyan
    & $body
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $label (exit $LASTEXITCODE)" -ForegroundColor Red
        $script:failed++
        return $false
    }
    return $true
}

try {
    # Clean slate, then install both templates from this repo.
    # (Uninstalls fail when nothing is installed — expected, ignored.)
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Project") 2>&1 | Out-Null } catch { }
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Page") 2>&1 | Out-Null } catch { }
    if (-not (Invoke-Step "install devtem-winui" { & dotnet new install (Join-Path $repoRoot "Templates\Project") })) { throw "install failed" }
    if (-not (Invoke-Step "install devtem-page" { & dotnet new install (Join-Path $repoRoot "Templates\Page") })) { throw "install failed" }

    foreach ($combo in $Combos) {
        if (-not $comboDefs.ContainsKey($combo)) { throw "Unknown combo: $combo (allon/alloff/notray/noupd)" }
        $c = $comboDefs[$combo]
        $outDir = Join-Path $tempRoot "$combo\$($c.Name)"

        $scaffoldArgs = @("devtem-winui", "-n", $c.Name, "--displayName", $c.Display,
            "--company", $c.Company, "--repo", $c.Repo, "--scheme", $c.Scheme,
            "--output", $outDir) + $c.Flags
        if (-not (Invoke-Step "scaffold $combo" { & dotnet new @scaffoldArgs })) { continue }

        # Item-template proof: a page scaffolded inside the all-on app must build too.
        if ($combo -eq "allon") {
            if (-not (Invoke-Step "scaffold Orders page in allon" {
                    & dotnet new devtem-page -n Orders --output (Join-Path $outDir "Pages_TemplateProbe")
                })) { continue }
            # Probe goes to a throwaway dir (not wired into the build); remove it.
            Remove-Item -LiteralPath (Join-Path $outDir "Pages_TemplateProbe") -Recurse -Force
        }

        $buildLog = ""
        $buildCode = 0
        Push-Location $outDir
        try {
            $buildLog = & dotnet build -c Debug -p:Platform=x64 --no-incremental -v q 2>&1 | Out-String
            $buildCode = $LASTEXITCODE
        }
        finally { Pop-Location }
        if ($buildCode -ne 0) {
            Write-Host "FAILED: build $combo (exit $buildCode)" -ForegroundColor Red
            Write-Host $buildLog
            $failed++
            continue
        }
        $warn = ([regex]::Matches($buildLog, '(\d+) Warning\(s\)') | Select-Object -Last 1).Groups[1].Value
        $err = ([regex]::Matches($buildLog, '(\d+) Error\(s\)') | Select-Object -Last 1).Groups[1].Value
        if ($warn -ne "0" -or $err -ne "0") {
            Write-Host "FAILED: $combo has $warn warning(s), $err error(s) (must be 0/0)" -ForegroundColor Red
            $failed++
            continue
        }
        Write-Host "build ${combo}: 0 warnings, 0 errors" -ForegroundColor Green

        $testOut = & dotnet test (Join-Path $outDir "Tests\$($c.Name).Tests.csproj") -c Debug -p:Platform=x64 --nologo -v q 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0 -or $testOut -notmatch 'Passed!' -or $testOut -match 'Failed:\s+[1-9]') {
            Write-Host "FAILED: tests for $combo" -ForegroundColor Red
            Write-Host $testOut
            $failed++
            continue
        }
        Write-Host "PASSED: $combo" -ForegroundColor Green
    }
}
finally {
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Project") 2>&1 | Out-Null } catch { }
    try { & dotnet new uninstall (Join-Path $repoRoot "Templates\Page") 2>&1 | Out-Null } catch { }
    if (-not $KeepTemp -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
    elseif ($KeepTemp) {
        Write-Host "Temp scaffolds kept at: $tempRoot"
    }
}

if ($failed -gt 0) {
    Write-Host "`nMATRIX FAILED ($failed)" -ForegroundColor Red
    exit 1
}
Write-Host "`nMATRIX PASSED" -ForegroundColor Green

