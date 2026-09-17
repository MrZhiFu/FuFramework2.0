@echo off
rem Build Luban first if the binary is missing (fresh clone / other machine).
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
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/LanguageKey ^
    -x tableImporter.name=fuframework ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf

dotnet ../Tools/Luban/bin/Luban.dll ^
    -t aot ^
    -d bin ^
    -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Resources/Config ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/LanguageKey ^
    -x tableImporter.name=fuframework ^
    -x tableImporter.target=aot ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
pause