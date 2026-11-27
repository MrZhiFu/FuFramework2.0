# unity-cli

通过 TCP 直接操控 Unity Editor。**本文件无 `paths:`，每次会话常驻。**

**CLI 随仓库提供**：二进制在 `Tools/UnityCli/unity-cli.exe`（**进 git**），**不在 PATH 中**。下文命令均在**项目根**执行，克隆仓库即可用，无需安装。

原因：CLI 版本必须与项目内 bridge 包版本一致，两者同处一个仓库一起提交，避免出现 `CLI 0.19.1 does not match bridge 0.12.0`。升级流程见 `Tools/UnityCli/README.md`。

## 连接

```bash
Tools/UnityCli/unity-cli.exe system ping      # 返回 "pong" 即连通
```

**必需的两个环境变量**（User 级，新终端生效；`.claude/fu-unity-compile.sh` 已内置，手动调用仍需用户级设置）：

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
bash .claude/fu-unity-compile.sh
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

### eval_csharp：直接注入 C# 代码（首选）

**不停止游戏、不修改代码、不重编译**，Roslyn 编译后立即执行：

```bash
Tools/UnityCli/unity-cli.exe tool call eval_csharp --json '{"code":"UnityEngine.Application.unityVersion"}'
# → {"value":"2022.3.33f1c1","state":"completed","diagnostics":[],"exception":null,"logs":[]}
```

**两种模式（`mode` 参数）**：

| mode | 包装方式 | 用途 |
|---|---|---|
| `expression`（默认） | `return (object)(<code>);` | 求一个值。**不能调用 void 方法**（报 CS0030）|
| `statements` | `<code>` 原样作为语句块 | 执行多行语句、调 void 方法、局部变量、`Debug.Log` |

```json
{"mode":"statements","code":"var btn = ...;\nbtn.FireClick(false, true);\nDebug.Log(\"clicked\");"}
```

**其他要点**：
- 引用范围 = `AppDomain` 里所有已加载程序集，项目类型（`Hotfix.*`/`Assembly-CSharp`/`FairyGUI`）直接可用，无需 using（`System`/`UnityEngine`/`UnityEditor` 已自动注入）。
- 返回 `value` / `diagnostics`（编译错误，含行列与 `CSxxxx`）/ `exception` / `state` / **`logs`（捕获执行期间的 `Debug.Log`）**。
- **代码含双引号/换行时用 `--params-file` 传 JSON**，直接 `--json` 会被 shell 转义搞坏。

### FairyGUI 自动化（实测套路）

Play 模式下可直接操作 FGUI 界面——官方 `click_ui_element` 走 Input System 虚拟设备，**打不到 FGUI**（FGUI 在 Windows 上读 `Mouse.current`），必须用 `eval_csharp`。

```json
// 1. 列层级：7 个 UIGroup（SceneUI/MainUI/Normal/Window/Tips/Guide/Loading）
{"code":"string.Join(\" | \", System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0, FairyGUI.GRoot.inst.numChildren), i => FairyGUI.GRoot.inst.GetChildAt(i).name + \"=\" + ((FairyGUI.GComponent)FairyGUI.GRoot.inst.GetChildAt(i)).numChildren.ToString()))"}

// 2. 列某界面下的控件（含类型，便于找 GButton）
{"code":"string.Join(\" | \", ...GetChild(\"Normal\")).GetChildAt(0)... 的每个子节点 .name + \"(\" + .GetType().Name + \")\")"}

// 3. 点按钮：FireClick(bool downEffect, bool clickCallbacks)
{"mode":"statements","code":"var btn = (FairyGUI.GButton)<界面>.GetChild(\"_btnLogin\"); btn.FireClick(false, true);"}

// 4. 读控件文本
{"code":"<界面>.GetChild(\"_txtError\").text"}
```

**踩过的坑**：
- `GObject` **没有** `numChildren` / `GetChildAt` 的子组件遍历语义——要 `(FairyGUI.GComponent)` 强转。
- `FireClick()` 无参调用编译失败，必须给两个 bool。
- 非 Play 模式下访问 `GRoot.inst` 抛 `DontDestroyOnLoad` 异常（Stage 只能在 Play 创建）。
- `capture_screenshot` 报 `GAME_VIEW_NOT_FOCUSED` 时，需先让 Game View 获得焦点。

### 拖拽/滑动：反射驱动 TouchInfo（实测可用）

**真实指针注入在本项目全部不可行**（均实测过）：

| 方案 | 结果 |
|---|---|
| `Stage.SetCustomInput` | 注入被消费（`_customInput` 变 false），但 `HitTest` 全落空——连全屏背景都不命中 |
| `input_mouse` / `click_ui_element`（Input System 虚拟设备）| 打不到：FGUI 未启用 `FAIRYGUI_INPUT_SYSTEM`，读的是旧 `Input.mousePosition` |
| 系统 `mouse_event` / `SendInput` | 鼠标**移动**生效（`Input.mousePosition` 随之变化），但**按键事件被过滤**，Unity 检测不到 |

**可行方案**：反射操控 `Stage._touches[0]`，手动走 FGUI 的触摸状态机。`target` 需自己指定（不走 HitTest）。

```csharp
// statements 模式；先取 target（例如 _btnLogin）
var B = System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public;
var touches = (Array)typeof(FairyGUI.Stage).GetField("_touches", B).GetValue(FairyGUI.Stage.inst);
var touch = touches.GetValue(0);
var tt = touch.GetType();
var c = btn.LocalToGlobal(new Vector2(btn.width*0.5f, btn.height*0.5f));

tt.GetField("x").SetValue(touch, c.x);
tt.GetField("y").SetValue(touch, c.y);
tt.GetField("target").SetValue(touch, btn.displayObject);
tt.GetField("button").SetValue(touch, 0);
tt.GetField("touchId").SetValue(touch, 0);

// 按下
tt.GetMethod("Begin").Invoke(touch, null);
tt.GetMethod("UpdateEvent").Invoke(touch, null);
btn.displayObject.BubbleEvent("onTouchBegin", tt.GetField("evt").GetValue(touch));

// 拖拽：改 x/y + Move + onTouchMove（可多帧多次）
tt.GetField("x").SetValue(touch, c.x + 60f);
tt.GetMethod("UpdateEvent").Invoke(touch, null);
btn.displayObject.BubbleEvent("onTouchMove", tt.GetField("evt").GetValue(touch));

// 抬起 + 点击判定
tt.GetMethod("End").Invoke(touch, null);
var ct = tt.GetMethod("ClickTest").Invoke(touch, null);   // 移动超过阈值会返回 null
if (ct != null) {
    tt.GetMethod("UpdateEvent").Invoke(touch, null);
    ((FairyGUI.DisplayObject)ct).BubbleEvent("onClick", tt.GetField("evt").GetValue(touch));  // 是 "onClick" 不是 "onTouchEnd"
}
tt.GetField("button").SetValue(touch, -1);
```

**实测结论**：
- 不移动 → `clickCancelled=false` → 派发 `onClick` → 业务链路触发 ✓
- 移动 60px → `clickCancelled=true` → `ClickTest()` 返回 null → **不触发点击** ✓（拖拽语义正确）
- 点击派发的事件名是 **`"onClick"`**，不是 `onTouchEnd`；目标要经 `ClickTest()` 取，不是直接用 `target`。

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
