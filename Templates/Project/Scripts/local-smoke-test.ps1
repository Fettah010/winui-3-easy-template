# ------------------------------------------------------------------------------
# Local smoke-test of Velopack WITHOUT GitHub.
#
# 1. Packs the app into Releases/ as an installable setup.exe + update feed.
# 2. Packs a second bump (Version+1) into ReleasesLocal/ so you can point the
#    app at a local folder ("file://..." source) and demo Check-for-updates.
#
#   .\local-smoke-test.ps1
#
# Point the app's GitHub feed at a local folder for testing by editing
# Services/UpdateService.cs (GitHubRepoUrl) to a file:// path, OR just run
# setup.exe from Releases/ to see the installer work.
# ------------------------------------------------------------------------------

$ErrorActionPreference = "Stop"

param(
    [string]$Runtime = "win-x64"
)

$AppId     = "DevTemWinUi3"
$MainExe   = "DevTemWinUi3.exe"
$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

# --- v1.0.0 (first install) ---------------------------------------------
& dotnet publish (Join-Path $ProjectRoot "DevTemWinUi3.csproj") `
    -c Release -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=false `
    /p:PublishTrimmed=false `
    /p:WindowsAppSDKSelfContained=true `
    /p:EnableMsixTooling=true
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

$publishDir = Join-Path $ProjectRoot "bin\Release\net10.0-windows10.0.19041.0\$Runtime\publish"

& vpk pack -u $AppId -v 1.0.0 -p $publishDir `
    -o (Join-Path $ProjectRoot "Releases") `
    -c stable -r $Runtime -e $MainExe `
    --packTitle "DevTem-WinUI 3" --packAuthors "Fettah"
if ($LASTEXITCODE -ne 0) { throw "vpk pack v1.0.0 failed" }

# --- v1.0.1 (update to demo Check-for-updates) ----------------------------
# Simulate a changed version by bumping AssemblyVersion then re-packing.
& dotnet publish (Join-Path $ProjectRoot "DevTemWinUi3.csproj") `
    -c Release -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=false `
    /p:PublishTrimmed=false `
    /p:WindowsAppSDKSelfContained=true `
    /p:EnableMsixTooling=true `
    /p:Version=1.0.1 /p:AssemblyVersion=1.0.1.0 /p:FileVersion=1.0.1.0
if ($LASTEXITCODE -ne 0) { throw "publish 1.0.1 failed" }

& vpk pack -u $AppId -v 1.0.1 -p $publishDir `
    -o (Join-Path $ProjectRoot "ReleasesLocal") `
    -c stable -r $Runtime -e $MainExe `
    --packTitle "DevTem-WinUI 3" --packAuthors "Fettah"
if ($LASTEXITCODE -ne 0) { throw "vpk pack v1.0.1 failed" }

Write-Host ""
Write-Host "==> Local smoke-test complete." -ForegroundColor Green
Write-Host "   Install:  .\Releases\setup.exe"
Write-Host "   Update:   .\ReleasesLocal\  (point the app feed here for a local update demo)"
