# -*- coding: utf-8 -*-
"""一次性迁移：把 InitUIEvent 方法（含事件注册代码）从 .Gen.cs 迁入手写 Win/Comp 文件。

背景：CSharpCodeGen 的 InitUIEvent 定义由 Gen 层移至手写层，存量手写文件（旧模板生成）
没有该方法定义，Gen 层重生成后会因 ConstructFromXML/OnInit 调用而编译失败。
本脚本从现存 .Gen.cs（仍含 InitUIEvent）提取方法体，插入手写文件"注册相关逻辑事件"
summary 之前，tab 缩进转 4 空格与手写文件风格统一。幂等：已有定义的文件自动跳过。
"""
import os
import re
import sys

GAME_UI = r"D:\_WorkSpace\Unity\FuFramework2.0\Unity\Assets\Scripts\Hotfix\Game\UI"
AUTOGEN = r"D:\_WorkSpace\Unity\FuFramework2.0\Unity\Assets\Scripts\Hotfix\Game\AutoGen\UI"


def read_text(path):
    with open(path, "r", encoding="utf-8-sig", newline="") as f:
        return f.read()


def write_text(path, content, has_bom):
    with open(path, "w", encoding="utf-8-sig" if has_bom else "utf-8", newline="") as f:
        f.write(content)


def extract_inituievent(gen_path):
    """从 .Gen.cs 提取 InitUIEvent 定义块（summary+方法，含配对花括号），tab 转 4 空格。"""
    if not os.path.exists(gen_path):
        return None, "no-gen-file"
    src = read_text(gen_path)
    sig = src.find("private void InitUIEvent()")
    if sig < 0:
        return None, "no-method-in-gen"
    seg_start = sig
    sum_pos = src.rfind("/// <summary>", 0, sig)
    if sum_pos >= 0:
        seg_start = sum_pos
        # 吃掉 summary 前的行首缩进
        line_start = src.rfind("\n", 0, seg_start) + 1
        seg_start = line_start
    brace = src.find("{", sig)
    if brace < 0:
        return None, "no-brace"
    depth = 0
    end = brace
    for i in range(brace, len(src)):
        if src[i] == "{":
            depth += 1
        elif src[i] == "}":
            depth -= 1
            if depth == 0:
                end = i
                break
    block = src[seg_start:end + 1].replace("\t", "    ").rstrip()
    return block, "ok"


def migrate(hand_path, gen_path):
    if not os.path.exists(hand_path):
        return "skip(no-hand-file)"
    src = read_text(hand_path)
    has_bom = open(hand_path, "rb").read(3) == b"\xef\xbb\xbf"
    if "private void InitUIEvent" in src:
        return "skip(already-has)"

    block, reason = extract_inituievent(gen_path)
    if block is None:
        return "skip(%s)" % reason

    anchor = "/// 注册相关逻辑事件"
    pos = src.find(anchor)
    if pos < 0:
        return "skip(no-anchor)"
    sum_pos = src.rfind("/// <summary>", 0, pos)
    if sum_pos < 0:
        return "skip(no-summary)"
    line_start = src.rfind("\n", 0, sum_pos) + 1

    new_src = src[:line_start] + block + "\n\n" + src[line_start:]
    write_text(hand_path, new_src, has_bom)
    return "migrated"


def main():
    results = []
    for pkg in sorted(os.listdir(GAME_UI)):
        pkg_dir = os.path.join(GAME_UI, pkg)
        if not os.path.isdir(pkg_dir):
            continue
        # 手写 Win：Game/UI/{pkg}/WinXxx.cs
        for name in sorted(os.listdir(pkg_dir)):
            if not name.endswith(".cs"):
                continue
            hand = os.path.join(pkg_dir, name)
            gen = os.path.join(AUTOGEN, pkg, name[:-3] + ".Gen.cs")
            results.append((os.path.relpath(hand, GAME_UI), migrate(hand, gen)))
        # 手写 Comp：Game/UI/{pkg}/Comp/CompXxx.cs
        comp_dir = os.path.join(pkg_dir, "Comp")
        if os.path.isdir(comp_dir):
            for name in sorted(os.listdir(comp_dir)):
                if not name.endswith(".cs"):
                    continue
                hand = os.path.join(comp_dir, name)
                gen = os.path.join(AUTOGEN, pkg, "Comp", name[:-3] + ".Gen.cs")
                results.append((os.path.relpath(hand, GAME_UI), migrate(hand, gen)))

    migrated = [p for p, r in results if r == "migrated"]
    skipped = [(p, r) for p, r in results if r != "migrated"]
    print("migrated: %d" % len(migrated))
    for p in migrated:
        print("  [OK] %s" % p)
    print("skipped: %d" % len(skipped))
    for p, r in skipped:
        print("  [--] %s (%s)" % (p, r))


if __name__ == "__main__":
    sys.exit(main())
