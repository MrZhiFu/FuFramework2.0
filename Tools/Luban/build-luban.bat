@echo off
rem Build the Luban tool from source (Tools/Luban/source/src) into Tools/Luban/bin.
rem Pass any arg (e.g. "ci") to skip the trailing pause. Used by Config/gen-*.bat guard.
rem NOTE: keep this file ASCII-only (cmd.exe parses .bat in the system ANSI codepage).
cd /d "%~dp0"
rd /s /q bin
dotnet build source\src\Luban\Luban.csproj -c Release -o bin
if "%~1"=="" pause

