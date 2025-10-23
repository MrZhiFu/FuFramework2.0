dotnet.exe ./Tools/Luban.dll ^
    -t server ^
    -d json ^
    -c cs-dotnet-json ^
    -x outputDataDir=../Server/GameFrameX.Config/Json ^
    -x outputCodeDir=../Server/GameFrameX.Config/Config ^
    -x tableImporter.name=gameframex ^
    --conf ./Luban.conf
pause