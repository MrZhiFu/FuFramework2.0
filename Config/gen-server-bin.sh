if [ ! -f ../Tools/Luban/bin/Luban.dll ]; then
    echo "[Luban] bin/Luban.dll not found, building first ..."
    bash ../Tools/Luban/build-luban.sh
fi

dotnet ../Tools/Luban/bin/Luban.dll \
    -t server \
    -d bin \
    -c cs-dotnet-bin \
    -x outputDataDir=../Server/FuFramework.Config/Json \
    -x outputCodeDir=../Server/FuFramework.Config/Config \
    -x tableImporter.name=fuframework \
    --conf ./Luban.conf
pause