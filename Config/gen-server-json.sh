# ============================================================
#  服务端配置表生成脚本（json 变体：数据 .json，cs-dotnet-json 目标）
#
#  功能：
#    1. 检测 Luban.dll 缺失时自动先构建（换机 / 新 clone 后无需手动构建）
#    2. server 段：全部业务表与本地化表（服务端视角）
#       → 数据产物 Server/FuFramework.Config/Json/
#       → 代码产物 Server/FuFramework.Config/Config/（表与 bean，cs-dotnet-* 目标）
#  用法：在 Config/ 目录下运行：bash gen-xxx.sh
#  注意：改动 Luban 源码后需先手动跑 Tools/Luban/build-luban.sh 重建
# ============================================================

# Luban.dll 缺失时先自动构建（换机 / 新 clone 后无需手动构建）。

if [ ! -f ../Tools/Luban/bin/Luban.dll ]; then
    echo "[Luban] bin/Luban.dll 未找到，先自动构建 ..."
    bash ../Tools/Luban/build-luban.sh
fi

dotnet ../Tools/Luban/bin/Luban.dll \
    -t server \
    -d json \
    -c cs-dotnet-json \
    -x outputDataDir=../Server/FuFramework.Config/Json \
    -x outputCodeDir=../Server/FuFramework.Config/Config \
    -x tableImporter.name=fuframework \
    --conf ./Luban.conf
