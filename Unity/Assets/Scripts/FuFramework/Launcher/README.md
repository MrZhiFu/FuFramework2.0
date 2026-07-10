# FuFramework Launcher Module

## 1. 简介

**FuFramework Launcher** 模块是整个游戏框架的启动入口和核心驱动器。它负责初始化框架核心、注册所有功能模块、加载并启动游戏流程（Procedure），并提供了一个全局静态访问点（`GlobalModule`）以便于访问各个功能模块。

本模块采用**部分类（partial class）**设计，将功能划分为多个文件：
- `Launcher.cs`：核心生命周期和驱动
- `Launcher.Modules.cs`：模块注册
- `Launcher.Procedures.cs`：流程管理
- `Launcher.GameControl.cs`：游戏控制

***

## 2. 特性

- **模块化注册**：按正确顺序自动注册所有框架模块
- **流程驱动**：基于有限状态机的游戏流程管理
- **全局访问**：通过 `GlobalModule` 静态类快速访问所有模块
- **生命周期管理**：驱动框架的 Update/LateUpdate/FixedUpdate
- **游戏控制**：支持暂停、恢复、重启、退出游戏
- **可视化配置**：Inspector 面板支持流程选择和优先级排序

***

## 3. 核心类详解

### 3.1 Launcher

游戏启动器，继承自 `MonoSingleton<Launcher>`，是整个框架的入口点。

#### 职责

1. **注册框架模块**：在 `Start` 中调用 `RegisterModules()` 注册所有模块
2. **启动游戏流程**：初始化并启动入口流程
3. **驱动框架生命周期**：在 Update/LateUpdate/FixedUpdate 中驱动模块管理器
4. **游戏控制**：提供暂停、恢复、重启、退出游戏功能

#### 生命周期流程

```
OnInit() -> Start() -> Update()/LateUpdate()/FixedUpdate()
   ↓
初始化日志 -> 注册模块 -> 启动流程 -> 驱动框架轮询
```

#### 代码结构

```csharp
public partial class Launcher : MonoSingleton<Launcher>
{
    protected override void OnInit()
    {
        // 初始化运行时日志查看器
        SRDebug.Init();
    }

    private void Start()
    {
        RegisterModules();      // 注册框架模块
        StartProcedure();       // 开始游戏流程
    }

    private void Update()
    {
        ModuleManager.Update(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void LateUpdate()
    {
        ModuleManager.LateUpdate(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void FixedUpdate()
    {
        ModuleManager.FixedUpdate();
    }
}
```

### 3.2 Launcher.Modules

模块注册部分，负责按正确顺序注册所有框架模块。

#### 模块注册顺序

```csharp
private void RegisterModules()
{
    // 基础模块（最先注册）
    ModuleManager.RegisterModule<ReferencePoolModule>(); // 引用池管理模块
    ModuleManager.RegisterModule<ObjectPoolModule>();    // 对象池管理模块
    ModuleManager.RegisterModule<FsmModule>();           // 有限状态机管理模块
    ModuleManager.RegisterModule<ProcedureModule>();     // 流程管理模块
    ModuleManager.RegisterModule<EventModule>();         // 事件管理模块
    ModuleManager.RegisterModule<CoroutineModule>();     // 协程管理模块
    ModuleManager.RegisterModule<MonoModule>();          // Mono管理模块
    ModuleManager.RegisterModule<TimerModule>();         // 计时器管理模块
    
    // 功能模块
    ModuleManager.RegisterModule<AssetModule>();         // 资源管理模块
    ModuleManager.RegisterModule<DownloadModule>();      // 下载管理模块
    ModuleManager.RegisterModule<DataSaveModule>();      // 本地存储数据管理模块
    ModuleManager.RegisterModule<GlobalConfigModule>();  // 全局配置管理模块
    ModuleManager.RegisterModule<ConfigModule>();        // 配置管理模块
    ModuleManager.RegisterModule<SoundModule>();         // 声音管理模块
    ModuleManager.RegisterModule<EntityModule>();        // 实体管理模块
    ModuleManager.RegisterModule<NetworkModule>();       // 网络管理模块
    ModuleManager.RegisterModule<UIModule>();            // UI管理模块
    ModuleManager.RegisterModule<WebModule>();           // Web管理模块
}
```

#### 注册顺序说明

