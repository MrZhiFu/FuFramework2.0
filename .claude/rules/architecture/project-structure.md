---
paths:
  - "Unity/Assets/Scripts/**"
---

# 项目结构

## AOT / 热更双层（HybridCLR）

| 层 | 目录 | 内容 | 可热更 |
|---|---|---|---|
| AOT | `Unity/Assets/Scripts/AOT/` | 极简引导：`Launcher.cs`、`Launch/`（LaunchProcess/LaunchView/ILaunchView/LaunchAssetHelper/UpdateConfig/UI/Localization）、`Framework/`（Extension/Log/Utility）、`GameSetting.cs` | 否，随包 |
| 热更 | `Unity/Assets/Scripts/Hotfix/` | 全部框架模块 + 全部游戏逻辑 | 是，加载 DLL |

**铁律：AOT 绝不能引用 Hotfix**；热更可引用 AOT。AOT 不含任何框架模块注册代码。

构建产物：AOT DLL → `Assets/Bundles/AOTCode/`，热更 DLL → `Assets/Bundles/Code/`，均为 `.bytes` 供 YooAsset 分发。菜单 `FuFramework/Build/Copy AOT Code(...)`、`FuFramework/Build/Copy Hotfix Code(...)`（完整名见 `BuildHotfixHelper.cs`；用 `execute_menu_item` 需完整名）。

## 热更框架模块（21 个）

`Unity/Assets/Scripts/Hotfix/Framework/` 下，每个模块一个目录：

Asset · Config · Core · Download · Entity · Event · FSM · Guide · Localization · Model · Mono · Network · ObjectPool · Procedure · RedDot · Scene · Sound · Storage · Timer · UI · Web

`Core/` 存放模块基座（ModuleBase/ModuleManager/GameDriven），不是业务模块。

## 游戏层

`Unity/Assets/Scripts/Hotfix/Game/`：`AutoGen/`（生成代码：Tables/Proto/MessageIds/UI）、`Events/`（事件定义）、`Model/`（业务数据）、`Network/`（协议处理与会话流）、`UI/`（界面）、`Manager_ToDelete/`（**待删除的旧实现，勿照抄其中写法**）。

## 程序集

- 用 `.asmdef` 定义。实际仅有四个：`AOT.asmdef`、`HotFix.asmdef`、`ProtoBuffer.Runtime.asmdef`、`Unity.Editor.asmdef`。
- **框架模块本体全部在 `Hotfix` 程序集，不拆 Runtime/Editor 子程序集**（磁盘上残留的 `FuFramework.*.Runtime.csproj` 为历史产物，非现行结构）。
- Editor 工具集中在 `Assets/Editor/`，归 `Unity.Editor` 程序集。

## 三方与工具

- 第三方：`Unity/Assets/3rdPlugins/`（FairyGUI、SRDebugger、NiceVibrations）。
- 配置表源：`Config/Excels/`；生成代码到 `Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables/`。
- 协议源：`Protobuf/Proto/`；导出 `Proto2CsExport-All.bat`（必须带 `ci` 参数）。
- Unity 版本 2022.3.33f1c1；关键依赖 YooAsset 3.0.5、HybridCLR、UniTask、FairyGUI、protobuf-net、PrimeTween、Newtonsoft.Json。
- 项目**无自动化测试套件**，验证走 Unity Editor 实机 + `bash .claude/fu-compile.sh`。

## 平台与宏

条件编译 `#if UNITY_EDITOR` / `#if UNITY_WEBGL` 等；自定义宏经 `FuFramework/脚本编译宏定义设置` 菜单管理（`ENABLE_WECHAT_MINI_GAME`、`ENABLE_DOUYIN_MINI_GAME` 等）。日志分级宏：`ENABLE_LOG`、`ENABLE_INFO_LOG`、`ENABLE_DEBUG_AND_ABOVE_LOG`。

## 模块文档

每个模块有 `README.md`：`Unity/Assets/Scripts/Hotfix/Framework/<模块>/README.md`。改模块前先读它。
