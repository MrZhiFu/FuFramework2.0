dotnet ./Tools/Luban/Luban.dll \
    -t client \
    -d json \
    -c cs-simple-json \
    -c cs-l10n-key \
    -x outputDataDir=../Unity/Assets/Bundles/Config \
    -x cs-simple-json.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Config/Generate \
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Config/LanguageKey \
    -x tableImporter.name=fuframework \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    --conf ./Luban.conf
pause