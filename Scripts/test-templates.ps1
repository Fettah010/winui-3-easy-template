# Scaffold matrix for the dotnet-new templates: installs both templates from
# this repo, scaffolds all-on / all-off / no-tray / no-updates, wires a page
# end-to-end inside all-on via add-page.ps1, then builds (0 warnings,
# 0 errors) and tests each scaffold. Fails the run on the first broken combo.
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
    "allon"  = @{ Name = "AcmeDesk";  Safe = "AcmeDesk";  Display = "Acme Desk";  Company = "Acme"; Repo = "acme/desk-app"; Scheme = "acme://"; Flags = @() }
    "alloff" = @{ Name = "Bare App";  Safe = "Bare_App";  Display = "Bare App";   Company = "Bare"; Repo = "bare/app";      Scheme = "bare://"; Flags = @("--tray", "false", "--updates", "false", "--database", "false") }
    "notray" = @{ Name = "NoTrayApp"; Safe = "NoTrayApp"; Display = "NoTray App"; Company = "Nt";   Repo = "nt/app";        Scheme = "nt://";   Flags = @("--tray", "false") }
    "noupd"  = @{ Name = "NoUpdApp";  Safe = "NoUpdApp";  Display = "NoUpd App";  Company = "Nu";   Repo = "nu/app";        Scheme = "nu://";   Flags = @("--updates", "false") }
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

# Rename-engine proof: the scaffold must carry the requested scheme in every
# surface the engines touch (dotnet-new `scheme`/`schemeName` symbols).
function Assert-ScaffoldIdentity([string]$outDir, [string]$scheme) {
    $bare = $scheme.TrimEnd('/').TrimEnd(':')
    $manifest = Join-Path $outDir "Packaging\Msix\Package.appxmanifest"
    $manifestText = [System.IO.File]::ReadAllText($manifest)
    if ($manifestText -notmatch [regex]::Escape('Name="' + $bare + '"')) {
        throw "manifest protocol Name is not '$bare'"
    }
    $metadata = [System.IO.File]::ReadAllText((Join-Path $outDir "Services\Helpers\AppMetadata.cs"))
    if ($metadata -notmatch [regex]::Escape('"' + $scheme + '"')) {
        throw "AppMetadata protocol prefix is not '$scheme'"
    }
    Write-Host "identity OK ($scheme / $bare)"
}

# init-template proof: re-brand a scratch copy of THIS repo and let the
# script's own leftover verification pass or fail the run (throws).
# Excludes build output, VCS, and generated dirs for copy speed.
function Invoke-InitTemplateScratchTest {
    $scratch = Join-Path ([System.IO.Path]::GetTempPath()) ("devtem-init-" + [System.Guid]::NewGuid().ToString("N"))
    try {
        $exclude = @("bin", "obj", ".git", ".vs", "TestResults", "Logs", "nupkgs", "Releases", ".icon-backup")
        $items = Get-ChildItem -LiteralPath $repoRoot -Force |
            Where-Object { $exclude -notcontains $_.Name }
        New-Item -ItemType Directory -Path $scratch -Force | Out-Null
        foreach ($item in $items) {
            Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $scratch $item.Name) -Recurse -Force
        }
        & (Join-Path $scratch "Scripts\init-template.ps1") `
            -AppName "Renamed App" -Company "Renamed" `
            -RepoUrl "https://github.com/re/named"
        Write-Host "init-template scratch re-brand OK"
    }
    finally {
        Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
    }
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

        # Rename-engine proof for this combo's scheme (cheap, fail fast).
        try { Assert-ScaffoldIdentity $outDir $c.Scheme }
        catch {
            Write-Host "FAILED: identity $combo : $_" -ForegroundColor Red
            $failed++
            continue
        }

        # Item-template proof: add-page.ps1 wires a page end-to-end inside the
        # all-on app (scaffold + strings + DI + route/nav + its own build +
        # test). Not a git repo down here, so snapshot rollback applies.
        if ($combo -eq "allon") {
            if (-not (Invoke-Step "add-page smoke in allon" {
                    & (Join-Path $repoRoot "Scripts\add-page.ps1") -RepoRoot $outDir -TemplateSource (Join-Path $repoRoot "Templates\Page") -Name OrdersSmoke -Title "Smoke Orders" -Icon Shop
                })) { continue }
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

        $testOut = & dotnet test (Join-Path $outDir "Tests\$($c.Safe).Tests.csproj") -c Debug -p:Platform=x64 --nologo -v q 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0 -or $testOut -notmatch 'Passed!' -or $testOut -match 'Failed:\s+[1-9]') {
            Write-Host "FAILED: tests for $combo" -ForegroundColor Red
            Write-Host $testOut
            $failed++
            continue
        }
        Write-Host "PASSED: $combo" -ForegroundColor Green
    }

    # init-template proof (all combos green or not, this is independent):
    # re-brand a scratch repo copy; the script's own leftover check decides.
    Write-Host "`n==> init-template scratch" -ForegroundColor Cyan
    try {
        Invoke-InitTemplateScratchTest | Out-Null
        Write-Host "PASSED: init-template scratch" -ForegroundColor Green
    }
    catch {
        Write-Host "FAILED: init-template scratch: $_" -ForegroundColor Red
        $failed++
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

