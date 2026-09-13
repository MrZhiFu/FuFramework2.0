@echo off
rem ============================================================
rem Generate the proto message registry:
rem   Unity/Assets/Scripts/Hotfix/Framework/Network/Generated/ProtoMessageRegistry.g.cs
rem
rem Run this AFTER exporting protos (Protobuf/Proto2CsExport_Client.bat), or
rem just use the one-shot Protobuf/Proto2CsExport-All.bat which does both in the right order.
rem Also required after adding or removing [MessageHandler] methods.
rem
rem Usage:
rem   gen-proto-registry.bat        interactive run, pauses at the end
rem   gen-proto-registry.bat ci     for CI / scripts, no pause
rem
rem NOTE: keep this file ASCII-only. cmd.exe parses .bat in the system
rem ANSI codepage, so non-ASCII text breaks command parsing.
rem ============================================================
cd /d "%~dp0"
python gen-proto-registry.py
set "REG_ERR=%errorlevel%"
if "%~1"=="" pause
exit /b %REG_ERR%