**注意**：注册顺序不可修改，某些模块依赖于其他模块：
1. **引用池/对象池**：最基础，其他模块可能使用
2. **FSM/Procedure**：流程管理依赖状态机
3. **Event/Coroutine/Mono/Timer**：基础服务模块
4. **Asset/Download/DataSave**：资源相关模块
5. **GlobalConfig/Config**：配置模块
6. **Sound/Entity/Network**：功能模块
7. **UI/Guide/RedDot/Localization/Model/Web**：上层业务模块

### 3.3 Launcher.Procedures

流程管理部分，负责初始化和管理游戏流程。

#### 配置字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `m_AvailableProcedureTypeNames` | `string[]` | 所有可用的流程类型名称数组 |
| `m_EntryProcedureTypeName` | `string` | 入口流程类型名称 |

#### 公开属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `CurrentProcedure` | `ProcedureBase` | 获取当前正在运行的流程 |

#### 流程启动流程

```csharp
private void StartProcedure()
{
    // 1. 获取所有流程实例
    var states = GetProcedures();
    
    // 2. 初始化流程管理模块
    var procedureModule = ModuleManager.GetModule<ProcedureModule>();
    procedureModule.InitProcedures(states);
    
    // 3. 启动入口流程
    procedureModule.StartProcedure(m_EntryProcedure.GetType());
}
```

#### 流程获取逻辑

```csharp
private ProcedureBase[] GetProcedures()
{
    // 1. 遍历所有配置的流程类型名称
    for (var i = 0; i < m_AvailableProcedureTypeNames.Length; i++)
    {
        // 2. 通过反射获取类型
        var procedureType = Utility.Assembly.GetType(m_AvailableProcedureTypeNames[i]);
        
        // 3. 创建流程实例
        m_Procedures[i] = Activator.CreateInstance(procedureType) as ProcedureBase;
        
        // 4. 识别入口流程
        if (m_EntryProcedureTypeName == m_AvailableProcedureTypeNames[i])
        {
            m_EntryProcedure = m_Procedures[i];
        }
    }
    
    return states;
}
```

### 3.4 Launcher.GameControl

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
    // 1. 释放所有模块
    ModuleManager.Dispose();
    
    // 2. 重新初始化所有模块
    ModuleManager.ReInit();
    
    // 3. 重新开始游戏流程
    StartProcedure();
}
```

#### 退出游戏流程

```csharp
public void QuitGame()
{
    // 1. 释放所有模块
    ModuleManager.Dispose();
    
    // 2. 退出应用
    Application.Quit();
    
#if UNITY_EDITOR
    // 编辑器模式下停止播放
    UnityEditor.EditorApplication.isPlaying = false;
#endif
}
```

### 3.5 GlobalModule

全局模块访问入口，静态类，提供对所有框架模块的便捷访问。

#### 设计模式

采用**延迟初始化（Lazy Initialization）**模式：

```csharp
public static class GlobalModule
{
    private static AssetModule m_AssetModule;
    
    public static AssetModule AssetModule => 
        m_AssetModule ??= ModuleManager.GetModule<AssetModule>();
}
```

#### 提供的模块访问

| 属性 | 模块 | 说明 |
|------|------|------|
| `ReferencePoolModule` | 引用池模块 | 管理可复用对象的引用 |
| `ObjectPoolModule` | 对象池模块 | 管理 GameObject 对象池 |
| `EventModule` | 事件管理模块 | 全局事件订阅和广播 |
| `AssetModule` | 资源管理模块 | 资源加载和管理 |
| `ConfigModule` | 配置管理模块 | 游戏配置数据管理 |
| `CoroutineModule` | 协程管理模块 | 全局协程管理 |
| `TimerModule` | 计时器管理模块 | 定时器和倒计时 |
| `DownloadModule` | 下载管理模块 | 文件下载管理 |
| `EntityModule` | 实体管理模块 | 游戏实体生命周期 |
| `FsmModule` | 有限状态机模块 | 状态机管理 |
| `ProcedureModule` | 流程管理模块 | 游戏流程控制 |
| `UIModule` | UI 管理模块 | UI 窗口管理 |
| `GlobalConfigModule` | 全局配置模块 | 服务器配置管理 |
| `MonoModule` | Mono 管理模块 | MonoBehaviour 管理 |
| `NetworkModule` | 网络管理模块 | 网络通信 |
| `WebModule` | Web 管理模块 | Web 请求管理 |

#### 使用对比

```csharp
// 传统方式（需要知道模块类型，代码冗长）
var assetModule = ModuleManager.GetModule<AssetModule>();
assetModule.LoadAssetAsync<GameObject>("Prefab/Player");

