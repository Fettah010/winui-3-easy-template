# Creates a desktop shortcut to run DevTem-WinUI 3
# Run this script once to create the shortcut

$WshShell = New-Object -ComObject WScript.Shell
$Desktop = [System.Environment]::GetFolderPath("Desktop")
$Shortcut = $WshShell.CreateShortcut("$Desktop\DevTem-WinUI 3.lnk")
$Shortcut.TargetPath = "dotnet"
$Shortcut.Arguments = "run --project `"$PSScriptRoot\DevTemWinUi3.csproj`" -c Debug -p:Platform=x64"
$Shortcut.WorkingDirectory = $PSScriptRoot
$Shortcut.Description = "DevTem-WinUI 3 Template App"
$Shortcut.IconLocation = "$PSScriptRoot\Assets\app.ico"
$Shortcut.Save()

Write-Host "Desktop shortcut created: $Desktop\DevTem-WinUI 3.lnk" -ForegroundColor Green
