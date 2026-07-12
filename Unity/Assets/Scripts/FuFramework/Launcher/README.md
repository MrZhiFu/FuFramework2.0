# FuFramework Launcher Module

## 1. 简介

**FuFramework Launcher** 模块是 AOT 侧的 MonoBehaviour 入口和核心驱动器。它挂载在启动场景的 GameObject 上，负责：

1. 启动 AOT 极简引导流程（`BootstrapProcess.RunAsync()`）
2. 通过委托桥接驱动 Hotfix 侧的帧更新循环（`ModuleManager.Update/LateUpdate/FixedUpdate`）
3. 提供游戏控制功能（暂停、恢复、重启、退出）

所有框架模块的注册和生命周期已移交 Hotfix 侧（`HotfixLauncher.MainAsync()`）。Launcher 仅保留委托桥接，不再直接引用任何框架模块。

本模块采用**部分类（partial class）**设计：

- `Launcher.cs`：核心生命周期和委托桥接
- `Launcher.GameControl.cs`：游戏控制

## 2. 特性

- **极简入口**：仅负责引导启动 + 委托桥接，零模块注册代码
- **委托驱动**：通过 `public static Action` 委托将帧循环移交 Hotfix 侧 ModuleManager
- **游戏控制**：支持暂停、恢复、重启、退出游戏
- **反射移交**：引导完成后反射调用 `HotfixLauncher.MainAsync()` 进入热更逻辑

## 3. 核心类详解

### 3.1 Launcher

游戏启动器，继承自 `MonoSingleton<Launcher>`，是整个框架的 AOT 入口点。

#### 职责

1. **启动引导流程**：在 `Start()` 中调用 `BootstrapProcess.RunAsync()`
2. **驱动帧循环**：通过委托桥接 Hotfix 侧的 `ModuleManager`
3. **游戏控制**：提供暂停、恢复、重启、退出游戏功能

#### 生命周期流程

```
OnInit() -> Start() -> Update()/LateUpdate()/FixedUpdate()
   ↓
初始化日志 -> 启动引导流程 -> 引导完成回调 -> 反射进入热更 -> 驱动帧循环
```

#### 代码结构

```csharp
public partial class Launcher : MonoSingleton<Launcher>
{
    // 委托桥接 —— HotfixLauncher 在注册模块后挂接
    public static Action<float, float> OnUpdate;
    public static Action<float, float> OnLateUpdate;
    public static Action OnFixedUpdate;
    public static Action DisposeModules;
    public static Action ReInitModules;

    protected override void OnInit()
    {
        SRDebug.Init();
    }

    private void Start()
    {
        // 启动 AOT 极简引导流程，引导完成后回调 InvokeHotfixEntryAsync 进入热更入口
        BootstrapProcess.RunAsync(InvokeHotfixEntryAsync).Forget();
    }

    private void Update()
    {
        OnUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void LateUpdate()
    {
        OnLateUpdate?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void FixedUpdate()
    {
        OnFixedUpdate?.Invoke();
    }

    private static async UniTask InvokeHotfixEntryAsync(BootstrapView view)
    {
        // 反射调用 Hotfix.HotfixLauncher.MainAsync(IBootstrapView)
        var hotfixAssembly = GetHotfixAssembly();
        var entryType  = hotfixAssembly.GetType("Hotfix.HotfixLauncher");
        var mainMethod = entryType?.GetMethod("MainAsync", BindingFlags.Public | BindingFlags.Static);
        await (UniTask)mainMethod.Invoke(null, new object[] { view });
    }
}
```

#### 委托挂接（Hotfix 侧）

```csharp
// HotfixLauncher.MainAsync() 中：
FuFramework.Launcher.Runtime.Launcher.OnUpdate       = ModuleManager.Update;
FuFramework.Launcher.Runtime.Launcher.OnLateUpdate   = ModuleManager.LateUpdate;
FuFramework.Launcher.Runtime.Launcher.OnFixedUpdate  = ModuleManager.FixedUpdate;
FuFramework.Launcher.Runtime.Launcher.DisposeModules = ModuleManager.Dispose;
FuFramework.Launcher.Runtime.Launcher.ReInitModules  = ModuleManager.ReInit;
```

### 3.2 Launcher.GameControl

