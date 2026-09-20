import io
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
base = r'D:/_WorkSpace/Unity/FuFramework2.0/Config/'

# 命令段（ASCII 字面量，含最新的 AutoGen outputCodeDir）
CLIENT_PASS = '''dotnet ../Tools/Luban/bin/Luban.dll ^
    -t client ^
    -d {data} ^
    -c {code} ^
    -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Bundles/Config ^
    -x {code}.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Generate ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/Extension ^
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false ^
    -x tableImporter.name=fuframework ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf

dotnet ../Tools/Luban/bin/Luban.dll ^
    -t aot ^
    -d {data} ^
    -c cs-l10n-key ^
    -x outputDataDir=../Unity/Assets/Resources/LaunchLocalizationText ^
    -x cs-l10n-key.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen ^
    -x cs-l10n-key.className=LaunchL10nKey ^
    -c cs-enums ^
    -x cs-enums.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen ^
    -x outputSaver.cs-enums.cleanUpOutputDir=false ^
    -c cs-bean ^
    -x cs-bean.outputCodeDir=../Unity/Assets/Scripts/AOT/Launch/Localization/AutoGen ^
    -x outputSaver.cs-bean.cleanUpOutputDir=false ^
    -x outputSaver.cs-l10n-key.cleanUpOutputDir=false ^
    -x tableImporter.name=fuframework ^
    -x tableImporter.target=aot ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
'''

SERVER_PASS = '''dotnet ../Tools/Luban/bin/Luban.dll ^
    -t server ^
    -d {data} ^
    -c {code} ^
    -x outputDataDir=../Server/FuFramework.Config/Json ^
    -x {code}.outputCodeDir=../Server/FuFramework.Config/Config ^
    -x tableImporter.name=fuframework ^
    -x l10n.provider=fuframework ^
    -x l10n.textFile.keyFieldName=key ^
    -x l10n.textFile.path=./Excels/Local/ ^
    --conf ./Luban.conf
'''

scripts = {
    'gen-client-bin.bat': (
        'Client config generation script (binary variant: .bytes data, cs-bin target)',
        CLIENT_PASS.format(data='bin', code='cs-bin')),
    'gen-client-json.bat': (
        'Client config generation script (json variant: .json data, cs-simple-json target)',
        CLIENT_PASS.format(data='json', code='cs-simple-json')),
    'gen-server-bin.bat': (
        'Server config generation script (binary variant: .bytes data, cs-dotnet-bin target)',
        SERVER_PASS.format(data='bin', code='cs-dotnet-bin')),
    'gen-server-json.bat': (
        'Server config generation script (json variant: .json data, cs-dotnet-json target)',
        SERVER_PASS.format(data='json', code='cs-dotnet-json')),
}

for name, (title, passes) in scripts.items():
    content = (
        '@echo off\n'
        '\n'
        'rem ============================================================\n'
        f'rem  {title}\n'
        'rem\n'
        'rem  What it does:\n'
        'rem    1. Builds Luban automatically if Luban.dll is missing (fresh clone / new machine)\n'
        'rem    2. Generates config data & code (see passes below)\n'
        'rem  Usage: run under the Config/ directory; ends with a pause\n'
        'rem  Note: rebuild Tools/Luban/build-luban.bat manually after changing Luban source\n'
        'rem ============================================================\n'
        'rem Luban.dll is built automatically if missing (fresh clone / other machine).\n'
        'if not exist "..\\Tools\\Luban\\bin\\Luban.dll" (\n'
        '    echo [Luban] bin/Luban.dll not found, building first ...\n'
        '    call "..\\Tools\\Luban\\build-luban.bat" ci\n'
        ')\n'
        '\n'
        + passes +
        'pause\n'
    )
    with open(base + name, 'w', encoding='ascii', newline='') as f:
        f.write(content.replace('\n', '\r\n'))
    print(name, 'REWRITTEN')
