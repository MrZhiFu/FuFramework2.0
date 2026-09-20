@echo off

rem ============================================================
rem  Client config generation script (binary variant: .bytes data, cs-bin target)
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
    -t client ^
    -d bin ^
    -c cs-bin ^
    -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Bundles/Config ^
    -x cs-bin.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Extension ^
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false ^
    -x tableImporter.name=fuframework ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf

dotnet ../Tools/Luban/bin/Luban.dll ^
    -t aot ^
    -d bin ^
    -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Resources/LaunchLocalizationText ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen ^
    -x cs-l10n-key.className=LaunchL10nKey ^
    -c cs-enums ^
    -x cs-enums.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen ^
    -x outputSaver.cs-enums.cleanUpOutputDir=false ^
    -c cs-bean ^
    -x cs-bean.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen ^
    -x outputSaver.cs-bean.cleanUpOutputDir=false ^
    -x cs-bean.tables=TbLocalizationAOT ^
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false ^
    -x tableImporter.name=fuframework ^
    -x tableImporter.target=aot ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
pause
