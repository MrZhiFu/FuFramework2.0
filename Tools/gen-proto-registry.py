#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen-proto-registry.py
=====================

扫描协议生成物与热更源码，产出「协议消息注册表」（纯静态、零运行时反射）。

产出文件：
    Unity/Assets/Scripts/Hotfix/Framework/Network/Generated/ProtoMessageRegistry.g.cs

扫描来源：
    1. <Unity>/Assets/Scripts/Hotfix/Game/AutoGen/Proto/*.cs
       -> [MessageTypeHandler(消息ID)] + 类名 + 是否实现 IRequestMessage / IResponseMessage /
          INotifyMessage / IHeartBeatMessage
    2. <Unity>/Assets/Scripts/Hotfix/**/*.cs
       -> [MessageHandler(typeof(消息类型), nameof(方法名))]（用户消息处理方法）
          连同**所在类型**与方法可见性一起解析，产出**直接委托**：
              new ProtoMessageHandlerMethod(typeof(X), "OnX",
                  static (handler, message) => ((Ns.Handler)handler).OnX((X)message))
          可见性契约：目标方法必须为 internal 或 public，否则**报错并中止生成**
          （生成物位于同程序集，需要能直接调用该方法）。
       -> 实现 IMessageHandler 的类型（用于区分「已登记但无处理方法」与「未登记」）
    3. <Unity>/Assets/Scripts/Hotfix/**/*.cs（排除 Framework/Network/Helper/）
       -> 框架外「具体（非抽象）」的包处理器实现：实现 IPacketReceiveHeaderHandler /
          IPacketReceiveBodyHandler / IPacketSendHeaderHandler / IPacketSendBodyHandler /
          IPacketHeartBeatHandler / IMessageCompressHandler / IMessageDecompressHandler，
          且（与原扫描一致）实现标记接口 IPacketHandler。
          接口判定包含继承链，因此「派生自框架基类」的实现（如
          Hotfix.Game.Network.DefaultPacketHeartBeatHandler : BasePacketHeartBeatHandler）
          也能被正确识别并注册为心跳处理器（后注册覆盖框架基类）。

背景（项目铁律 4：运行时杜绝反射）：
    原实现于启动时通过 Assembly.GetTypes() 扫描全部已加载程序集，读取特性构建
    「消息ID <-> 类型」映射，并用 MakeGenericMethod + CreateDelegate 构造强类型委托；
    用户 [MessageHandler] 方法靠 GetMethods + GetCustomAttribute 发现；
    包处理器同样靠扫描 + Activator.CreateInstance 发现。本脚本把这些工作全部前移到生成期，
    产出静态注册表与直接委托，运行时只做字典写入 / 绑定 / 显式装配，**零反射**。

本脚本可重复执行（幂等覆盖生成物）。proto 变更（新增/删除消息、改 ID、改接口）、
新增 [MessageHandler] 方法、新增或调整框架外包处理器后，必须重新运行：
    Windows : Tools/gen-proto-registry.bat
    macOS/Linux : bash Tools/gen-proto-registry.sh
