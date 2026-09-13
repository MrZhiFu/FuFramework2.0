if [ ! -f ../Tools/Luban/bin/Luban.dll ]; then
    echo "[Luban] bin/Luban.dll not found, building first ..."
    bash ../Tools/Luban/build-luban.sh
fi

dotnet ../Tools/Luban/bin/Luban.dll \
    -t client \
    -d bin \
    -c cs-bin \
    -c cs-l10n-key \
    -x outputDataDir=../Unity/Assets/Bundles/Config \
    -x cs-bin.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate \
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/LanguageKey \
    -x tableImporter.name=fuframework \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    --conf ./Luban.conf
pause