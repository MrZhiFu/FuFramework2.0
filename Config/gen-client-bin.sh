
#============================================================
# Client config generation script (.bytes data, cs-bin target)
#
# What it does:
#   1. Builds Luban automatically if Luban.dll is missing (fresh clone / new machine)
#   2. Generates config data & code (see passes below)
# Usage: run under the Config/ directory; ends with a pause
# Note: rebuild Tools/Luban/build-luban.bat manually after changing Luban source
#============================================================

#Luban.dll is built automatically if missing (fresh clone / other machine).
if [ ! -f ../Tools/Luban/bin/Luban.dll ]; then
    echo "[Luban] bin/Luban.dll not found, building first ..."
    bash ../Tools/Luban/build-luban.sh
fi

dotnet ../Tools/Luban/bin/Luban.dll \
    -t client \
    -d bin \
    -c cs-bin \
    -c cs-l10n-key \
    -x outputDataDir=../Unity/Assets/Bundles/Config/Data \
    -x cs-bin.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate \
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Extension \
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false \
    -x tableImporter.name=fuframework \
    -x tableImporter.target=client \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    -x dataPostprocess=split-localization \
    --conf ./Luban.conf

dotnet ../Tools/Luban/bin/Luban.dll \
    -t aot \
    -d bin \
    -c cs-l10n-key \
    -x outputDataDir=../Unity/Assets/Resources/LaunchLocalizationText \
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen \
    -x cs-l10n-key.className=LaunchL10nKey \
    -c cs-enums \
    -x cs-enums.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen \
    -x outputSaver.cs-enums.cleanUpOutputDir=false \
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false \
    -x tableImporter.name=fuframework \
    -x tableImporter.target=aot \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    -x dataPostprocess=split-aot-localization \
    --conf ./Luban.conf

# Language registry (short standalone call): LanguageDef is a -c- table,
#   must be imported with client importer view; -o bypasses group filter to
#   include the table, -f loads table data without producing data files;
#   output goes to AOT AutoGen (AOT.Launch.Localization namespace)

dotnet ../Tools/Luban/bin/Luban.dll \
    -t aot \
    -c cs-language-registry \
    -o Tables.TbLanguageDef \
    -f \
    -x cs-language-registry.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen \
    -x outputSaver.cs-language-registry.cleanUpOutputDir=false \
    -x tableImporter.name=fuframework \
    -x tableImporter.target=client \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    --conf ./Luban.conf

python sync_localization_yooasset_collectors.py
