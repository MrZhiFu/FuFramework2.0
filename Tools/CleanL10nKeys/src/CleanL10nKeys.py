#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
CleanL10nKeys - Multi-language table unreferenced-key cleaner.

Scans four kinds of reference sources and finds keys in the localization
Excel tables that are referenced by NONE of them:

  1. Code (Assets/Scripts/**/*.cs): static class field refs (L10nKey.xxx /
     LaunchL10nKey.xxx) AND raw string literals (covers manual
     GetLanguageText("xxx") calls).
  2. Config data (Bundles/Config/*.json): covers table-driven keys consumed
     via TableManager.Translate.
  3. FGUI sources (FairyGUIProject/assets/**/*.xml): customData "L10n:"
     segments (simple mode / controller mode "|"-composed).

Match semantics: whole-token match on [A-Za-z0-9_]+ (prefix collisions like
common_user vs common_user_name are impossible). Known blind spot: dynamic
key composition ("common_" + x) is statically undetectable -- the report
warns about it, human review is the final gate.

Usage:
  python Tools/CleanL10nKeys.py             # preview report only
  python Tools/CleanL10nKeys.py --apply     # delete unreferenced rows from the
                                            # Excel tables (re-export is manual:
                                            # run gen-client-*.bat afterwards)
"""
import re
import sys
from pathlib import Path

import openpyxl

REPO_ROOT      = Path(__file__).resolve().parents[3]             # repo root (src/ -> tool dir -> Tools -> repo)
CODE_DIR       = REPO_ROOT / 'Unity/Assets/Scripts'              # all runtime code (AOT + Hotfix)
CONFIG_DIR     = REPO_ROOT / 'Unity/Assets/Bundles/Config'       # exported config json
FGUI_DIR       = REPO_ROOT / 'FairyGUIProject/assets'            # FGUI source xml
EXCEL_FILES    = [
    REPO_ROOT / 'Config/Excels/Local/L-Localization-通用.xlsx',
    REPO_ROOT / 'Config/Excels/Local/L-Localization-成就.xlsx',
    REPO_ROOT / 'Config/Excels/Local/L-Localization-设置.xlsx',
    REPO_ROOT / 'Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx',
]

TOKEN_RE = re.compile(r'[A-Za-z0-9_]+')
KEY_RE   = re.compile(r'^[A-Za-z0-9_]+$')

# Self-reference exclusions: these files are DERIVED from the tables being
# cleaned, scanning them would make every key look referenced forever:
#   - tblocalization*.json      exported data of the localization tables themselves
#   - L10nKey.cs/LaunchL10nKey.cs  generated key classes listing every is_code key
EXCLUDED_JSON_RE = re.compile(r'^tblocalization.*\.json$')
EXCLUDED_CS      = {'L10nKey.cs', 'LaunchL10nKey.cs'}


def collect_reference_tokens():
    """Collect every [A-Za-z0-9_]+ token from all reference sources into one set.
    Derived files (see exclusions above) are silently skipped."""
    tokens = set()

    def feed(path):
        tokens.update(TOKEN_RE.findall(path.read_text(encoding='utf-8', errors='ignore')))

    for path in CODE_DIR.rglob('*.cs'):
        if path.name in EXCLUDED_CS:
            continue
        feed(path)
    for path in CONFIG_DIR.glob('*.json'):
        if EXCLUDED_JSON_RE.match(path.name):
            continue
        feed(path)
    for path in FGUI_DIR.rglob('*.xml'):
        feed(path)

    return tokens


def read_table(path):
    """Read keys from one Luban table. Returns list of dicts:
    {row, key, is_code, filled_langs, total_langs}. Header rows (##var/##type/##)
    are skipped by column-name lookup on row 1, data rows start at row 4."""
    wb = openpyxl.load_workbook(path, read_only=True)
    ws = wb.worksheets[0]
    rows = list(ws.iter_rows(values_only=True))
    wb.close()

    header = [str(c).strip() if c is not None else '' for c in rows[0]]
    key_col = header.index('key')

    entries = []
    for idx, row in enumerate(rows[3:], start=4):   # data starts at Excel row 4
        if key_col >= len(row):
            continue
        raw = row[key_col]
        key = str(raw).strip() if raw is not None else ''
        if not key or not KEY_RE.match(key):
            continue                                 # blank / Luban comment row (#xxx)
        entries.append({'row': idx, 'key': key})
    return entries


def main():
    # Console (tty) keeps the system code page (GBK cmd windows); piped output
    # (Unity editor captures stdout) switches to UTF-8 as both ends agreed.
    if not sys.stdout.isatty():
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    else:
        sys.stdout.reconfigure(errors='replace')
    do_apply = '--apply' in sys.argv[1:]

    print('=' * 70)
    print('多语言 key 清理工具')
    print(f'模式：{"执行（删除 Excel 中未引用行）" if do_apply else "预览（仅输出报告，不修改 Excel）"}')
    print('=' * 70)

    tokens = collect_reference_tokens()

    all_unreferenced = []   # (excel_path, entry)
    for excel in EXCEL_FILES:
        if not excel.exists():
            print(f'[跳过] 表不存在：{excel.name}')
            continue
        entries = read_table(excel)
        unreferenced = [e for e in entries if e['key'] not in tokens]
        if not unreferenced:
            continue                                   # 只输出有可清理项的表
        print(f'\n【{excel.name}】未引用 {len(unreferenced)} 个：')
        for e in unreferenced:
            print(f'    {e["key"]}')
            all_unreferenced.append((excel, e))

    if not all_unreferenced:
        print('\n所有 key 均被引用，无需清理。')
        return

    print()
    print('-' * 70)
    print(f'合计待清理：{len(all_unreferenced)} 个 key，涉及 '
          f'{len({str(p) for p, _ in all_unreferenced})} 张表')
    print('警告：动态拼接的 key（如 "common_" + x）无法静态检测，执行前请人工核对清单；')
    print('      Excel 由 git 管理，误删可回退。')
    print('-' * 70)

    if not do_apply:
        print('当前为预览，未做任何修改。加 --apply 参数（或 Unity 菜单「清理多语言配置表—执行」）执行删除。')
        return

    # ---- apply: delete rows per table (bottom-up so indices stay valid) ----
    by_table = {}
    for excel, e in all_unreferenced:
        by_table.setdefault(excel, []).append(e)

    deleted = 0
    for excel, entries in by_table.items():
        wb = openpyxl.load_workbook(excel)
        ws = wb.worksheets[0]
        for e in sorted(entries, key=lambda x: x['row'], reverse=True):
            ws.delete_rows(e['row'])
        wb.save(excel)
        deleted += len(entries)
        print(f'[已删除] {excel.name}：{len(entries)} 行')
    print(f'[已删除] 合计 {deleted} 行')

    # Re-export is deliberately manual: run gen-client-json.bat / gen-client-bin.bat
    # (Unity menu "FuFramework/Config/...") whenever you choose, so the deletion and
    # the export stay reviewable as separate steps.
    print('注意：本次仅修改了 Excel。请执行 gen-client-json.bat / gen-client-bin.bat')
    print('      （或 Unity 菜单「FuFramework/配置表/导出配置表」）重新导表。')

    print('完成。')


if __name__ == '__main__':
    main()
