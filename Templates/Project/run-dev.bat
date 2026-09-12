@echo off
cd /d "%~dp0"
dotnet run -c Debug -p:Platform=x64