游戏控制部分，提供游戏级别的控制功能。

#### 方法

| 方法 | 说明 |
|------|------|
| `PauseGame()` | 暂停游戏（通过 ModuleSetting） |
| `ResumeGame()` | 恢复游戏（通过 ModuleSetting） |
| `RestartGame()` | 重启游戏（释放并重新初始化所有模块） |
| `QuitGame()` | 退出游戏（释放模块并退出应用） |

#### 重启游戏流程

```csharp
public void RestartGame()
{
    DisposeModules?.Invoke();  // 释放所有模块
    ReInitModules?.Invoke();   // 重新初始化所有模块
    // 重新开始引导流程...
}
```

## 4. 委托桥接数据流

```
AOT Launcher                     HotfixLauncher                     ModuleManager
───────────                      ─────────────                      ─────────────
Update()                         MainAsync()                        Update(dt, udt)
  └── OnUpdate?.Invoke()  ──→     OnUpdate = ModuleManager.Update ──→ 遍历所有模块.OnUpdate()
LateUpdate()
  └── OnLateUpdate?.Invoke() ──→ OnLateUpdate = ...             ──→ 遍历所有模块.OnLateUpdate()
FixedUpdate()
  └── OnFixedUpdate?.Invoke() ──→ OnFixedUpdate = ...           ──→ 遍历所有模块.OnFixedUpdate()

GameControl.RestartGame()
  ├── DisposeModules?.Invoke() ──→ DisposeModules = ModuleManager.Dispose   ──→ 遍历所有模块.OnDispose()
  └── ReInitModules?.Invoke()  ──→ ReInitModules  = ModuleManager.ReInit    ──→ 遍历所有模块.OnInit()
```

## 5. GlobalModule

`GlobalModule` 已随模块系统下沉到 Hotfix 程序集（`Hotfix/Framework/Core/GlobalModule.cs`），供热更侧代码使用。

通过 `GlobalModule.<模块名>` 全局访问各模块（内部采用延迟初始化单例）：

```csharp
GlobalModule.AssetModule.LoadAssetAsync<GameObject>("Prefab/Player");
GlobalModule.EventModule.Subscribe<GameStartEventArgs>(OnGameStart);
GlobalModule.UIModule.OpenUI<WinLogin>();
```

## 6. 目录结构

```
FuFramework/Launcher/
├── README.md                                 # 模块说明文档
├── Runtime/                                  # 运行时代码
│   ├── FuFramework.Launcher.Runtime.asmdef   # 程序集定义
│   ├── Launcher.cs                           # 核心启动器 + 委托桥接
│   └── Launcher.GameControl.cs               # 游戏控制
└── Editor/                                   # 编辑器代码
    └── Inspector/
        └── LauncherInspector.cs              # Launcher Inspector
```

> **注意**：`Launcher.Modules.cs` 和 `Launcher.Procedures.cs` 已删除。模块注册已移交 `HotfixLauncher.MainAsync()`，流程管理已由 AOT 侧 `BootstrapProcess` 替代。

## 7. 依赖

AOT 侧最小依赖：

- **FuFramework.Core.Runtime**：基础工具（FuLogger/Utility/FuException）
- **FuFramework.ModuleSetting.Runtime**：模块设置
- **HybridCLR.Runtime**：AOT 元数据补充
- **YooAsset**：资源管理
- **UniTask**：异步操作
- **FairyGUI**：UI 框架（BootstrapView 自包含包）

## 8. 注意事项

1. **单例模式**：Launcher 继承自 `MonoSingleton`，确保场景中只有一个实例
2. **委托安全**：委托默认为 null，HotfixLauncher 挂接前不会执行任何帧更新
3. **全限定类名**：Hotfix 侧引用 Launcher 委托时必须使用 `FuFramework.Launcher.Runtime.Launcher.OnUpdate`（避免与 `global::Launcher` 命名空间冲突）
4. **反射入口**：HotfixLauncher.MainAsync 通过反射调用，AOT 不直接引用 Hotfix 程序集
5. **重启游戏**：`RestartGame` 通过委托调用 `DisposeModules` + `ReInitModules`
6. **场景引用**：`Launcher.unity` 场景中若有旧版 Procedure 引用需在 Unity Editor 中清理
