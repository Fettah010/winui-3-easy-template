# Creates the desktop shortcuts for DevTem-WinUI 3 (no terminal needed).
# Run once (re-run after moving the repo); both shortcuts are created:
#
#   DevTem-WinUI 3.lnk        fast launch: targets the built exe directly.
#                             Do NOT point it at run-dev.vbs/.bat (those run
#                             `dotnet run`, which re-evaluates the build and
#                             adds seconds to every launch). The app is WinExe:
#                             it never shows a terminal window on its own.
#   DevTem-WinUI 3 (Dev).lnk  dev loop: builds Debug/x64 if needed, then
#                             launches via Scripts\run-app.ps1 — double-click
#                             to build + run, no terminal typing needed.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Scripts\create-shortcut.ps1
#   powershell -File Scripts\create-shortcut.ps1 -WhatIf   # preview only

[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = "Stop"

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ExeRelative = "bin\x64\Debug\net10.0-windows10.0.19041.0\DevTemWinUi3.exe"
$ExePath = Join-Path $ProjectRoot $ExeRelative
$RunApp = Join-Path $ProjectRoot "Scripts\run-app.ps1"
$Icon = Join-Path $ProjectRoot "Assets\app.ico"

if (-not (Test-Path $ExePath)) {
    throw "Built exe not found at $ExePath. Run `dotnet build -c Debug -p:Platform=x64` first."
}

$WshShell = New-Object -ComObject WScript.Shell
$Desktop = [System.Environment]::GetFolderPath("Desktop")

function New-DesktopShortcut([string]$Name, [string]$Target, [string]$Arguments, [string]$WorkDir) {
    $Path = Join-Path $Desktop $Name
    if ($PSCmdlet.ShouldProcess($Path, "Create desktop shortcut")) {
        $Link = $WshShell.CreateShortcut($Path)
        $Link.TargetPath = $Target
        $Link.Arguments = $Arguments
        $Link.WorkingDirectory = $WorkDir
        $Link.Description = "DevTem-WinUI 3 Template App"
        $Link.IconLocation = "$Icon,0"
        $Link.Save()
        Write-Host "Desktop shortcut created: $Path" -ForegroundColor Green
    }
}

# Fast launch (no build, no terminal).
New-DesktopShortcut "DevTem-WinUI 3.lnk" $ExePath "" (Split-Path $ExePath)

# Dev loop (build if needed + launch, no terminal typing).
$PsCmd = Get-Command powershell.exe -ErrorAction SilentlyContinue
if ($PsCmd) { $PsExe = $PsCmd.Source }
else { $PsExe = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\powershell.exe" }
New-DesktopShortcut "DevTem-WinUI 3 (Dev).lnk" $PsExe "-NoProfile -ExecutionPolicy Bypass -File `"$RunApp`"" (Join-Path $ProjectRoot "Scripts")
