dotnet ./Tools/Luban/Luban.dll \
    -t server \
    -d bin \
    -c cs-dotnet-bin \
    -x outputDataDir=../Server/FuFramework.Config/Json \
    -x outputCodeDir=../Server/FuFramework.Config/Config \
    -x tableImporter.name=fuframework \
    --conf ./Luban.conf
pause