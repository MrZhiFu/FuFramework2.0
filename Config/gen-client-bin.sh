dotnet ./Tools/Luban.dll \
    -t client \
    -d bin \
    -c cs-bin \
    -x outputDataDir=../Unity/Assets//Bundles/Config \
    -x outputCodeDir=../Unity/Assets//Scripts/Hotfix/Config/Generate \
    -x tableImporter.name=gameframex \
    -x l10n.provider=gameframex \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    --conf ./Luban.conf
pause