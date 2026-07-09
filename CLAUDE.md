# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在本仓库中工作时提供指导。

## 交流语言

全程使用中文交流。

## 常用命令

### 配置表生成 (Luban)
```bash
# 编译 Luban 工具（首次使用，或 Luban 源码有改动时）
cd Config/Tools && ./build-luban.bat   # 或 build-luban.sh

# 生成客户端二进制配置 + 本地化Key
cd Config && ./gen-client-bin.bat
# 生成客户端 JSON 配置
cd Config && ./gen-client-json.bat
# 生成服务端二进制配置
cd Config && ./gen-server-bin.bat
# 生成服务端 JSON 配置
cd Config && ./gen-server-json.bat
```

### Protobuf 协议导出
```bash
# 同时导出客户端和服务端 C# 协议代码
cd Protobuf && ./Proto2CsExport-All.bat
# 或单独导出:
cd Protobuf && ./Proto2CsExport_Client.bat
cd Protobuf && ./Proto2CsExport_Server.bat
```

### 服务端
```bash
# 快速启动游戏服务器
cd Server/FuFramework.Launcher && ./quick_start-game.bat
# 服务端解决方案: Server/Server.sln（使用 dotnet build）
```

### Unity
- **编辑器版本**: 2022.3.33f1c1
- 打开 `Unity/` 作为 Unity 项目，通过编辑器或 Unity CLI 构建。
- 关键依赖（参见 `Unity/Packages/manifest.json`）：YooAsset 2.3.18、HybridCLR (git)、UniTask (git)、FairyGUI（内嵌在 Assets 中）、protobuf-net、PrimeTween、Newtonsoft.Json。

当前项目没有自动化测试套件，测试采用在 Unity Editor 中手动进行。

## 架构：AOT / 热更分离 (HybridCLR)

本项目使用 **HybridCLR** 实现 C# 代码热更。代码分为两层：

- **AOT** (`Unity/Assets/Scripts/AOT/`) — 随包体预编译。负责启动、资源更新、加载热更 DLL，通过一套流程链驱动：`ProcedureLauncher → ProcedureReqRemoteUpdateConfig → ProcedureInitPackage → ProcedureGetPackageVersion → ProcedureUpdatePackageManifest → ProcedureCreateDownloader → ProcedureDownloadPackage → ProcedureUpdateDone → ProcedureCodeHotfix`。加载完热更程序集 (`Game.Hotfix`) 后，调用 `HotfixLauncher.Main()` 进入热更逻辑。

- **热更** (`Unity/Assets/Scripts/Hotfix/`) — 编译为 DLL，运行时加载。包含所有游戏业务逻辑（UI、网络消息处理、配置读取等）。热更程序集可以引用 AOT 程序集，但**绝不能反过来**。

AOT DLL 会被拷贝到 `Assets/Bundles/AOTCode/`，热更 DLL 拷贝到 `Assets/Bundles/Code/`，均以 `.bytes` 格式供 YooAsset 分发。编辑器菜单：`FuFramework/Build/Copy AOT Code` 和 `FuFramework/Build/Copy Hotfix Code`。

## 框架架构

### 模块系统（核心模式）
每个框架功能都是一个 **`FuModule`**（继承自 `MonoBehaviour`，但生命周期由框架驱动，继承 MonoBehaviour 仅为了在 Inspector 面板中方便展示模块信息）。模块按顺序注册（参见 `Launcher.Modules.cs`），由 `ModuleManager` 统一管理。注册顺序不可随意修改——基础模块（ReferencePool、ObjectPool、FSM、Procedure、Event、Coroutine、Mono、Timer）必须先于功能模块（Asset、Download、Network、UI 等）注册。

通过 `GlobalModule.<模块名>` 全局访问各模块（内部采用延迟初始化单例）。

### 模块依赖
使用 `[ModuleDependency(typeof(依赖模块))]` 特性标记模块，确保初始化顺序。例如：`ProcedureModule` 依赖 `FsmModule`。

### 游戏流程：Procedure 系统
基于 FSM 模块构建。`ProcedureBase` 子类定义游戏的各个阶段（启动、闪屏、登录、主菜单、战斗等）。生命周期：`OnInit`、`OnEnter`、`OnUpdate`、`OnLeave`。流程切换通过 `ChangeState<T>()` 实现。流程在 Launcher 的 Inspector 面板中配置——编辑器会自动扫描所有 `ProcedureBase` 子类。