// GlobalModule 方式（简洁直观）
GlobalModule.AssetModule.LoadAssetAsync<GameObject>("Prefab/Player");
```

### 3.6 LauncherInspector

Launcher 的自定义 Inspector 编辑器，提供可视化配置界面。

#### 功能特性

1. **自动扫描**：自动扫描工程中所有继承自 `ProcedureBase` 的类
2. **优先级排序**：根据流程类的 `Priority` 属性自动排序显示
3. **复选框选择**：通过复选框选择需要启用的流程
4. **下拉选择入口**：从已选流程中选择入口流程
5. **运行时调试**：游戏运行时显示当前激活的流程名称
6. **编译自动刷新**：代码编译后自动刷新流程列表

#### 界面布局

```
┌─────────────────────────────────────┐
│  Launcher (Script)                  │
├─────────────────────────────────────┤
│  当前流程：ProcedureLaunch          │  ← 运行时显示
├─────────────────────────────────────┤
│  所有可用的流程类型：                │
│  ☑ ProcedureLaunch (Priority: 0)   │
│  ☑ ProcedureSplash (Priority: 1)   │
│  ☑ ProcedureMenu (Priority: 10)    │
│  ☐ ProcedureBattle (Priority: 20)  │
├─────────────────────────────────────┤
│  入口流程：ProcedureLaunch          │  ← 下拉选择
└─────────────────────────────────────┘
```

#### 优先级缓存机制

```csharp
private readonly Dictionary<string, int> m_ProcedurePriorityCache = new();

private int GetProcedurePriority(string procedureTypeName)
{
    // 1. 检查缓存
    if (m_ProcedurePriorityCache.TryGetValue(procedureTypeName, out var priority))
    {
        return priority;
    }
    
    // 2. 反射获取类型
    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.FullName == procedureTypeName);
    
    // 3. 创建实例获取 Priority 属性
    if (Activator.CreateInstance(type) is ProcedureBase instance)
    {
        priority = instance.Priority;
        m_ProcedurePriorityCache[procedureTypeName] = priority;
    }
    
    return priority;
}
```

***

## 4. 使用示例

### 4.1 创建启动场景

```csharp
// 1. 创建新场景 File -> New Scene
// 2. 保存场景为 LaunchScene.unity
// 3. 创建空 GameObject，命名为 "Launcher"
// 4. 挂载 Launcher 组件
```

### 4.2 配置流程

在 Inspector 面板中：

```csharp
// 1. 勾选需要的流程类型
☑ ProcedureLaunch      // 启动流程
☑ ProcedureSplash      // 闪屏流程
☑ ProcedureLogin       // 登录流程
☑ ProcedureMenu        // 主菜单流程
☑ ProcedureBattle      // 战斗流程

// 2. 选择入口流程
入口流程：ProcedureLaunch  // 游戏启动时首先进入的流程
```

### 4.3 编写自定义流程

```csharp
using FuFramework.Procedure.Runtime;

/// <summary>
/// 游戏启动流程
/// </summary>
public class ProcedureLaunch : ProcedureBase
{
    // 设置流程优先级（越小越靠前）
    public override int Priority => 0;

    protected override void OnEnter(IFsm<ProcedureModule> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        
        FuLogger.LogInfo("[ProcedureLaunch] 进入启动流程");
        
        // 初始化游戏数据
        InitializeGameData();
        
        // 检查资源更新
        CheckResourceUpdate();
    }

    private void InitializeGameData()
    {
        // 加载本地配置
        var configModule = GlobalModule.ConfigModule;
        configModule.LoadAllConfigs();
    }

    private async void CheckResourceUpdate()
    {
        // 检查资源版本
        var assetModule = GlobalModule.AssetModule;
        var needUpdate = await assetModule.CheckUpdateAsync();
        
        if (needUpdate)
        {
            // 需要更新，进入更新流程
            ChangeState<ProcedureUpdate>(procedureOwner);
        }
        else
        {
            // 无需更新，进入闪屏流程
            ChangeState<ProcedureSplash>(procedureOwner);
        }
    }
}

/// <summary>
/// 闪屏流程
/// </summary>
public class ProcedureSplash : ProcedureBase
{
    public override int Priority => 1;
    
    private float m_Timer;
    private const float SPLASH_DURATION = 3f;

    protected override void OnEnter(IFsm<ProcedureModule> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        
        // 显示闪屏UI
        GlobalModule.UIModule.Open<UISplash>();
        
        m_Timer = 0f;
    }

