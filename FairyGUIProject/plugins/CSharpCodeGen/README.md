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
- 静态多语言文本：`s_xxxTxt`，表示需要填入多语言 key

## 发布执行链（onPublish）

1. 初始化发布处理器与通用工具（`GenReady:Init`）
2. 校验导出路径有效性，无效则终止
3. 获取 Unity 工程路径（`xxx/Assets`）
4. **依赖检查**：包依赖必须位于任意 Common 包或当前发布包中，存在非法依赖时终止发布
5. 收集包内界面/组件清单 → 依次生成 Win →（非 Launcher）Comp → Binder

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
