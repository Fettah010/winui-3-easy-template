# Creates a desktop shortcut to run DevTem-WinUI 3 (silently, no terminal)
# Run this script once to create the shortcut
#
# NOTE: the shortcut targets the built exe directly. Do NOT point it at
# run-dev.vbs/.bat (those run `dotnet run`, which re-evaluates the build and
# adds several seconds to every launch). The app is WinExe: it never shows a
# terminal window on its own.

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$ExeRelative = "bin\x64\Debug\net10.0-windows10.0.19041.0\DevTemWinUi3.exe"
$ExePath = Join-Path $ProjectRoot $ExeRelative

if (-not (Test-Path $ExePath)) {
    throw "Built exe not found at $ExePath. Run `dotnet build -c Debug -p:Platform=x64` first."
}

$WshShell = New-Object -ComObject WScript.Shell
$Desktop = [System.Environment]::GetFolderPath("Desktop")
$Shortcut = $WshShell.CreateShortcut("$Desktop\DevTem-WinUI 3.lnk")
$Shortcut.TargetPath = $ExePath
$Shortcut.WorkingDirectory = Split-Path $ExePath
$Shortcut.Description = "DevTem-WinUI 3 Template App"
$Shortcut.IconLocation = (Join-Path $ProjectRoot "Assets\app.ico") + ",0"
$Shortcut.Save()

Write-Host "Desktop shortcut created: $Desktop\DevTem-WinUI 3.lnk" -ForegroundColor Green
