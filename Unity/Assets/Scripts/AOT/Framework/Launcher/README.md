# FuFramework Launcher Module

## 1. 简介

**FuFramework Launcher** 模块是 AOT 侧的 MonoBehaviour 入口和核心驱动器。它由两个组件组成，挂载在启动场景的同一 GameObject 上：

1. **Launcher** — 纯 MonoBehaviour 入口，启动 AOT 极简引导流程
2. **GameDriven** — MonoSingleton，帧驱动委托 + 游戏控制（暂停、恢复、重启、退出）

所有框架模块的注册和生命周期已移交 Hotfix 侧（`HotfixLauncher.MainAsync()`）。Launcher 模块仅保留 AOT 入口 + 帧驱动桥接，不再直接引用任何框架模块。

## 2. 特性

- **极简入口**：Launcher 仅负责引导启动，零模块注册代码
- **委托驱动**：GameDriven 持有帧更新委托，HotfixLauncher 挂接 ModuleManager 生命周期方法
- **游戏控制**：GameDriven 提供暂停、恢复、重启、退出游戏
- **自驱动**：GameDriven 自行处理 MonoBehaviour Update/LateUpdate/FixedUpdate，无需外部喂帧
- **反射移交**：`BootstrapProcess` 加载完 Hotfix 程序集后内置反射调用 `HotfixLauncher.MainAsync()` 进入热更逻辑

## 3. 核心类详解

### 3.1 Launcher

游戏启动器，普通 MonoBehaviour，是整个框架的 AOT 入口点。

#### 职责

1. **启动引导流程**：在 `Start()` 中调用 `BootstrapProcess.RunAsync()`
2. **DontDestroyOnLoad**：在 `Awake()` 中确保跨场景存活

#### 生命周期流程

```
Awake() -> Start()
              ↓
         DontDestroyOnLoad -> 启动引导流程 -> 反射进入热更
```

#### 委托挂接（Hotfix 侧）

```csharp
// HotfixLauncher.MainAsync() 中：
GameDriven.Instance.OnUpdate       = ModuleManager.Update;
GameDriven.Instance.OnLateUpdate   = ModuleManager.LateUpdate;
GameDriven.Instance.OnFixedUpdate  = ModuleManager.FixedUpdate;
GameDriven.Instance.DisposeModules = ModuleManager.Dispose;
GameDriven.Instance.ReInitModules  = ModuleManager.ReInit;
```

### 3.2 GameDriven

帧驱动 + 游戏控制中枢，继承自 `MonoSingleton<GameDriven>`。

#### 职责

1. **帧驱动委托**：持有 5 个 Action 委托供 Hotfix 侧挂接
2. **自驱动帧循环**：Update/LateUpdate/FixedUpdate 调用已挂接的委托
3. **游戏控制**：暂停、恢复、重启、退出

#### 委托桥接数据流

```
AOT                                         Hotfix
────                                        ──────

Launcher.Start()
  └── BootstrapProcess.RunAsync()
        └── 加载 Hotfix.dll → 反射调用 HotfixLauncher.MainAsync()

GameDriven.Update()                         ModuleManager.Update(dt, udt)
  └── OnUpdate?.Invoke()  ────────────────→ 遍历所有模块.OnUpdate()

GameDriven.PauseGame()    ← Hotfix UI 调用
GameDriven.RestartGame()
  ├── DisposeModules?.Invoke()  ───────────→ ModuleManager.Dispose
  ├── ReInitModules?.Invoke()   ───────────→ ModuleManager.ReInit
  └── BootstrapProcess.RunAsync()
```

## 4. 目录结构

```
FuFramework/Launcher/
├── README.md                                 # 模块说明文档
├── Runtime/                                  # 运行时代码
│   ├── FuFramework.Launcher.Runtime.asmdef   # 程序集定义
│   ├── Launcher.cs                           # AOT 入口 MonoBehaviour
│   └── GameDriven.cs                         # 帧驱动 + 游戏控制 MonoSingleton
```

> **注意**：`Launcher.GameControl.cs` 已删除。游戏控制功能已合并到 `GameDriven.cs`。

## 5. 依赖

AOT 侧最小依赖：

- **FuFramework.Core.Runtime**：基础工具（FuLogger/Utility/FuException/MonoSingleton）
- **FuFramework.ModuleSetting.Runtime**：模块设置
- **HybridCLR.Runtime**：AOT 元数据补充
- **YooAsset**：资源管理
- **UniTask**：异步操作
- **FairyGUI**：UI 框架（BootstrapView 自包含包）

## 6. 注意事项

1. **GameObject 结构**：Launcher 和 GameDriven 挂载在同一 GameObject 上，`DontDestroyOnLoad` 由 Launcher 在 Awake 中执行
2. **委托安全**：委托默认为 null，HotfixLauncher 挂接前不会执行任何帧更新，`?.Invoke()` 天然安全
3. **Hotfix 侧引用**：挂接委托时使用 `GameDriven.Instance.OnUpdate = ...`，已存在 `using FuFramework.Launcher.Runtime;` 无需额外添加
4. **反射入口**：HotfixLauncher.MainAsync 通过反射调用，AOT 不直接引用 Hotfix 程序集
5. **重启游戏**：`RestartGame()` 通过委托调用 `DisposeModules` + `ReInitModules`，随后重新运行 `BootstrapProcess` 引导流程
