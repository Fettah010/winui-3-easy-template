# Creates a desktop shortcut to run DevTem-WinUI 3
# Run this script once to create the shortcut

$WshShell = New-Object -ComObject WScript.Shell
$Desktop = [System.Environment]::GetFolderPath("Desktop")
$Shortcut = $WshShell.CreateShortcut("$Desktop\DevTem-WinUI 3.lnk")
$Shortcut.TargetPath = "C:\Code\projects\DevEcosystem\DevTem WinUI 3\RunDevTem.bat"
$Shortcut.WorkingDirectory = "C:\Code\projects\DevEcosystem\DevTem WinUI 3"
$Shortcut.Description = "DevTem-WinUI 3 Template App"
$Shortcut.IconLocation = "C:\Code\projects\DevEcosystem\DevTem WinUI 3\Assets\app.ico,0"
$Shortcut.Save()

Write-Host "Desktop shortcut created: $Desktop\DevTem-WinUI 3.lnk" -ForegroundColor Green