    protected override void OnUpdate(IFsm<ProcedureModule> procedureOwner, float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(procedureOwner, deltaTime, unscaledDeltaTime);
        
        m_Timer += deltaTime;
        if (m_Timer >= SPLASH_DURATION)
        {
            // 闪屏结束，进入登录流程
            ChangeState<ProcedureLogin>(procedureOwner);
        }
    }

    protected override void OnLeave(IFsm<ProcedureModule> procedureOwner, bool isShutdown)
    {
        // 关闭闪屏UI
        GlobalModule.UIModule.Close<UISplash>();
        
        base.OnLeave(procedureOwner, isShutdown);
    }
}

/// <summary>
/// 登录流程
/// </summary>
public class ProcedureLogin : ProcedureBase
{
    public override int Priority => 10;

    protected override void OnEnter(IFsm<ProcedureModule> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        
        // 打开登录UI
        GlobalModule.UIModule.Open<UILogin>();
        
        // 订阅登录成功事件
        GlobalModule.EventModule.Subscribe<LoginSuccessEventArgs>(OnLoginSuccess);
    }

    private void OnLoginSuccess(object sender, LoginSuccessEventArgs e)
    {
        // 登录成功，进入主菜单
        ChangeState<ProcedureMenu>(procedureOwner);
    }

    protected override void OnLeave(IFsm<ProcedureModule> procedureOwner, bool isShutdown)
    {
        // 取消订阅事件
        GlobalModule.EventModule.Unsubscribe<LoginSuccessEventArgs>(OnLoginSuccess);
        
        // 关闭登录UI
        GlobalModule.UIModule.Close<UILogin>();
        
        base.OnLeave(procedureOwner, isShutdown);
    }
}
```

### 4.4 使用 GlobalModule 访问模块

```csharp
public class GameManager : MonoBehaviour
{
    private void Start()
    {
        // 播放背景音乐
        GlobalModule.SoundModule.PlayMusic("BGM_Main", loop: true);
        
        // 加载玩家预制体
        GlobalModule.AssetModule.LoadAssetAsync<GameObject>("Prefab/Player", (asset) =>
        {
            Instantiate(asset);
        });
        
        // 订阅事件
        GlobalModule.EventModule.Subscribe<PlayerDeadEventArgs>(OnPlayerDead);
        
        // 启动计时器
        GlobalModule.TimerModule.AddTimer(5f, () =>
        {
            FuLogger.LogInfo("5秒计时结束");
        });
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件
        GlobalModule.EventModule.Unsubscribe<PlayerDeadEventArgs>(OnPlayerDead);
    }
    
    private void OnPlayerDead(object sender, PlayerDeadEventArgs e)
    {
        // 播放死亡音效
        GlobalModule.SoundModule.PlaySFX("SFX_PlayerDead");
        
        // 显示死亡UI
        GlobalModule.UIModule.Open<UIGameOver>();
    }
}
```

### 4.5 游戏控制

```csharp
public class GamePauseManager : MonoBehaviour
{
    // 暂停游戏
    public void OnPauseButtonClicked()
    {
        Launcher.Instance.PauseGame();
        GlobalModule.UIModule.Open<UIPause>();
    }
    
    // 恢复游戏
    public void OnResumeButtonClicked()
    {
        Launcher.Instance.ResumeGame();
        GlobalModule.UIModule.Close<UIPause>();
    }
    
    // 重启游戏
    public void OnRestartButtonClicked()
    {
        // 显示确认对话框
        GlobalModule.UIModule.Open<UIConfirmDialog>("确定要重启游戏吗？", () =>
        {
            Launcher.Instance.RestartGame();
        });
    }
    
    // 退出游戏
    public void OnQuitButtonClicked()
    {
        // 显示确认对话框
        GlobalModule.UIModule.Open<UIConfirmDialog>("确定要退出游戏吗？", () =>
        {
            Launcher.Instance.QuitGame();
        });
    }
}
```

***

## 5. 目录结构

```
FuFramework/Launcher/
├── README.md                              # 模块说明文档
├── Runtime/                               # 运行时代码
│   ├── FuFramework.Launcher.Runtime.asmdef   # 程序集定义
│   ├── Launcher.cs                        # 核心启动器
│   ├── Launcher.Modules.cs                # 模块注册
│   ├── Launcher.Procedures.cs             # 流程管理
│   ├── Launcher.GameControl.cs            # 游戏控制
│   └── GlobalModule.cs                    # 全局模块访问
└── Editor/                                # 编辑器代码
    ├── FuFramework.Launcher.Editor.asmdef    # 编辑器程序集定义
    └── Inspector/
        └── LauncherInspector.cs             # Launcher Inspector
