@echo off
rem Build Luban first if the binary is missing (fresh clone / other machine).
if not exist "..\Tools\Luban\bin\Luban.dll" (
    echo [Luban] bin/Luban.dll not found, building first ...
    call "..\Tools\Luban\build-luban.bat" ci
)

dotnet ../Tools/Luban/bin/Luban.dll ^
    -t server ^
    -d json ^
    -c cs-dotnet-json ^
    -x outputDataDir=../Server/FuFramework.Config/Json ^
    -x outputCodeDir=../Server/FuFramework.Config/Config ^
    -x tableImporter.name=fuframework ^
    --conf ./Luban.conf
pause