"""

from __future__ import annotations

import os
import re
import sys

# ---------------------------------------------------------------------------
# 路径（相对脚本自身定位仓库根目录，避免依赖调用方 cwd）
# ---------------------------------------------------------------------------

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.dirname(SCRIPT_DIR)

UNITY_ROOT = os.path.join(REPO_ROOT, "Unity")
HOTFIX_ROOT = os.path.join(UNITY_ROOT, "Assets", "Scripts", "Hotfix")
PROTO_DIR = os.path.join(HOTFIX_ROOT, "Game", "AutoGen", "Proto")
NETWORK_HELPER_DIR = os.path.join(HOTFIX_ROOT, "Framework", "Network", "Helper")
OUTPUT_PATH = os.path.join(
    HOTFIX_ROOT, "Framework", "Network", "Generated", "ProtoMessageRegistry.g.cs"
)
OUTPUT_REL = os.path.relpath(OUTPUT_PATH, REPO_ROOT).replace("\\", "/")

# 生成物中消息类型所使用的命名空间（由 Protobuf/Proto2CsExport_Client.* 的 --namespaceName 决定）。
# 这里从 proto 文件里实际解析，解析不到时回退到该默认值。
DEFAULT_PROTO_NAMESPACE = "Hotfix.Game.Proto"

# ---------------------------------------------------------------------------
# 正则
# ---------------------------------------------------------------------------

# [MessageTypeHandler(6553610)] 之后紧跟 public sealed class X : Base, IRequestMessage
RE_MSG_TYPE_ATTR = re.compile(r"\[\s*MessageTypeHandler\s*\(\s*(\d+)\s*\)\s*\]")
RE_CLASS_DECL = re.compile(
    r"^\s*(?:public|internal|sealed|abstract|static|\s)*\b(?:class|struct)\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*([^\r\n{]+)",
    re.MULTILINE,
)

# 全量类声明（基类列表可选），用于建立「类型 -> 基类/接口」索引
RE_CLASS_DECL_ANY = re.compile(
    r"^\s*(?P<mods>(?:(?:public|internal|sealed|abstract|static|partial|unsafe)\s+)*)"
    r"\b(?:class|struct|record)\s+(?P<name>\w+)\s*(?:<[^>]*>)?\s*"
    r"(?::\s*(?P<bases>[^\r\n{]*))?",
    re.MULTILINE,
)

# [MessageHandler(typeof(NotifyBagInfoChanged), nameof(NotifyBagInfoChanged))]
RE_MSG_HANDLER_ATTR = re.compile(
    r"\[\s*MessageHandler\s*\(\s*typeof\s*\(\s*([\w\.]+)\s*\)\s*,\s*"
    r"(?:nameof\s*\(\s*([\w\.]+)\s*\)|\"([^\"]+)\")\s*\)\s*\]"
)

# 方法声明（用于读取 [MessageHandler] 目标的可见性与首参类型）
RE_METHOD_DECL = re.compile(
    r"^\s*(?P<mods>(?:(?:public|internal|protected|private|static|virtual|override|sealed|"
    r"async|extern|unsafe|partial|new|readonly)\s+)*)"
    r"(?:[\w\.<>\[\],\?]+\s+)?(?P<name>\w+)\s*\(\s*(?P<param>[\w\.<>\[\],\?]*)?",
    re.MULTILINE,
)

RE_NAMESPACE = re.compile(r"^\s*namespace\s+([\w\.]+)", re.MULTILINE)
RE_TYPE_DECL = re.compile(r"\b(?:class|struct)\s+(\w+)")

# 消息接口 -> 语义
IFACE_REQUEST = "IRequestMessage"
IFACE_RESPONSE = "IResponseMessage"
IFACE_NOTIFY = "INotifyMessage"
IFACE_HEARTBEAT = "IHeartBeatMessage"

# 包处理器接口（顺序 = 原扫描的 else-if 优先级；
# 一个类型命中多个时只按第一个匹配项注册，与原实现保持一致）
PACKET_INTERFACE_ORDER = [
    ("IPacketReceiveHeaderHandler", "channel.RegisterHandler((IPacketReceiveHeaderHandler)new {fq}());"),
    ("IPacketReceiveBodyHandler", "channel.RegisterHandler((IPacketReceiveBodyHandler)new {fq}());"),
    ("IPacketSendHeaderHandler", "channel.RegisterHandler((IPacketSendHeaderHandler)new {fq}());"),
    ("IPacketSendBodyHandler", "channel.RegisterHandler((IPacketSendBodyHandler)new {fq}());"),
    ("IPacketHeartBeatHandler", "channel.RegisterHeartBeatHandler(new {fq}());"),
    ("IMessageCompressHandler", "channel.RegisterMessageCompressHandler(new {fq}());"),
    ("IMessageDecompressHandler", "channel.RegisterMessageDecompressHandler(new {fq}());"),
]
PACKET_INTERFACE_NAMES = set(n for n, _ in PACKET_INTERFACE_ORDER)
PACKET_MARKER_INTERFACE = "IPacketHandler"

KIND_NONE = "EMessageKind.None"


# ---------------------------------------------------------------------------
# 工具
# ---------------------------------------------------------------------------


def read_text(path: str) -> str:
    with open(path, "r", encoding="utf-8-sig", errors="replace") as fp:
        return fp.read()


def strip_comments(text: str) -> str:
    """把 // 行注释、/* */ 块注释（含 /// XML 文档注释）替换为等长空白。

    等长替换保证字符偏移与行号不变，后续 enclosing_* 等定位仍然有效。
    目的：避免把「文档注释/示例代码里写到的 [MessageHandler(...)]」当成真实声明
    （本脚本自身的类注释里就有这种示例）。
    已知边界：不解析插值字符串 `$"{...}"` 中的嵌套引号。
    """
    out = []
    i, n = 0, len(text)
    mode = None  # None | line | block | str | verbatim
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ""

        if mode == "line":
            if c in "\r\n":
                mode = None
                out.append(c)
            else:
                out.append(" ")
            i += 1
            continue

        if mode == "block":
            if c == "*" and nxt == "/":
                mode = None
                out.append("  ")
                i += 2
                continue
            out.append(c if c in "\r\n" else " ")
            i += 1
            continue

        if mode == "str":
            out.append(c)
            if c == "\\" and i + 1 < n:
                out.append(text[i + 1])
                i += 2
                continue
            if c == '"':
                mode = None
            i += 1
            continue

        if mode == "verbatim":
            out.append(c)
            if c == '"':
                if nxt == '"':
                    out.append(nxt)
                    i += 2
                    continue
                mode = None
            i += 1
            continue

        if c == "/" and nxt == "/":
            mode = "line"
            out.append("  ")
            i += 2
            continue
        if c == "/" and nxt == "*":
            mode = "block"
            out.append("  ")
            i += 2
            continue
        if c == '"':
            j = len(out) - 1
            while j >= 0 and out[j] in " \t":
                j -= 1
            mode = "verbatim" if j >= 0 and out[j] == "@" else "str"
        out.append(c)
        i += 1

    return "".join(out)


def read_code(path: str) -> str:
    """读取源码并剥离注释（扫描一律走这里，避免命中文档注释里的示例）。"""
    return strip_comments(read_text(path))


def iter_cs_files(root: str):
    for dirpath, dirnames, filenames in os.walk(root):
        # 跳过 Unity 的隐藏/临时目录
        dirnames[:] = [d for d in sorted(dirnames) if not d.startswith(".")]
        for name in sorted(filenames):
            if name.endswith(".cs"):
                yield os.path.join(dirpath, name)


def is_generated_output(path: str) -> bool:
    return os.path.normcase(os.path.abspath(path)) == os.path.normcase(os.path.abspath(OUTPUT_PATH))


def is_under(path: str, directory: str) -> bool:
    p = os.path.normcase(os.path.abspath(path))
    d = os.path.normcase(os.path.abspath(directory)) + os.sep
    return p.startswith(d)


def split_bases(text: str):
    """拆分基类/接口列表：先剥掉泛型实参，避免 `Singleton<A, B>` 中的逗号被误判为分隔符。"""
    if not text:
        return []
    text = re.sub(r"<[^<>]*>", "", text)
    return [p.strip() for p in text.split(",") if p.strip()]


def simple_name(identifier: str) -> str:
    return identifier.split(".")[-1].strip()


def method_is_accessible(mods: str) -> bool:
    """生成物需要直接调用目标方法：仅 public / internal 可及。

    C# 无修饰符即 private，故必须显式出现 public 或 internal；
    只要出现 private / protected（含 protected internal / private protected）即不可及。
    """
    tokens = mods.split()
    if "private" in tokens or "protected" in tokens:
        return False
    return "public" in tokens or "internal" in tokens


def enclosing_type_name(text: str, pos: int):
    """返回 pos 位置所在的最内层类型名（仅支持非嵌套/一层嵌套的简单场景）。"""
    decls = [(m.start(), m.group(1)) for m in RE_TYPE_DECL.finditer(text, 0, pos)]
    for start, name in reversed(decls):
        seg = text[start:pos]
        brace = seg.find("{")
        if brace < 0:
            continue
        depth = 0
        for ch in seg[brace:]:
            if ch == "{":
                depth += 1
            elif ch == "}":
                depth -= 1
        # depth == 1 表示当前仍处于该类型的类型体内（只有类型体本身未闭合）
        if depth == 1:
            return name
    return None


def enclosing_namespace(text: str, pos: int) -> str:
    ns = ""
    for m in RE_NAMESPACE.finditer(text, 0, pos):
        ns = m.group(1)
    return ns


# ---------------------------------------------------------------------------
# 1. 扫描 proto：消息ID <-> 类型
# ---------------------------------------------------------------------------


def scan_proto_messages():
    """返回 (messages, proto_namespaces, warnings)。

    messages: list of dict(id, type, kind_flags, file)
      kind_flags 已按原 ProtoMessageIdHandler 的注册优先级归一：
        心跳位独立；请求/响应/推送三者按 Request > Response > Notify 取首个命中项。
    """
    messages = []
    warnings = []
    namespaces = set()

    if not os.path.isdir(PROTO_DIR):
        raise SystemExit("[错误] 未找到 proto 目录：%s" % PROTO_DIR)

    for path in iter_cs_files(PROTO_DIR):
        text = read_code(path)
        rel = os.path.relpath(path, REPO_ROOT).replace("\\", "/")

        ns = enclosing_namespace(text, len(text))
        if ns:
            namespaces.add(ns)

        for attr in RE_MSG_TYPE_ATTR.finditer(text):
            message_id = int(attr.group(1))
            decl = RE_CLASS_DECL.search(text, attr.end())
            if decl is None:
                warnings.append(
                    "%s: [MessageTypeHandler(%d)] 之后未找到类声明，已跳过。"
                    % (rel, message_id)
                )
                continue

            type_name = decl.group(1)
            bases = decl.group(2)

            def has(iface: str) -> bool:
                return re.search(r"\b%s\b" % iface, bases) is not None

            is_heartbeat = has(IFACE_HEARTBEAT)

            # 复刻原实现的优先级：Request > Response > Notify（命中即 continue）
            if has(IFACE_REQUEST):
                primary = "EMessageKind.Request"
            elif has(IFACE_RESPONSE):
                primary = "EMessageKind.Response"
            elif has(IFACE_NOTIFY):
                primary = "EMessageKind.Notify"
            else:
                primary = KIND_NONE

            parts = []
            if primary != KIND_NONE:
                parts.append(primary)
            if is_heartbeat:
                parts.append("EMessageKind.HeartBeat")
            kind_flags = " | ".join(parts) if parts else KIND_NONE

            if primary == KIND_NONE and not is_heartbeat:
                warnings.append(
                    "%s: 类型 %s 带 [MessageTypeHandler(%d)] 但未实现任何消息接口，"
                    "原实现同样不会注册（已保留原行为）。" % (rel, type_name, message_id)
                )

            messages.append(
                {
                    "id": message_id,
                    "type": type_name,
                    "kind": kind_flags,
                    "file": rel,
                }
            )

    messages.sort(key=lambda m: m["id"])
    return messages, namespaces, warnings


# ---------------------------------------------------------------------------
# 2. 扫描热更源码：全量类型索引
# ---------------------------------------------------------------------------


def scan_class_index():
    """返回 (decls, by_name)。decls: 所有类型声明；by_name: 简单名 -> 声明（首次出现优先）。"""
    decls = []
    by_name = {}
    for path in iter_cs_files(HOTFIX_ROOT):
        if is_generated_output(path):
            continue
        text = read_code(path)
        rel = os.path.relpath(path, REPO_ROOT).replace("\\", "/")
        for m in RE_CLASS_DECL_ANY.finditer(text):
            decl = {
                "file": rel,
                "path": path,
                "name": m.group("name"),
                "mods": m.group("mods") or "",
                "bases": split_bases(m.group("bases") or ""),
                "namespace": enclosing_namespace(text, m.start()),
            }
            decls.append(decl)
            by_name.setdefault(decl["name"], decl)
    return decls, by_name


# ---------------------------------------------------------------------------
# 3. 扫描 [MessageHandler] 方法：产出直接委托
# ---------------------------------------------------------------------------


def scan_message_handler_methods(by_name):
    """返回 (handlers, message_type_namespaces, errors, warnings, registered_type_names)。

    handlers: dict[(namespace, type)] -> list of dict(message_type, method_name, invoke)
      invoke 为生成期产出的直接委托表达式：
          static (handler, message) => ((Ns.Handler)handler).OnX((X)message)
    errors: 可见性/定位问题（非空则中止生成）
    """
    handlers = {}
    message_type_namespaces = set()
    errors = []
    warnings = []

    for path in iter_cs_files(HOTFIX_ROOT):
        if is_generated_output(path):
            continue

        text = read_code(path)
        rel = os.path.relpath(path, REPO_ROOT).replace("\\", "/")

        for m in RE_MSG_HANDLER_ATTR.finditer(text):
            message_type = simple_name(m.group(1))
            method_name = m.group(2) or m.group(3)
            line = text.count("\n", 0, m.start()) + 1

            type_name = enclosing_type_name(text, m.start())
            if type_name is None:
                warnings.append(
                    "%s: 第 %d 行的 [MessageHandler] 未能定位所属类型，已跳过。" % (rel, line)
                )
                continue

            ns = enclosing_namespace(text, m.start())

            # --- 定位目标方法并校验可见性 ---
            decl = RE_METHOD_DECL.search(text, m.end())
            if decl is None or decl.group("name") != method_name:
                errors.append(
                    "%s: 第 %d 行 [MessageHandler(..., %s)] 之后未找到同名方法声明，"
                    "无法生成直接委托。请确认方法紧跟在特性之后。"
                    % (rel, line, method_name)
                )
                continue

            mods = decl.group("mods") or ""
            if not method_is_accessible(mods):
                errors.append(
                    "%s: 第 %d 行的方法 %s.%s 不可访问（修饰符：%s）。"
                    "生成物需要直接调用该方法，请将其改为 internal 或 public。"
                    % (rel, line, type_name, method_name, mods.strip() or "(无修饰符，即 private)")
                )
                continue

            # --- 参数类型自检：应与 [MessageHandler] 声明的消息类型一致 ---
            param = simple_name(decl.group("param") or "")
            if param != message_type:
                warnings.append(
                    "%s: 第 %d 行的方法 %s.%s 首参类型为 '%s'，与 [MessageHandler] 声明的消息类型 '%s' 不一致；"
                    "生成的直接委托会据此做强制转换，若无法编译请核对（原实现会在注册期抛异常）。"
                    % (rel, line, type_name, method_name, param or "(无)", message_type)
                )

            # --- 解析消息类型所在命名空间（生成物据此补 using）---
            msg_decl = by_name.get(message_type)
            if msg_decl is not None and msg_decl["namespace"]:
                message_type_namespaces.add(msg_decl["namespace"])
            else:
                warnings.append(
                    "%s: 第 %d 行的消息类型 %s 未在热更源码中定位到声明，"
                    "生成物将依赖既有 using 解析该类型；若编译报错请检查其所在命名空间。"
                    % (rel, line, message_type)
                )

            handler_fq = ("%s.%s" % (ns, type_name)) if ns else type_name
            invoke = "static (handler, message) => ((%s)handler).%s((%s)message)" % (
                handler_fq,
                method_name,
                message_type,
            )

            handlers.setdefault((ns, type_name), []).append(
                {
                    "message_type": message_type,
                    "method_name": method_name,
                    "invoke": invoke,
                }
            )

    return handlers, message_type_namespaces, errors, warnings


def scan_message_handler_types():
    """返回所有实现 IMessageHandler 的类型（含无 [MessageHandler] 方法的类型）。"""
    results = set()
    for path in iter_cs_files(HOTFIX_ROOT):
        if is_generated_output(path):
            continue
        text = read_code(path)
        for decl in RE_CLASS_DECL.finditer(text):
            if re.search(r"\bIMessageHandler\b", decl.group(2)) is None:
                continue
            results.add((enclosing_namespace(text, decl.start()), decl.group(1)))
    return results


# ---------------------------------------------------------------------------
# 4. 扫描框架外的包处理器实现
# ---------------------------------------------------------------------------


def scan_external_packet_handlers(decls, by_name):
    """返回 (handlers, warnings)。

    handlers: list of dict(fq, name, iface, register, file, depth)
      * 排除 Framework/Network/Helper/ 下的框架自带实现（那些由 DefaultNetworkChannelHelper
        显式装配，见 RegisterDefaultHandlers）。
      * 只取具体类型（跳过 abstract / static）。
      * 接口判定含继承链，故「派生自框架基类」的游戏侧实现同样能被识别。
      * 每个类型只按 PACKET_INTERFACE_ORDER 的首个命中接口注册一次，与原扫描的 else-if 一致。
      * 排序：继承深度浅的在前（后注册者覆盖），同深度按全名排序，保证幂等且「更具体的实现生效」。
    """
    iface_memo = {}

    def resolve_ifaces(name, seen=None):
        if name in iface_memo:
            return iface_memo[name]
        if seen is None:
            seen = set()
        if name in seen:
            return set()
        seen.add(name)

        decl = by_name.get(name)
        if decl is None:
            return set()

        result = set()
        for base in decl["bases"]:
            bn = simple_name(base)
            if bn in PACKET_INTERFACE_NAMES or bn == PACKET_MARKER_INTERFACE:
                result.add(bn)
            else:
                result |= resolve_ifaces(bn, seen)
        iface_memo[name] = result
        return result

    depth_memo = {}

    def class_depth(name, seen=None):
        if name in depth_memo:
            return depth_memo[name]
        if seen is None:
            seen = set()
        if name in seen:
            return 0
        seen.add(name)

        decl = by_name.get(name)
        if decl is None:
            return 0

        best = 0
        for base in decl["bases"]:
            bn = simple_name(base)
            if bn in PACKET_INTERFACE_NAMES or bn == PACKET_MARKER_INTERFACE:
                continue
            if bn in by_name:
                best = max(best, 1 + class_depth(bn, seen))
        depth_memo[name] = best
        return best

    handlers = []
    warnings = []

    for decl in decls:
        if is_under(decl["path"], NETWORK_HELPER_DIR):
            continue

        if re.search(r"\b(abstract|static)\b", decl["mods"]):
            # 与原扫描一致：跳过抽象类型（静态类不是可实例化的处理器）
            continue

        ifaces = resolve_ifaces(decl["name"])
        hit = None
        for iface_name, template in PACKET_INTERFACE_ORDER:
            if iface_name in ifaces:
                hit = (iface_name, template)
                break
        if hit is None:
            continue

        if PACKET_MARKER_INTERFACE not in ifaces:
            # 与原扫描的 `IsImplWithInterface(IPacketHandler)` 前置条件一致
            warnings.append(
                "%s: 类型 %s 实现了 %s 但未实现标记接口 %s，原扫描同样会跳过（已保留原行为）。"
                % (decl["file"], decl["name"], hit[0], PACKET_MARKER_INTERFACE)
            )
            continue

        fq = ("%s.%s" % (decl["namespace"], decl["name"])) if decl["namespace"] else decl["name"]
        handlers.append(
            {
                "fq": fq,
                "name": decl["name"],
                "iface": hit[0],
                "register": hit[1].format(fq=fq),
                "file": decl["file"],
                "depth": class_depth(decl["name"]),
            }
        )

    handlers.sort(key=lambda h: (h["depth"], h["fq"]))
    return handlers, warnings


# ---------------------------------------------------------------------------
# 5. 生成
# ---------------------------------------------------------------------------

HEADER = """// <auto-generated>
//     *** 自动生成，请勿手动修改 ***
//
//     生成脚本：{script}
//     生成命令：Tools/gen-proto-registry.bat  (Windows)
//               bash Tools/gen-proto-registry.sh  (macOS/Linux)
//
//     proto 变更（新增/删除消息、修改消息ID、修改消息接口）、新增 [MessageHandler] 方法，
//     或新增/调整框架外的包处理器实现后，必须重新运行上述脚本；
//     否则运行时注册表会与实际代码不一致。
//
//     本文件取代了原先运行时的 Assembly.GetTypes() 全程序集扫描与特性反射读取，
//     并直接产出强类型处理委托（不再有 MethodInfo / CreateDelegate / GetMethods），
//     以满足项目铁律 4（运行时杜绝反射）。
// </auto-generated>

