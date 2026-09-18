# ============================================================
#  客户端配置表生成脚本（bin 二进制变体：数据 .bytes，cs-bin 目标）
#
#  功能：
#    1. 检测 Luban.dll 缺失时自动先构建（换机 / 新 clone 后无需手动构建）
#    2. client 段：全部业务表与本地化表
#       → 数据产物 Unity/Assets/Bundles/Config/
#       → 代码产物 Hotfix 表代码 + Tables/Extension/L10nKey 常量类
#    3. aot 段：AOT 前置本地化（热更前文案）
#       → 数据产物 Unity/Assets/Resources/LaunchLocalizationText/
#       → 代码产物 AOT/Launch/Localization 的 LaunchL10nKey / ELanguage
#  用法：在 Config/ 目录下运行：bash gen-xxx.sh
#  注意：改动 Luban 源码后需先手动跑 Tools/Luban/build-luban.sh 重建
# ============================================================

# Luban.dll 缺失时先自动构建（换机 / 新 clone 后无需手动构建）。
if [ ! -f ../Tools/Luban/bin/Luban.dll ]; then
    echo "[Luban] bin/Luban.dll 未找到，先自动构建 ..."
    bash ../Tools/Luban/build-luban.sh
fi

dotnet ../Tools/Luban/bin/Luban.dll \
    -t client \
    -d bin \
    -c cs-bin \
    -c cs-l10n-key \
    -x outputDataDir=../Unity/Assets/Bundles/Config \
    -x cs-bin.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate \
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Extension \
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false \
    -x tableImporter.name=fuframework \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    --conf ./Luban.conf
    
dotnet ../Tools/Luban/bin/Luban.dll \
    -t aot \
    -d bin \
    -c cs-l10n-key \
    -x outputDataDir=../Unity/Assets/Resources/LaunchLocalizationText \
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization \
    -x cs-l10n-key.className=LaunchL10nKey \
    -c cs-enums \
    -x cs-enums.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization \
    -x outputSaver.cs-enums.cleanUpOutputDir=false \
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false \
    -x tableImporter.name=fuframework \
    -x tableImporter.target=aot \
    -x l10n.provider=fuframework \
    -x l10n.textFile.keyFieldName=key \
    -x l10n.textFile.path=./Excels/Local/ \
    --conf ./Luban.conf
