# unity-cli

通过 TCP 直接操控 Unity Editor。**本文件无 `paths:`，每次会话常驻。**

**CLI 随仓库提供**：二进制在 `Tools/UnityCli/unity-cli.exe`（**进 git**），**不在 PATH 中**。下文命令均在**项目根**执行，克隆仓库即可用，无需安装。

原因：CLI 版本必须与项目内 bridge 包版本一致，两者同处一个仓库一起提交，避免出现 `CLI 0.19.1 does not match bridge 0.12.0`。升级流程见 `Tools/UnityCli/README.md`。

## 连接

```bash
Tools/UnityCli/unity-cli.exe system ping      # 返回 "pong" 即连通
```

**必需的两个环境变量**（User 级，新终端生效；`.claude/verify-compile.sh` 已内置，手动调用仍需用户级设置）：

- `UNITY_CLI_NO_AUTO_UPDATE=1`——**禁止自更新**。`unity-cli.exe` 是版本库文件，自更新会覆盖它并污染工作区，还会阻塞退出约 135s
- `UNITY_CLI_HOST=127.0.0.1`——跳过 localhost 解析（省 2s）

新机器克隆后必须设置，详见 `Tools/UnityCli/README.md`。若命令变慢或工作区莫名多出 exe 改动，先查这两个变量。

## 调用语法

```bash
Tools/UnityCli/unity-cli.exe tool call <tool> --json '{"key":"value"}'      # 内联 JSON
Tools/UnityCli/unity-cli.exe tool call <tool> --params-file <file>          # 从文件
Tools/UnityCli/unity-cli.exe tool schema <tool>                             # 查参数名（必查）
```

参数 schema **严格**（`additionalProperties: false`），字段名先 `tool schema` 查。例如 `read_console` 用 `logTypes` 数组 + `count`，不是 `logType`/`limit`。

## 编译验证（改完 .cs 必跑）

```bash
bash .claude/verify-compile.sh
```

内部：ping → `clear_console` → `refresh_assets` → 轮询 `get_compilation_state`（看 `isCompiling`/`errorCount`）→ 有错则 `read_console` 列出。exit 0 通过 / 1 有错 / 2 未连接。

## 常用命令

| 命令 | 用途 |
|---|---|
| `play_game` / `stop_game` / `pause_game` | 进入 / 停止 / 暂停 Play（`stop_game` 不接受 `delayMs`） |
| `refresh_assets` | 刷新资源触发编译 |
| `get_compilation_state` | 编译状态（`--output json`） |
| `clear_console` / `read_console` | 清 / 读 Console |
| `execute_menu_item` | 执行 Editor 菜单（运行时探针的唯一入口） |

## 运行时能力

### eval_csharp：直接注入 C# 表达式（首选）

**不停止游戏、不修改代码、不重编译**，Roslyn 编译后立即求值：

```bash
Tools/UnityCli/unity-cli.exe tool call eval_csharp --json '{"code":"UnityEngine.Application.unityVersion"}'
# → {"value":"2022.3.33f1c1","state":"completed","diagnostics":[],"exception":null}
```

- 参数是 **C# 表达式**（不是完整类定义）——可含 LINQ、链式调用、项目类型、运行时对象。
- 实测可用：`System.Linq.Enumerable.Sum(...)`、`typeof(Hotfix.Framework.Core.ModuleBase).Name`、`UnityEngine.SceneManagement.SceneManager.GetActiveScene().name`。
- 返回 `value` / `diagnostics`（编译错误）/ `exception`（运行时异常）/ `state`。
- 编译错误时看 `diagnostics` 的 `id`（如 `CS1525`）与行列位置。

**这是查运行时状态的最快路径**——比写 `[MenuItem]` 探针 + 编译快一个数量级。

### 其余现成工具

| 能力 | 工具 | 说明 |
|---|---|---|
| 读组件字段 | `get_component_values` | `gameObjectName` + `componentType`；`includePrivateFields` 可读**私有字段** |
| 查引用关系 | `get_object_references` | 谁引用了这个对象 |
| 看层级 / 场景 | `get_hierarchy`、`analyze_scene_contents`、`get_gameobject_details` | 结构快照 |
| **性能分析** | `profiler_start` → `profiler_get_metrics` → `profiler_stop` | `listAvailable:true` 先枚举可用指标 |
| **热重载** | `hot_reload`、`hot_reload_status` | 需装 Fast Script Reload 包，否则返回 Unavailable |
| **打包** | `build_player`、`get_build_status` | |
| UI 验收 | `click_ui_element`、`set_ui_element_value`、`get_ui_element_state`、`find_ui_elements`、`simulate_ui_input` | 按元素路径操作 |
| 输入模拟 | `input_mouse`、`input_keyboard`、`input_touch`、`input_gamepad` | |
| 测试 | `run_tests`、`get_test_status` | |
| 取证 | `capture_screenshot`、`capture_video_start/stop/status` | |

**选择顺序**：`eval_csharp` 一句话能答 → 用它；需要结构化输出 → 专用工具；都不行 → 才写 `[MenuItem]` 探针 + `execute_menu_item`。

CLI 侧 `Tools/UnityCli/unity-cli.exe tool list` 列出全部工具名（当前 154 个）。

## 运行时实测（插桩套路）

1. **快照**：`cp 文件 /tmp/x` 并记 `md5sum`。
2. **插桩**：改动点用 `// ==== 临时探针（用完删） ====` 包裹；日志用 `public static bool ProbeEnabled` 门控。
3. **触发**：在 Hotfix 程序集加 `#if UNITY_EDITOR` 的 `[MenuItem]`，用 `execute_menu_item` 执行（**菜单名用 ASCII**，中文不稳）。探针放 Hotfix 可免改 asmdef（`Unity.Editor.asmdef` 未引用 UniTask）。
4. **还原**：从快照逐字节还原并用 `md5sum` 比对。
5. **清理**：删探针目录后，一并删孤儿 `.meta`；`git status --porcelain -uall | grep -i "temp\|probe"` 复查。

## 关键陷阱（实测）

- **`read_console` 是消费式读取**：读一次 `totalCaptured` 即清零。
- **Play 模式下 `manage_asset_database` 被 block**（`PLAY_MODE_BLOCKED`）——改代码必须先停 Play。
- **探针里的 `Debug.Log` 被捕获为 `Warning` 类型**（不是 `Log`）。按 `logTypes:["Log"]` 过滤会永远读不到。长时探针优先**写结果文件**并用 `grep -q` 轮询完成标记。
- **`EditorSimulateMode` 下所有资源加载恒约 1 帧**（1KB 与 5.9MB 同）——**帧时序类问题在此模式下无法验证**，需切 `OfflinePlayMode`/`HostPlayMode`。
- **Unity Mono 堆栈格式**：`Environment.StackTrace` 带 `[0xIL偏移]` 段，行号是 `:39` 不是 `:line 39`。解析正则必须兼容。
- **反射调热更带 `params` 的方法**：`Invoke` 参数个数必须与签名完全一致，`params` 不会自动补。
- **Editor 探针反射热更类型**：`Type.GetType("命名空间.类型, Hotfix")`（程序集名 `Hotfix`），private 成员带 `BindingFlags.NonPublic | BindingFlags.Instance`。
