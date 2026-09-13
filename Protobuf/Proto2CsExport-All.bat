@echo off
rem ============================================================
rem Full one-shot: export protos for BOTH client and server,
rem then regenerate the client proto message registry.
rem
rem Replaces the previous async version which used "start" and did
rem NOT regenerate the registry (leaving a stale message map).
rem Order matters: the registry script scans the EXPORTED client C#,
rem not the .proto files.
rem
rem Usage:
rem   Proto2CsExport-All.bat        interactive run, pauses at the end
rem   Proto2CsExport-All.bat ci     for CI / scripts, no pause
rem
rem Client-only, faster: Protobuf/Proto2CsExport-All.bat
rem
rem NOTE: keep this file ASCII-only. cmd.exe parses .bat in the system
rem ANSI codepage, so non-ASCII text breaks command parsing.
rem ============================================================
cd /d "%~dp0"

set "TOOLDIR=%~dp0..\Tools\ProtoExport\bin\Debug\net8.0"

echo [1/3] Exporting protos for CLIENT: protoc to Unity hotfix C# ...
pushd "%TOOLDIR%"
call dotnet ProtoExport.dll --mode unity --inputPath ./../../../../../Protobuf/Proto --outputPath ./../../../../../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Proto --namespaceName Hotfix.Game.Proto --isGenerateErrorCode true
set "ERR=%errorlevel%"
popd
if not "%ERR%"=="0" goto :failed

echo.
echo [2/3] Exporting protos for SERVER: protoc to server C# ...
pushd "%TOOLDIR%"
call dotnet ProtoExport.dll --mode server --inputPath ./../../../../../Protobuf/Proto --outputPath ./../../../../../Server/FuFramework.Proto/Proto --namespaceName FuFramework.Proto.Proto --isGenerateErrorCode true
set "ERR=%errorlevel%"
popd
if not "%ERR%"=="0" goto :failed

echo.
echo [3/3] Generating client proto message registry ...
python "%~dp0gen-proto-registry.py"
set "ERR=%errorlevel%"
if not "%ERR%"=="0" goto :failed

echo.
echo [DONE] Client C#, server C# and the client registry are updated.
echo        Commit the generated outputs, then let Unity reimport so the
echo        AutoGen .cs.meta files are regenerated.
if "%~1"=="" pause
exit /b 0

:failed
echo.
echo [ERROR] A step failed, exit %ERR%. Aborting: remaining steps were skipped.
if "%~1"=="" pause
exit /b %ERR%
