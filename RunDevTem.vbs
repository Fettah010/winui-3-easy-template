Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "cmd /c cd /d ""C:\Code\projects\DevEcosystem\DevTem WinUI 3"" && dotnet run -c Debug -p:Platform=x64", 0, False
