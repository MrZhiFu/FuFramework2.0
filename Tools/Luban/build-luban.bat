@echo off
rem Build the Luban tool from source (Tools/Luban/source/src) into Tools/Luban/bin.
rem NOTE: keep this file ASCII-only (cmd.exe parses .bat in the system ANSI codepage).
cd /d "%~dp0"
rd /s /q bin
dotnet build source\src\Luban\Luban.csproj -c Release -o bin
pause
