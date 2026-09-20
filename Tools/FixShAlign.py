import io
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
base = r'D:/_WorkSpace/Unity/FuFramework2.0/Config/'
BATS = ['gen-client-bin.bat', 'gen-client-json.bat', 'gen-server-bin.bat', 'gen-server-json.bat']

for bat_name in BATS:
    sh_name = bat_name[:-4] + '.sh'
    with open(base + bat_name, encoding='ascii') as f:
        bat_lines = f.read().replace('\r\n', '\n').split('\n')

    out = []
    in_if = False
    for line in bat_lines:
        stripped = line.strip()

        # @echo off：sh 不需要
        if stripped == '@echo off':
            continue

        # if not exist 块 → bash 语法
        if stripped.startswith('if not exist'):
            out.append('if [ ! -f ../Tools/Luban/bin/Luban.dll ]; then')
            in_if = True
            continue
        if stripped == ')':
            out.append('fi')
            in_if = False
            continue
        if in_if and stripped.startswith('call '):
            out.append('    bash ../Tools/Luban/build-luban.sh')
            continue
        if in_if and stripped.startswith('echo '):
            out.append('    echo "[Luban] bin/Luban.dll not found, building first ..."')
            continue

        # rem 注释 → # 注释
        if stripped.startswith('rem '):
            out.append('#' + line[line.index('rem') + 4:])
            continue
        if stripped == 'rem':
            out.append('#')
            continue

        # pause：sh 无此概念
        if stripped == 'pause':
            continue

        # ^ 续行符 → \
        if stripped.endswith('^'):
            out.append(line[:line.rindex('^')].rstrip() + ' \\')
            continue

        # Usage 行：pause 改为 bash 调用说明
        if 'ends with a pause' in line:
            out.append(line.replace('ends with a pause', 'run via: bash ' + sh_name))
            continue

        out.append(line)

    with open(base + sh_name, 'w', encoding='ascii', newline='\n') as f:
        f.write('\n'.join(out) + '\n')
    print(sh_name, 'SYNCED')

# 校验：bat 与 sh 的参数行逐行一致（剥前缀后）
for bat_name in BATS:
    sh_name = bat_name[:-4] + '.sh'
    with open(base + bat_name, encoding='ascii') as f:
        bat_args = [l.strip() for l in f if l.strip().startswith(('-t ', '-d ', '-c ', '-x ', '--conf'))]
    with open(base + sh_name, encoding='ascii') as f:
        sh_args = [l.strip() for l in f if l.strip().startswith(('-t ', '-d ', '-c ', '-x ', '--conf'))]
    status = 'ARGS-MATCH' if bat_args == sh_args else 'ARGS-MISMATCH'
    print(sh_name, status, f'({len(bat_args)}/{len(sh_args)})')