```

***

## 6. 依赖

- **FuFramework.Core**：基础框架模块
- **FuFramework.Procedure**：流程管理模块
- **FuFramework.Fsm**：有限状态机模块
- **FuFramework.ModuleSetting**：配置管理模块
- **FuFramework.ReferencePool**：引用池模块
- **FuFramework.ObjectPool**：对象池模块
- **FuFramework.Event**：事件管理模块
- **FuFramework.Asset**：资源管理模块
- **FuFramework.Config**：配置管理模块
- **FuFramework.Coroutine**：协程管理模块
- **FuFramework.Timer**：计时器管理模块
- **Hotfix.Download**：下载管理模块（已迁移至热更层）
- **FuFramework.Entity**：实体管理模块
- **FuFramework.UI**：UI 管理模块
- **Hotfix.Scene**：场景管理模块（已迁移至热更层）
- **Hotfix.Sound**：声音管理模块（已迁移至热更层）
- **Hotfix.Network**：网络管理模块（已迁移至热更层）
- **Hotfix.Web**：Web 管理模块（已迁移至热更层）
- **Hotfix.Download**：下载管理模块（已迁移至热更层）
- **Hotfix.Storage**：数据存储模块（已迁移至热更层）
- **Hotfix.Localization**：本地化管理模块（已迁移至热更层）
- **Hotfix.Model**：数据模型模块（已迁移至热更层）
- **Hotfix.RedDot**：红点管理模块（已迁移至热更层）
- **Hotfix.Guide**：引导管理模块（已迁移至热更层）
- **FuFramework.Mono**：Mono 管理模块
- **FuFramework.GlobalConfig**：全局配置模块
- **SRDebugger**：运行时日志查看器
- **UnityEngine**：Unity 引擎

***

## 7. 最佳实践

### 7.1 流程设计原则

```csharp
// 1. 每个流程职责单一
public class ProcedureLoading : ProcedureBase
{
    protected override void OnEnter(IFsm<ProcedureModule> procedureOwner)
    {
        // 只做加载相关的事情
        LoadResources();
        LoadData();
    }
}

// 2. 流程切换要清晰
public class ProcedureMenu : ProcedureBase
{
    public void OnStartGameClicked()
    {
        // 明确切换到下一个流程
        ChangeState<ProcedureBattle>(procedureOwner);
    }
}

// 3. 正确处理流程退出
protected override void OnLeave(IFsm<ProcedureModule> procedureOwner, bool isShutdown)
{
    // 清理资源
    Cleanup();
    
    // 取消事件订阅
    UnsubscribeEvents();
    
    base.OnLeave(procedureOwner, isShutdown);
}
```

### 7.2 GlobalModule 使用建议

```csharp
// 推荐：直接访问
GlobalModule.AssetModule.LoadAssetAsync<T>(path);

// 不推荐：缓存模块引用（GlobalModule 已经做了缓存）
private AssetModule m_AssetModule;  // 多余

// 推荐：在热更代码中使用 GlobalModule
public class HotfixGameLogic
{
    public void Start()
    {
        // 通过 GlobalModule 访问框架功能
        GlobalModule.EventModule.Subscribe<GameStartEventArgs>(OnGameStart);
    }
}
```

### 7.3 模块注册注意事项

```csharp
// 如果需要添加自定义模块，在 RegisterModules 中添加
private void RegisterModules()
{
    // ... 原有模块 ...
    
    // 添加自定义模块
    ModuleManager.RegisterModule<MyCustomModule>();
}

// 在 GlobalModule 中添加访问属性
public static class GlobalModule
{
    private static MyCustomModule m_MyCustomModule;
    
    public static MyCustomModule MyCustomModule => 
        m_MyCustomModule ??= ModuleManager.GetModule<MyCustomModule>();
}
```

***

## 8. 注意事项

1. **单例模式**：Launcher 继承自 `MonoSingleton`，确保场景中只有一个实例
2. **启动场景**：必须在初始场景中挂载 Launcher 组件
3. **流程配置**：必须至少配置一个流程，且入口流程不能为空
4. **模块顺序**：注册顺序不可随意修改，注意模块依赖关系
5. **重启游戏**：`RestartGame` 会完全重置所有模块状态
6. **退出游戏**：`QuitGame` 会正确释放所有模块资源
7. **运行时修改**：流程配置在运行时不允许修改
8. **优先级排序**：流程在 Inspector 中按 Priority 属性自动排序
9. **热更兼容**：GlobalModule 设计支持热更代码访问框架模块
