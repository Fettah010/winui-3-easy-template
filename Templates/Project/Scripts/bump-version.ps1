# Bumps the app version across every file the release pipeline reads:
# the WinExe csproj pair (app + template mirror, keeps mirror parity
# green), CITATION.cff, and a CHANGELOG.md stub when the section is
# missing. The NuGet template package version stays independent
# (CI passes -p:Version from templates-v* tags).
#
#   powershell -File Scripts/bump-version.ps1 -Version 0.0.6-beta
#   powershell -File Scripts/bump-version.ps1 -Version 0.0.6-beta -WhatIf
#
# After bumping: review the CHANGELOG stub, commit, then tag
# (v0.0.6-beta for the app release).

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Date = ""
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+(-[A-Za-z0-9.]+)?$') {
    throw "Version '$Version' must look like 0.0.6 or 0.0.6-beta."
}
if ([string]::IsNullOrWhiteSpace($Date)) {
    $Date = Get-Date -Format "yyyy-MM-dd"
}
$plain = $Version -replace '-.*$', ''
$parts = @($plain.Split('.'))
while ($parts.Count -lt 4) { $parts += "0" }
$quad = $parts[0..3] -join "."
$assemblyQuad = $quad

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Get-WinExeProjects {
    $found = @()
    $roots = @($projectRoot)
    $nested = Join-Path $projectRoot "Templates\Project"
    if (Test-Path -LiteralPath $nested) { $roots += $nested }
    foreach ($root in $roots) {
        foreach ($csproj in (Get-ChildItem -LiteralPath $root -Filter "*.csproj" -File -ErrorAction SilentlyContinue)) {
            try {
                # .NET UTF-8 IO (BOM-aware) on purpose: bare Get-Content
                # decodes with the system codepage and a later Save re-encodes
                # as UTF-8, doubling any non-ASCII on every run (this once
                # grew the template csproj 5KB -> 148MB across releases).
                [xml]$xml = [System.IO.File]::ReadAllText($csproj.FullName)
                $outputType = $xml.Project.PropertyGroup |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_.OutputType) } |
                    Select-Object -First 1 -ExpandProperty OutputType
                if ($outputType -eq "WinExe") { $found += $csproj.FullName }
            }
            catch { }
        }
    }
    return $found
}

function Set-XmlProp([xml]$xml, [string]$name, [string]$value) {
    $group = $xml.Project.PropertyGroup |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_.$name) } |
        Select-Object -First 1
    if ($group) { $group.$name = $value; return $true }
    return $false
}

$csprojs = @(Get-WinExeProjects)
if ($csprojs.Count -eq 0) { throw "No WinExe csproj found under $projectRoot." }
foreach ($path in $csprojs) {
    if ($PSCmdlet.ShouldProcess($path, "Set version $Version")) {
        [xml]$xml = [System.IO.File]::ReadAllText($path)
        $touched = @()
        foreach ($pair in @( @("Version", $plain), @("AssemblyVersion", $assemblyQuad), @("FileVersion", $assemblyQuad), @("InformationalVersion", $Version) )) {
            if (Set-XmlProp $xml $pair[0] $pair[1]) { $touched += $pair[0] }
        }
        $xml.Save($path)
        Write-Host "Bumped ${path}: $($touched -join ', ')"
    }
}

$citation = Join-Path $projectRoot "CITATION.cff"
if (Test-Path -LiteralPath $citation) {
    if ($PSCmdlet.ShouldProcess($citation, "Set version/date")) {
        # Same UTF-8 rule as above: bare Set-Content writes ANSI here.
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        $text = [System.IO.File]::ReadAllText($citation, [System.Text.Encoding]::UTF8)
        $text = [regex]::Replace($text, '(?m)^version:.*$', 'version: "' + $Version + '"')
        $text = [regex]::Replace($text, '(?m)^date-released:.*$', 'date-released: "' + $Date + '"')
        [System.IO.File]::WriteAllText($citation, $text, $utf8NoBom)
        Write-Host "Bumped $citation"
    }
}
else { Write-Host "No CITATION.cff, skipping." }

$changelog = Join-Path $projectRoot "CHANGELOG.md"
if (Test-Path -LiteralPath $changelog) {
    $lines = @([System.IO.File]::ReadAllLines($changelog, [System.Text.Encoding]::UTF8))
    $escaped = [regex]::Escape($Version)
    if ($lines -match ('^## \[' + $escaped + '\]')) {
        Write-Host "CHANGELOG already has [$Version], leaving it."
    }
    elseif ($PSCmdlet.ShouldProcess($changelog, "Insert [$Version] stub")) {
        $stub = @("", "## [$Version] - $Date", "", "### Added", "", "- TBD", "")
        $firstSection = -1
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^## ') { $firstSection = $i; break }
        }
        if ($firstSection -lt 0) { $lines = @($lines) + $stub }
        elseif ($firstSection -eq 0) { $lines = @($stub) + @($lines) }
        else { $lines = @($lines[0..($firstSection - 1)]) + $stub + @($lines[$firstSection..($lines.Count - 1)]) }
        [System.IO.File]::WriteAllLines($changelog, $lines, (New-Object System.Text.UTF8Encoding($false)))
        Write-Host "Inserted CHANGELOG stub for [$Version] (fill in the notes, then tag)."
    }
}
else { Write-Host "No CHANGELOG.md, skipping." }
