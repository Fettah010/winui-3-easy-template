# ------------------------------------------------------------------------------
# Build + publish a full Velopack release and upload it to GitHub Releases.
#
#   .\build-and-release.ps1 -Version 1.0.1 -Channel stable -Runtime win-x64
#
# Prerequisites:
#   - .NET SDK 8+ (the project targets .NET 10)
#   - `vpk` global tool:   dotnet tool install --global vpk
#   - A GitHub token with `repo` scope set in $env:GITHUB_TOKEN
#
# This script is the single source of truth for releases: it is used both
# locally and by the GitHub Actions workflow (.github/workflows/release.yml).
#
# Output:
#   Releases/  containing setup.exe, *.nupkg update packages and releases feed.
# ------------------------------------------------------------------------------

param(
    [string]$Version = "1.0.1",
    [string]$Channel = "stable",
    [string]$Runtime = "win-x64",
    [string]$ReleaseNotes = "",   # optional path to a markdown notes file
    [switch]$SkipBuild,           # SKIP the dotnet publish step
    [switch]$Download,            # fetch the previous release so deltas are generated
    [switch]$Publish,             # create the GitHub release as published (not a draft)
    [switch]$PreRelease,          # mark the GitHub release as a pre-release
    [string]$Tag = ""             # git tag for the release (default: "v$Version")
)

$ErrorActionPreference = "Stop"

# ──────────────────────────────────────────────────────────────────────────────
#  PROJECT SETTINGS — the repo that hosts releases on GitHub.
#    GITHUB_TOKEN : export as env var (recommended) or hardcode below.
#                     https://github.com/settings/tokens  (repo scope)
#    VPK_REPO_URL : override the release repo (used by the CI workflow / forks).
# ──────────────────────────────────────────────────────────────────────────────
$GithubRepoUrl = $env:VPK_REPO_URL
if ([string]::IsNullOrWhiteSpace($GithubRepoUrl)) {
    $GithubRepoUrl = "https://github.com/Fettah010/winui-3-easy-template"
}
$GithubToken   = $env:GITHUB_TOKEN      # <- export GITHUB_TOKEN=<token> first
# ──────────────────────────────────────────────────────────────────────────────

$AppId     = "DevTemWinUi3"
$MainExe   = "DevTemWinUi3.exe"
$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$PublishDir  = Join-Path $ProjectRoot "bin\Release\net10.0-windows10.0.19041.0\$Runtime\publish"
$ReleasesDir = Join-Path $ProjectRoot "Releases"

Write-Host "==> Velopack release build" -ForegroundColor Cyan
Write-Host "    AppId     : $AppId"
Write-Host "    Version   : $Version"
Write-Host "    Channel   : $Channel"
Write-Host "    Runtime   : $Runtime"
Write-Host "    Repo      : $GithubRepoUrl"
Write-Host ""

# 1) Publish a self-contained, unpackaged build (no trimming, no single-file).
if (-not $SkipBuild) {
    Write-Host "==> dotnet publish (self-contained, WinUI + WinAppSDK bundled)" -ForegroundColor Cyan
    & dotnet publish (Join-Path $ProjectRoot "DevTemWinUi3.csproj") `
        -c Release -r $Runtime `
        --self-contained true `
        /p:PublishSingleFile=false `
        /p:PublishTrimmed=false `
        /p:WindowsAppSDKSelfContained=true `
        /p:EnableMsixTooling=true

    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
}

if (-not (Test-Path (Join-Path $PublishDir $MainExe))) {
    throw "Publish output not found at $PublishDir. Build did not produce $MainExe"
}

# 1.5) (Optional) download the previous release so `vpk pack` can build delta
#      packages. Failure here only means a full (larger) update is shipped.
if ($Download -and -not [string]::IsNullOrWhiteSpace($GithubToken)) {
    Write-Host "`n==> vpk download github (previous release for delta generation)" -ForegroundColor Cyan
    try {
        & vpk download github --repoUrl $GithubRepoUrl --token $GithubToken -o $ReleasesDir -c $Channel
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "vpk download exited with $LASTEXITCODE - continuing without deltas."
        }
    }
    catch {
        Write-Warning "vpk download failed: $_ - continuing without deltas."
    }
}

# 2) Pack the publish folder into a Velopack release (installer + deltas).
Write-Host "`n==> vpk pack" -ForegroundColor Cyan
$packArgs = @(
    "pack",
    "-u", $AppId,
    "-v", $Version,
    "-p", $PublishDir,
    "-o", $ReleasesDir,
    "-c", $Channel,
    "-r", $Runtime,
    "-e", $MainExe,
    "--packTitle", "DevTem-WinUI 3",
    "--packAuthors", "Fettah"
)
if (-not [string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    if (-not (Test-Path $ReleaseNotes)) { throw "Release notes file not found: $ReleaseNotes" }
    $packArgs += @("--releaseNotes", $ReleaseNotes)
}
& vpk @packArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed with exit code $LASTEXITCODE" }

# 3) Upload the release to GitHub (draft by default; add -Publish to flip).
Write-Host "`n==> vpk upload github" -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($GithubToken)) {
    Write-Warning "GITHUB_TOKEN is not set - skipping upload. Release is ready in $ReleasesDir"
    Write-Host "Run .\upload-github.ps1 after exporting GITHUB_TOKEN." -ForegroundColor Yellow
    return
}

$uploadArgs = @(
    "upload", "github",
    "--repoUrl", $GithubRepoUrl,
    "--token", $GithubToken,
    "-o", $ReleasesDir,
    "-c", $Channel,
    "--releaseName", "DevTem-WinUI 3 $Version"
)
if ($Publish)    { $uploadArgs += "--publish" }
if ($PreRelease) { $uploadArgs += "--pre" }
if ([string]::IsNullOrWhiteSpace($Tag)) { $Tag = "v$Version" }
$uploadArgs += @("--tag", $Tag)
& vpk @uploadArgs
if ($LASTEXITCODE -ne 0) { throw "vpk upload github failed with exit code $LASTEXITCODE" }

Write-Host "`n==> Done. Release $Version published to $GithubRepoUrl" -ForegroundColor Green