# ------------------------------------------------------------------------------
# Dev launcher for DevTem-WinUI 3.
#
# Double-click the "DevTem-WinUI 3 (Dev)" desktop shortcut (or run this file) to
# build the Debug configuration and launch the app window — no terminal typing
# needed. Incremental builds are fast when nothing changed. The shortcut is
# created by Scripts\create-shortcut.ps1 (re-run it if the shortcut is missing).
#
#   .\run-app.ps1            # build (if needed) + launch
#   .\run-app.ps1 -NoBuild   # just launch the existing Debug output
# ------------------------------------------------------------------------------

param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Exe = Join-Path $ProjectRoot "bin\x64\Debug\net10.0-windows10.0.19041.0\DevTemWinUi3.exe"

if (-not $NoBuild) {
    Write-Host "==> Building DevTem-WinUI 3 (Debug/x64)..." -ForegroundColor Cyan
    Push-Location $ProjectRoot
    try {
        & dotnet build -c Debug -p:Platform=x64
        if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path $Exe)) {
    throw "App not found at $Exe`nRun without -NoBuild once, or check the build output."
}

# Stale-binary guard (pain-log #18): the desktop shortcut targets the built
# exe directly, so after big changes (new pages/routes) it can launch an OLD
# binary where the new route does not exist. Print the binary timestamp vs
# the repo HEAD so a "click does nothing" report names the mismatch.
try {
    $exeTime = (Get-Item -LiteralPath $Exe).LastWriteTimeUtc.ToString("u")
    Write-Host "Binary: $Exe" -ForegroundColor DarkGray
    Write-Host "Built:  $exeTime (UTC)" -ForegroundColor DarkGray
    Push-Location $ProjectRoot
    try {
        $head = (& git rev-parse --short HEAD 2>$null)
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($head)) {
            $headTime = (& git log -1 --format=%cI 2>$null)
            Write-Host "HEAD:   $head ($headTime)" -ForegroundColor DarkGray
            if (-not [string]::IsNullOrWhiteSpace($headTime)) {
                $headUtc = [DateTime]::Parse($headTime).ToUniversalTime()
                $exeLocal = (Get-Item -LiteralPath $Exe).LastWriteTimeUtc
                if ($exeLocal -lt $headUtc) {
                    Write-Host "WARNING: binary is older than HEAD - rebuild to test latest routes." -ForegroundColor Yellow
                }
            }
        }
    }
    finally { Pop-Location }
}
catch { }

Write-Host "==> Launching $Exe" -ForegroundColor Cyan
Start-Process -FilePath $Exe
Write-Host "==> Running. Close the app window to end."