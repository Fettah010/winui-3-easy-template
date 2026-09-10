# ------------------------------------------------------------------------------
# Upload an already-packed Releases/ folder to GitHub Releases.
# Use this when you packed locally (vpk pack) and just want to ship it.
#
#   .\upload-github.ps1 -Channel stable
#
# Requires: $env:GITHUB_TOKEN (PAT with repo-scope), vpk global tool.
# ------------------------------------------------------------------------------

param(
    [string]$Channel = "stable",
    [switch]$Publish,       # flip switch: create as published release instead of draft
    [switch]$PreRelease
)

$ErrorActionPreference = "Stop"

# ──────────────────────────────────────────────────────────────────────────────
#  PLACEHOLDER CREDENTIALS — replace with your real project values.
# ──────────────────────────────────────────────────────────────────────────────
$GithubRepoUrl = "https://github.com/YOUR_USERNAME/YOUR_REPOSITORY"
$GithubToken   = $env:GITHUB_TOKEN      # <- export GITHUB_TOKEN=<token> first
# ──────────────────────────────────────────────────────────────────────────────

$ProjectRoot  = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ReleasesDir  = Join-Path $ProjectRoot "Releases"

if ([string]::IsNullOrWhiteSpace($GithubToken)) {
    throw "GITHUB_TOKEN is not set. Run: `$env:GITHUB_TOKEN = 'ghp_...'"
}
if (-not (Test-Path $ReleasesDir)) {
    throw "No Releases folder found at $ReleasesDir. Run .\build-and-release.ps1 -SkipBuild first."
}

$args = @(
    "upload", "github",
    "--repoUrl", $GithubRepoUrl,
    "--token", $GithubToken,
    "-o", $ReleasesDir,
    "-c", $Channel
)
if ($Publish)    { $args += "--publish" }
if ($PreRelease) { $args += "--pre" }

Write-Host "==> Uploading $ReleasesDir to $GithubRepoUrl (channel: $Channel)" -ForegroundColor Cyan
& vpk @args
if ($LASTEXITCODE -ne 0) { throw "vpk upload github failed with exit code $LASTEXITCODE" }

Write-Host "==> Done." -ForegroundColor Green