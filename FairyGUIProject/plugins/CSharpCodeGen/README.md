# CSharpCodeGen - C# 代码自动生成

FairyGUI 编辑器插件，**发布（Publish）时自动启用**（`onPublish` 钩子，无需菜单触发），为每个发布包生成 Unity Hotfix 侧的界面/组件 C# 代码。

## 生成内容

| 生成物 | 层级 | 输出目录（Hotfix） |
| ------ | ---- | ------------------ |
| 界面 WinXXX | .Gen.cs（自动生成层）/ .cs（逻辑层） | AutoGen/UI/{pkg}/ 与 UI/{pkg}/ |
| 组件 CompXXX | 自定义组件代码 | AutoGen/UI/{pkg}/Comp/ |
| 统一绑定 CustomCompBind | 组件绑定代码 | AutoGen/UI/ |

**Launcher 包（AOT）特殊处理**：仅生成 `WinLauncher.Gen.cs` → `Launch/UI/`，不生成组件与绑定。

## 命名规范

- 界面：`WinXXX`
- 自定义组件：`CompXXX`
- 控件：`_` + 控件类型缩写 + 功能名，如 `_btnXXX`、`_txtXXX`、`_imgXXX`、`_listXXX`
- 静态多语言文本：`s_txtXXX`，表示需要填入多语言 key

## 发布执行链（onPublish）

1. 初始化发布处理器与通用工具（`GenReady:Init`）
2. **包级开关**：未勾选"为本包生成代码"（`publishSettings.genCode`）的包直接跳过，不生成任何代码（资源发布不受影响）
3. 校验导出路径有效性，无效则终止
4. 获取 Unity 工程路径（`xxx/Assets`）
5. **依赖检查**：包依赖必须位于任意 Common 包或当前发布包中，存在非法依赖时终止发布
6. 收集包内界面/组件清单 → 依次生成 Win →（非 Launcher）Comp → Binder

## 事件接线（InitUIEvent）与事件白名单

事件注册代码（`AddUIListener(...)`）归属于**手写层**的 `InitUIEvent` 方法：

- 首次生成 `WinXxx.cs` / `CompXxx.cs` 时自动写入方法与注册代码，**之后增删由开发人员维护**（Gen 层每次发布重生成，不再包含该方法）
- 控件字段、`InitUIComp` 赋值、枚举等仍留在 Gen 层自动重生成
- Gen 层 `ConstructFromXML` 调用 `InitUIEvent`（与调用 `OnInit` 同模式）

默认事件白名单（`Src/GenCommon.lua` 的 `COMP_EVENT_CONFIG`，按控件类型配置）：

| 类型 | 默认事件 |
| ---- | -------- |
| GButton | onClick |
| GList | onClickItem |
| GSlider | onChanged |
| GComboBox | onChanged |
| GTextInput | onChanged、onFocusOut、onSubmit |
| GGraph | onClick |
| GRichTextField | onClick、onClickLink（`ctx.data` 为 href） |

未收录的类型（GImage、GLoader 等纯展示居多）不生成事件；需要时在 `COMP_EVENT_CONFIG` 加一行即可，其余链路零改动。

## 文件结构

```
CSharpCodeGen/
├── main.lua                       ← 插件入口（onPublish 发布钩子）
├── Src/
│   ├── GenReady.lua               ← 初始化、路径/依赖校验、界面组件清单收集
│   ├── GenWin.lua                 ← 界面代码生成
│   ├── GenComp.lua                ← 组件代码生成
│   ├── GenBinder.lua              ← 统一绑定代码生成
│   ├── GenCommon.lua              ← 生成通用逻辑
│   └── Tool.lua                   ← 日志工具
├── Template/
│   ├── WinGenTemplate.txt         ← 界面 .Gen.cs 模板
│   ├── WinGenLauncherTemplate.txt ← Launcher 界面 .Gen.cs 模板
│   ├── WinTemplate.txt            ← 界面逻辑层模板
│   ├── CompGenTemplate.txt        ← 组件 .Gen.cs 模板
│   ├── CompTemplate.txt           ← 组件逻辑层模板
│   └── CustomCompBindTemplate.txt ← 统一绑定模板
├── package.json                   ← 插件描述
└── README.md                      ← 本文件
```

## Lua 调试

发布时可用 Rider 的 EmmyLua 插件调试本插件 Lua 代码。临时在 `main.lua` 顶部加入（路径按本机 Rider 安装位置调整，用后移除）：

```lua
package.cpath = package.cpath .. ';C:/Users/<用户名>/AppData/Roaming/JetBrains/Rider<版本>/plugins/EmmyLua/debugger/emmy/windows/x64/?.dll'
local dbg = require('emmy_core')
dbg.tcpListen('localhost', 9966)
```
