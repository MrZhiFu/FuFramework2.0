@echo off

rem ============================================================
rem  Server config generation script (binary variant: .bytes data, cs-dotnet-bin target)
rem
rem  What it does:
rem    1. Builds Luban automatically if Luban.dll is missing (fresh clone / new machine)
rem    2. Generates config data & code (see passes below)
rem  Usage: run under the Config/ directory; ends with a pause
rem  Note: rebuild Tools/Luban/build-luban.bat manually after changing Luban source
rem ============================================================

rem Luban.dll is built automatically if missing (fresh clone / other machine).
if not exist "..\Tools\Luban\bin\Luban.dll" (
    echo [Luban] bin/Luban.dll not found, building first ...
    call "..\Tools\Luban\build-luban.bat" ci
)

dotnet ../Tools/Luban/bin/Luban.dll ^
    -t server ^
    -d bin ^
    -c cs-dotnet-bin ^
    -x outputDataDir=../Server/FuFramework.Config/Json ^
    -x cs-dotnet-bin.outputCodeDir=../Server/FuFramework.Config/Config ^
    -x tableImporter.name=fuframework ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
pause