using System;
{proto_usings}// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Network
{{
    /// <summary>
    /// 协议消息注册表（生成物）。
    /// 说明：静态注册「消息ID &lt;-&gt; 类型」「消息类型 -&gt; 消息处理方法（直接委托）」，
    /// 并注册框架外的包处理器实现；运行时不再扫描程序集、不再读取特性、不再查找方法。
    /// </summary>
    internal static class ProtoMessageRegistry
    {{
        /// <summary>
        /// 框架外包处理器的注册是否已执行（ProtoMessageIdHandler.Init 失败重试时避免重复追加委托）。
        /// </summary>
        private static bool s_ExternalPacketHandlersRegistered;

        /// <summary>
        /// 注册全部协议消息、用户消息处理方法与框架外包处理器。
        /// 由 ProtoMessageIdHandler.Init 调用，且仅在首次初始化时执行一次。
        /// </summary>
        internal static void RegisterAll()
        {{
            RegisterMessageIds();
            RegisterMessageHandlerMethods();
            RegisterExternalPacketHandlers();
        }}

        /// <summary>
        /// 注册「消息ID &lt;-&gt; 类型」映射、心跳类型集合。
        /// 共 {message_count} 条。
        /// </summary>
        private static void RegisterMessageIds()
        {{
{message_registrations}        }}

        /// <summary>
        /// 注册用户 [MessageHandler] 方法所属类型及其 (消息类型, 直接委托) 清单。
        /// 共 {handler_type_count} 个类型。
        /// 委托形如 static (handler, message) =&gt; ((Handler)handler).OnX((X)message)：
        /// 直接调用目标方法，运行时无反射、无按名查找、无 MethodInfo.Invoke。
        /// </summary>
        private static void RegisterMessageHandlerMethods()
        {{
{handler_registrations}        }}

        /// <summary>
        /// 注册框架外（<c>Framework/Network/Helper/</c> 之外）的具体包处理器。
        /// 共 {external_handler_count} 个。
        ///
        /// 时序说明：这里只登记「装配委托」，真正的装配发生在每个频道
        /// <c>DefaultNetworkChannelHelper.Initialize</c> 中，且位于
        /// <c>RegisterDefaultHandlers()</c>（框架自带 7 个处理器）之后，
        /// 因此按 RegisterXxxHandler 的「后注册覆盖」语义，
        /// 框架外实现会覆盖框架默认实现（例如游戏侧心跳实现覆盖框架基类 BasePacketHeartBeatHandler）。
        ///
        /// 多个框架外实现命中同一接口时，按「继承更深者后注册」排序，即最具体的实现生效。
        /// </summary>
        private static void RegisterExternalPacketHandlers()
        {{
            if (s_ExternalPacketHandlersRegistered) return;
            s_ExternalPacketHandlersRegistered = true;

{external_handler_registrations}        }}
    }}
}}
"""


def build_output(messages, proto_namespaces, message_type_namespaces, handlers, handler_types, external_handlers):
    # ---- using ----
    ns_list = sorted(set(proto_namespaces) | set(message_type_namespaces))
    if not ns_list:
        ns_list = [DEFAULT_PROTO_NAMESPACE]
    proto_usings = "".join("using %s;\n" % ns for ns in ns_list)
    if proto_usings:
        proto_usings += "\n"

    # ---- 消息注册 ----
    lines = []
    for msg in messages:
        lines.append(
            "            MessageIdRegistry.Register<%s>(%d, %s);\n"
            % (msg["type"], msg["id"], msg["kind"])
        )
    message_registrations = "".join(lines) if lines else "            // 未扫描到任何协议消息。\n"

    # ---- 消息处理方法注册（直接委托）----
    lines = []
    registered_types = []
    for (ns, type_name) in sorted(handlers.keys()):
        entries = handlers[(ns, type_name)]
        full_name = ("%s.%s" % (ns, type_name)) if ns else type_name
        registered_types.append((ns, type_name))
        lines.append(
            "            ProtoMessageHandler.RegisterHandlerType(typeof(%s), new[]\n            {\n"
            % full_name
        )
        for entry in entries:
            lines.append(
                '                new ProtoMessageHandlerMethod(typeof(%s), "%s",\n'
                "                    %s),\n"
                % (entry["message_type"], entry["method_name"], entry["invoke"])
            )
        lines.append("            });\n")

    # 已实现 IMessageHandler 但无 [MessageHandler] 方法：登记空清单，
    # 使 ProtoMessageHandler 能区分「已登记无处理方法」与「未登记（需重新生成）」。
    for (ns, type_name) in sorted(handler_types):
        if (ns, type_name) in handlers:
            continue
        full_name = ("%s.%s" % (ns, type_name)) if ns else type_name
        registered_types.append((ns, type_name))
        lines.append(
            "            ProtoMessageHandler.RegisterHandlerType(typeof(%s), Array.Empty<ProtoMessageHandlerMethod>());\n"
            % full_name
        )
    handler_registrations = "".join(lines) if lines else "            // 未扫描到任何 IMessageHandler 实现类型。\n"

    # ---- 框架外包处理器注册 ----
    if external_handlers:
        lines = [
            "            DefaultNetworkChannelHelper.AddCustomHandlerRegistrar(channel =>\n",
            "            {\n",
        ]
        for h in external_handlers:
            lines.append("                // %s\t%s\n" % (h["iface"], h["file"]))
            lines.append("                %s\n" % h["register"])
        lines.append("            });\n")
        external_handler_registrations = "".join(lines)
    else:
        external_handler_registrations = (
            "            // 未扫描到框架外的包处理器实现（Framework/Network/Helper/ 之外）。\n"
        )

    return HEADER.format(
        script="Tools/gen-proto-registry.py",
        proto_usings=proto_usings,
        message_count=len(messages),
        handler_type_count=len(registered_types),
        external_handler_count=len(external_handlers),
        message_registrations=message_registrations,
        handler_registrations=handler_registrations,
        external_handler_registrations=external_handler_registrations,
    )


# ---------------------------------------------------------------------------
# 6. 校验
# ---------------------------------------------------------------------------


def validate(messages):
    errors = []

    by_id = {}
    by_type = {}
    for msg in messages:
        if msg["id"] in by_id:
            errors.append(
                "消息ID 重复：%d 同时用于 %s 与 %s（%s）"
                % (msg["id"], by_id[msg["id"]]["type"], msg["type"], msg["file"])
            )
        else:
            by_id[msg["id"]] = msg

        if msg["type"] in by_type:
            errors.append(
                "消息类型重复定义：%s（%s 与 %s）"
                % (msg["type"], by_type[msg["type"]]["file"], msg["file"])
            )
        else:
            by_type[msg["type"]] = msg

    return errors


# ---------------------------------------------------------------------------
# main
# ---------------------------------------------------------------------------


def main() -> int:
    print("[gen-proto-registry] 仓库根目录: %s" % REPO_ROOT)
    print("[gen-proto-registry] 扫描 proto: %s" % PROTO_DIR)

    messages, proto_namespaces, proto_warnings = scan_proto_messages()
    decls, by_name = scan_class_index()
    handlers, msg_ns, handler_errors, handler_warnings = scan_message_handler_methods(by_name)
    handler_types = scan_message_handler_types()
    external_handlers, external_warnings = scan_external_packet_handlers(decls, by_name)

    errors = validate(messages) + handler_errors
    warnings = proto_warnings + handler_warnings + external_warnings

    for w in warnings:
        print("[警告] %s" % w)
    for e in errors:
        print("[错误] %s" % e)
    if errors:
        print("[gen-proto-registry] 生成中止：请先修复上述问题（生成物未更新）。")
        return 1

    content = build_output(
        messages, proto_namespaces, msg_ns, handlers, handler_types, external_handlers
    )

    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    with open(OUTPUT_PATH, "w", encoding="utf-8", newline="\n") as fp:
        fp.write(content)

    print(
        "[gen-proto-registry] 已生成 %s（消息 %d 条，消息处理器类型 %d 个，框架外包处理器 %d 个）"
        % (
            OUTPUT_REL,
            len(messages),
            len(set(handlers.keys()) | handler_types),
            len(external_handlers),
        )
    )
    for h in external_handlers:
        print("    [外部处理器] %-28s %s  (depth=%s)" % (h["iface"], h["fq"], h["depth"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
