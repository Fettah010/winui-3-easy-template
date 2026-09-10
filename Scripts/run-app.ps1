# ------------------------------------------------------------------------------
# Dev launcher for MyWinUIApp.
#
# Double-click the "MyWinUIApp (Dev)" desktop shortcut (or run this file) to
# build the Debug configuration and launch the app window — no terminal typing
# needed. Incremental builds are fast when nothing changed.
#
#   .\run-app.ps1            # build (if needed) + launch
#   .\run-app.ps1 -NoBuild   # just launch the existing Debug output
# ------------------------------------------------------------------------------

param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Exe = Join-Path $ProjectRoot "bin\x64\Debug\net10.0-windows10.0.19041.0\MyWinUIApp.exe"

if (-not $NoBuild) {
    Write-Host "==> Building MyWinUIApp (Debug/x64)..." -ForegroundColor Cyan
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

Write-Host "==> Launching $Exe" -ForegroundColor Cyan
Start-Process -FilePath $Exe
Write-Host "==> Running. Close the app window to end."