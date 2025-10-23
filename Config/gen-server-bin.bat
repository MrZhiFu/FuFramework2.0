dotnet.exe ./Tools/Luban.dll ^
    -t server ^
    -d bin ^
    -c cs-dotnet-bin ^
    -x outputDataDir=../Server/GameFrameX.Config/Json ^
    -x outputCodeDir=../Server/GameFrameX.Config/Config ^
    -x tableImporter.name=gameframex ^
    --conf ./Luban.conf
pause