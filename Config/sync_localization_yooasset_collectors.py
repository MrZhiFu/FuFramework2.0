# -*- coding: utf-8 -*-
"""
sync_localization_yooasset_collectors.py — 按语言清单同步 YooAsset 收集器配置

功能：
    重写 Unity/Assets/AssetBundleCollectorSetting.asset 中 Config_Localization 组的
    Collectors 段：语言清单由生成产物（Bundles/Config/Localization/tblocalization_*.bytes）
    与配置源（__enums__.xlsx 的 ELanguage 成员）取交集驱动（**不 glob 目录**——残留文件会把
    已删语种加回来；目录残留由 Luban 拆分后处理负责清理）。
    幂等：语言集合不变时文件字节级不变。文件不存在/组缺失/语言清单为空 → 报错退出（不写文件）。

用法：
    python sync_localization_yooasset_collectors.py            # 同步
    python sync_localization_yooasset_collectors.py --check    # 只校验不一致（exit 1），不改文件
"""
import io
import os
import sys

if sys.stdout.encoding and sys.stdout.encoding.lower() not in ("utf-8", "utf8"):
    sys.stdout.reconfigure(encoding="utf-8")

import openpyxl

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSET_PATH = os.path.join(REPO_ROOT, "Unity", "Assets", "AssetBundleCollectorSetting.asset")
ENUMS_PATH = os.path.join(REPO_ROOT, "Config", "Excels", "__enums__.xlsx")
PRODUCT_DIR = os.path.join(REPO_ROOT, "Unity", "Assets", "Bundles", "Config", "Localization")

GROUP_NAME = "GroupName: Config_Localization"
COLLECT_TEMPLATE = [
    "      - CollectPath: Assets/Bundles/Config/Localization/tblocalization_{lang}.bytes",
    "        CollectorGUID: ",
    "        CollectorType: 0",
    "        AddressRuleName: AddressByFileName",
    "        PackRuleName: PackSeparately",
    "        FilterRuleName: CollectAll",
    "        AssetTags: {lang}",
    "        UserData: ",
]


def resolve_language_names():
    """语言清单 = ELanguage 枚举成员（除 Unspecified）∩ 拆分产物文件集合。"""
    wb = openpyxl.load_workbook(ENUMS_PATH, read_only=True)
    enum_names = set()
    for ws in wb.worksheets:
        in_e = False
        for row in ws.iter_rows(values_only=True):
            full_name = row[2] if len(row) > 2 else None
            if full_name == "ELanguage":
                in_e = True
                continue
            if in_e:
                if full_name:  # 下一个枚举开始
                    in_e = False
                    continue
                name = row[7] if len(row) > 7 else None
                if name and name != "Unspecified":
                    enum_names.add(str(name))
    wb.close()

    if not enum_names:
        raise SystemExit("[YooAsset多语言资源收集器更新] ELanguage 枚举成员解析为空，拒绝同步")

    products = set()
    for f in os.listdir(PRODUCT_DIR):
        if f.startswith("tblocalization_") and f.endswith(".bytes"):
            products.add(f[len("tblocalization_"):-len(".bytes")])

    langs = [n for n in sorted(enum_names) if n in products]
    missing = sorted(enum_names - products)
    if not langs:
        raise SystemExit("[YooAsset多语言资源收集器更新] 枚举与拆分产物交集为空，拒绝同步")
    if missing:
        print(f"[YooAsset多语言资源收集器更新] 警告：枚举成员缺少拆分产物（将不出现在收集器）：{missing}")
    return langs


def sync(check_only):
    print("==================== 更新YooAsset多语言资源收集器 ====================")
    langs = resolve_language_names()
    print(f"[YooAsset多语言资源收集器更新] 语言清单（{len(langs)}）：{langs}")

    with io.open(ASSET_PATH, encoding="utf-8", newline="") as f:
        lines = f.read().split("\n")

    # 定位组边界：GroupName 行起，至下一个 "    - GroupName:" 或 Packages 段结束（文件尾）
    start = None
    for i, line in enumerate(lines):
        if line.strip().startswith("- ") and line.strip()[2:] == GROUP_NAME.strip():
            start = i
            break
    if start is None:
        raise SystemExit(f"[YooAsset多语言资源收集器更新] 未找到分组 {GROUP_NAME}，拒绝同步")

    end = len(lines)
    for i in range(start + 1, len(lines)):
        if lines[i].strip().startswith("- GroupName:"):
            end = i
            break

    # 组头（GroupName..Collectors: 行）保留原样，仅替换 Collectors 列表
    header_end = start
    for i in range(start, end):
        if lines[i].strip() == "Collectors:":
            header_end = i + 1
            break
    else:
        raise SystemExit("[YooAsset多语言资源收集器更新] 组内未找到 Collectors: 行，拒绝同步")

    new_collectors = []
    for lang in langs:
        new_collectors.extend(t.format(lang=lang) for t in COLLECT_TEMPLATE)

    old_collectors = lines[header_end:end]
    if old_collectors == new_collectors:
        print("[YooAsset多语言资源收集器更新] 收集器已是最新，无变更")
        return

    if check_only:
        print("[YooAsset多语言资源收集器更新] 校验失败：收集器与语言清单不一致（跑一次同步或 gen 修复）")
        sys.exit(1)

    lines[header_end:end] = new_collectors
    with io.open(ASSET_PATH, "w", encoding="utf-8", newline="") as f:
        f.write("\n".join(lines))
    print(f"[YooAsset多语言资源收集器更新] 已重写 Config_Localization 组：{len(langs)} 个收集器")


if __name__ == "__main__":
    sync(check_only="--check" in sys.argv)
