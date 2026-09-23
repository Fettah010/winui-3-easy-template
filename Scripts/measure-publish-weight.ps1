# Measures the portable publish weight (performance plan P3-2).
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\measure-publish-weight.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\measure-publish-weight.ps1 -FailOnJump
#
# Publishes win-x64 Release to a temp dir (never touches Releases/), reports
# the total size, and compares against docs/publish-weight-baseline.json when
# present. -FailOnJump exits 1 on an unexplained >10% jump: CI (main-push)
# always passes it (armed Phase D1 — a jump gets triaged, never muted);
# without it the script only warns (local dev loop).
# Reads inside the repo only; the publish output goes to the temp folder.

[CmdletBinding()]
param(
    [switch]$FailOnJump
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$baselinePath = Join-Path $repoRoot "docs\publish-weight-baseline.json"
$outDir = Join-Path ([IO.Path]::GetTempPath()) ("devtem-weight-" + [Guid]::NewGuid().ToString("N"))

try {
    dotnet publish (Join-Path $repoRoot "DevTemWinUi3.csproj") `
        -c Release -p:Platform=x64 -r win-x64 --self-contained `
        -o $outDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "PUBLISH FAILED (exit $LASTEXITCODE)." -ForegroundColor Red
        exit 1
    }

    $bytes = (Get-ChildItem -LiteralPath $outDir -Recurse -File |
        Measure-Object -Property Length -Sum).Sum
    $mb = [math]::Round($bytes / 1MB, 1)
    Write-Host "Publish weight: $mb MB ($bytes bytes)." -ForegroundColor Green

    if (-not (Test-Path -LiteralPath $baselinePath)) {
        Write-Host "No baseline at docs/publish-weight-baseline.json; recording current as a hint (not writing)." -ForegroundColor Yellow
        Write-Host "{ `"bytes`": $bytes, `"mb`": $mb }"
        exit 0
    }

    $baseline = (Get-Content -LiteralPath $baselinePath -Raw) | ConvertFrom-Json
    $baseBytes = [long]$baseline.bytes
    if ($baseBytes -le 0) {
        Write-Host "Baseline has no usable byte count; skipping comparison." -ForegroundColor Yellow
        exit 0
    }

    $ratio = $bytes / $baseBytes
    $pct = [math]::Round(($ratio - 1) * 100, 1)
    Write-Host "Baseline: $([math]::Round($baseBytes / 1MB, 1)) MB; delta: $pct%."
    if ($ratio -gt 1.1) {
        $msg = "Publish weight jumped $pct% over baseline (>10% guardrail)."
        if ($FailOnJump) {
            Write-Host $msg -ForegroundColor Red
            exit 1
        }
        Write-Host "WARNING: $msg" -ForegroundColor Yellow
    }
    exit 0
}
finally {
    try { Remove-Item -LiteralPath $outDir -Recurse -Force -ErrorAction SilentlyContinue } catch { }
}
