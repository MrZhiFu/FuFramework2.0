dotnet.exe ./Tools/Luban/Luban.dll ^
    -t server ^
    -d bin ^
    -c cs-bin ^
    -x outputDataDir=../Server/GameFrameX.Config/Json ^
    -x outputCodeDir=../Server/GameFrameX.Config/Config ^
    -x tableImporter.name=fuframework ^
    --conf ./Luban.conf
pause