### 资源管理 (YooAsset)
`AssetModule` 封装 YooAsset，支持四种运行模式：
- `EditorSimulateMode` — 编辑器模拟模式，无需构建 AssetBundle
- `OfflinePlayMode` — 单机模式，从 StreamingAssets 读取
- `HostPlayMode` — 联机模式，支持 CDN 热更新
- `WebPlayMode` — WebGL/小游戏适配模式

### 网络层
`NetworkModule` 管理多个命名频道，每个频道独立配置。频道根据平台自动选择 TCP 或 WebSocket（WebGL 强制使用 WebSocket）。消息类型通过接口标记：`IRequestMessage`、`IResponseMessage`、`INotifyMessage`、`IHeartBeatMessage`。消息处理器通过 `[MessageHandler]` 特性注册。支持 RPC 调用：`channel.Call<TResponse>(request)`。

### UI 系统 (FairyGUI)
`ViewBase` — 所有 UI 界面的基类。每个界面属于一个 `UILayer`（WorldUI、MainUI、Normal、Window、Tips、Guide、Loading），层级值越大显示越靠前。每个层级对应一个 `UIGroup`。UI 包通过 `FuiPkgManager` 加载，支持引用计数和依赖自动加载。UI 实例通过 `ObjectPool<ViewObject>` 进行对象池管理。

### 本地数据存储
`StorageModule` — 持久化键值存储，支持多文件分离（每个 `StorageHelper` 对应一个文件）、AES 加密、自动保存和脏数据检测。数据以二进制格式存储（文件头为 `GMD` 标识），存放路径为 `Application.persistentDataPath/GameData/`。

### 配置系统
使用 Luban 工具从 `Config/Excels/` 中的 Excel 文件生成 C# 配置类。生成的代码输出到 `Unity/Assets/Scripts/Hotfix/Config/Generate/`。运行时通过 `GlobalModule.ConfigModule.GetConfig<T>()` 获取配置。自定义文件命名规则：`L-<表名>.xlsx` → `Tb<表名>`。

## 关键约定

- **程序集定义**：使用 `.asmdef` 文件。每个框架模块拆分为 Runtime 和 Editor 两个程序集（如 `FuFramework.Network.Runtime`、`FuFramework.Network.Editor`）。
- **平台适配**：使用条件编译（`#if UNITY_EDITOR`、`#if UNITY_WEBGL` 等）和自定义宏定义。宏通过 `FuFramework/相关的脚本编译宏定义设置` 菜单管理（如 `ENABLE_WECHAT_MINI_GAME`、`ENABLE_DOUYIN_MINI_GAME`）。
- **日志系统**：使用 `FuLogger`，支持分级条件编译控制：`ENABLE_LOG`、`ENABLE_INFO_LOG`、`ENABLE_DEBUG_AND_ABOVE_LOG` 等。
- **第三方代码**：放在 `Unity/Assets/FuFramework/3rdPlugins/` 下（FairyGUI、SRDebugger、NiceVibrations）。
- **模块文档**：每个模块都有详细的 README 文档，位于 `Unity/Assets/FuFramework/<模块名>/README.md`。

## 代码风格规范

**必须严格遵守**。完整规范文档：`Docs/FuFramework代码风格规范.md`

写任何 `.cs` 文件时，遵循以下硬性规则：

### 命名
- 私有字段：`m_` 前缀 + PascalCase（`m_FsmDict`、`m_OnConfirm`）
- 手写枚举：`E` 前缀（`EUILayer`、`ENetworkErrorCode`）
- 异步方法：必须加 `Async` 后缀（`LoginAsync()`、`LoadConfigAsync()`）
- 常量：PascalCase，不使用 ALL_CAPS（`HotfixDllName`，不是 `HOTFIX_DLL_NAME`）
- 局部变量/参数：camelCase + `var`（`var winLauncher`、`itemId`）

### 注释
- 全部使用 **中文**，XML 文档注释（`///`）为主
- 每个类、方法、属性、字段、枚举成员都要写 `<summary>`
- TODO 标记：`// TODO：xxx`

### 格式
- 缩进：Tab
- 括号：K&R 风格（左括号同行）
- 访问修饰符：始终显式声明

### 异常
- 自动生成代码不检查：Luban 生成 (`Config/Generate/`)、Proto 生成、FGUI `.Gen.cs` 文件、`UnityWebSocket` 命名空间

### 代码组织
- 单文件不超过 500 行，超过拆分为 partial class
- Region 名称用中文
- 生命周期拆分为：`InitUIComp()` → `InitUIEvent()` → `InitEvent()` → `InitRedDot()`
