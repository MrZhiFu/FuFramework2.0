---
name: fu-unity-cli-usage
description: 通过 unity-cli 驱动 Unity Editor 的工作流：连接检查、编译验证、Play/Stop、运行时插桩实测与还原。需要操作 Unity 编辑器、跑编译验证、或在 Play 模式里取证时使用。约束与陷阱清单见 rules/tooling/unity-cli.md。
allowed-tools: Bash(unity-cli:*), Bash(bash:*), Read, Grep, Glob
---

# unity-cli 工作流

约束与陷阱全表在 `rules/tooling/unity-cli.md`（该规则常驻加载）。本技能只讲**操作流程**。

## 0. 连接检查

```bash
Tools/UnityCli/unity-cli.exe system ping        # 期望 "pong"
```

未连通 → 提示用户打开 Unity 工程，不要反复重试。

## 1. 编译验证（改完 .cs 必跑）

```bash
bash .claude/fu-unity-compile.sh
```

退出码：`0` 通过 / `1` 有编译错误 / `2` 编辑器未连接 / `3` 编译未在时限内完成 / `4` 状态读不到。

不要自己拼 unity-cli 命令做编译验证——脚本已包含停 Play、清控制台、刷新、轮询、读错误。

## 2. 查命令与参数

```bash
Tools/UnityCli/unity-cli.exe tool list                        # 全部工具
Tools/UnityCli/unity-cli.exe tool schema <tool>               # 参数 schema（必查，字段名严格）
Tools/UnityCli/unity-cli.exe tool call <tool> --json '{"k":"v"}'
```

## 3. 运行时取证：eval_csharp 优先

**`eval_csharp` 可直接注入 C# 表达式**——不停止游戏、不改代码、不重编译：

```bash
Tools/UnityCli/unity-cli.exe tool call eval_csharp --json '{"code":"UnityEngine.Application.unityVersion"}'
# → {"value":"2022.3.33f1c1","state":"completed"}
```

参数是**表达式**（可含 LINQ、项目类型、运行时对象）。这是查运行时状态最快的路径。

现成工具答不了再写探针。常用工具：

| 要什么 | 用什么 |
|---|---|
| 任意表达式求值 | `eval_csharp` |
| 读组件字段（含私有） | `get_component_values` |
| 查引用关系 | `get_object_references` |
| 看层级/场景 | `get_hierarchy`、`analyze_scene_contents` |
| 性能数据 | `profiler_start` → `profiler_get_metrics` → `profiler_stop` |
| 热重载 | `hot_reload`（需 FSR 包） |
| UI 状态/操作 | `get_ui_element_state`、`click_ui_element`、`find_ui_elements` |
| 测试 | `run_tests` |
| 取证截图 | `capture_screenshot` |

完整能力表见 `rules/tooling/unity-cli.md`。

## 4. Play 模式实测（插桩套路）

1. **快照**：`cp <文件> /tmp/<文件>.bak` 并记 `md5sum`。
2. **插桩**：改动点用 `// ==== 临时探针（用完删） ====` 包裹；日志用一个 `public static bool ProbeEnabled` 开关门控。
3. **触发**：在 Hotfix 程序集加 `#if UNITY_EDITOR` 的 `[MenuItem("...")]`（**菜单名用 ASCII**），用 `execute_menu_item` 触发。放 Hotfix 可免改 asmdef。
4. **长时探针优先写结果文件**（`%TEMP%/xxx.json`）并用 `grep -q` 轮询完成标记——控制台是消费式读取且日志级别会被抬升。
5. **还原**：从快照逐字节还原，`md5sum` 比对确认。
6. **清理**：删探针目录后一并删孤儿 `.meta`；`git status --porcelain -uall | grep -i "temp\|probe"` 复查。

## 4. 常见阻断

| 现象 | 处理 |
|---|---|
| Play 中资源库操作报阻断 | 先 `stop_game` |
| 控制台读了一次再读为空 | 消费式读取，一次读完或先过滤 |
| 探针轮询一直空 | 日志级别被抬升；改用结果文件轮询 |
| 加载时序复现不出 | 编辑器模拟模式恒约 1 帧，需切真实 PlayMode |
| 命令卡很久 | 查 `UNITY_CLI_NO_AUTO_UPDATE` 与 `UNITY_CLI_HOST` 环境变量 |

## 5. 报告

给出**证据**：命令原文 + 关键输出片段 + 退出码。不要只说"验证通过"。
