# FuFramework Procedure Module

## 1. 简介

FuFramework Procedure 模块是游戏框架的流程管理系统，基于有限状态机（FSM）实现游戏流程的状态管理和转换。该模块提供了一种结构化的方式来管理游戏的不同阶段和流程，如大厅流程、匹配流程、战斗流程等。

## 2. 核心特性

- **基于 FSM**：继承自有限状态机模块，提供成熟的状态管理能力
- **流程管理**：统一管理游戏各个阶段的流程转换
- **生命周期**：完整的流程生命周期管理（初始化、进入、更新、离开、销毁）
- **编辑器优先级**：支持流程优先级设置（仅在 Editor 下生效，用于 Inspector 展示排序）
- **状态监控**：实时监控当前流程状态和持续时间
- **类型安全**：泛型接口确保流程类型安全

## 3. 核心概念

### 3.1 类继承与实现体系

```
【类继承体系】

ModuleBase (框架模块基类)
    └── ProcedureModule (流程管理模块)
        └── 内部持有 FsmModule 引用，通过 Fsm 驱动流程

FsmStateBase (状态机状态基类)
    └── ProcedureBase (流程基类)
        └── 用户自定义流程类 (如 LobbyProcedure, BattleProcedure)

【依赖关系】

ProcedureModule 依赖:
    └── FsmModule (有限状态机模块，在 OnInit 中通过 ModuleManager 获取)
        └── Fsm (状态机实例，ProcedureModule 内部维护)
            └── FsmStateBase[] (状态集合)
                └── ProcedureBase[] (流程集合)
```

### 3.2 流程架构

```
┌─────────────────────────────────────────────────────────────┐
│                   ProcedureModule                           │
│                      (ModuleBase)                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                   m_ProcedureFsm                     │   │
│  │                      (Fsm)                           │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ │   │
│  │  │LobbyProcedure│ │BattleProcedure│ │SettlementProc│ │   │
│  │  │(ProcedureBase)│ │(ProcedureBase)│ │(ProcedureBase)│  │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘ │   │
│  │                                                      │   │
│  │  CurrentStateBase ──────▶ CurrentProcedure           │   │
│  │  CurrentStateTime  ──────▶ CurrentProcedureTime      │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │    FsmModule     │
                    │   (依赖模块)      │
                    └──────────────────┘
```

### 3.3 流程生命周期

```
【流程状态流转】

初始化 (InitProcedures)
    │
    ▼
┌─────────┐     进入 (OnEnter)     ┌─────────┐     更新 (OnUpdate)    ┌─────────┐
│  待机   │ ─────────────────────▶ │  运行中 │ ─────────────────────▶ │  持续   │
│ (Idle)  │                        │ (Active)│                        │ (Update)│
└─────────┘                        └─────────┘                        └────┬────┘
    ▲                                                                     │
    │                                                                     │
    │ 离开 (OnLeave)                                                      │
    └─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                        ┌─────────┐
                        │  结束   │
                        │ (Leave) │
                        └─────────┘

【生命周期回调顺序】

流程切换: ProcedureA ──▶ ProcedureB

ProcedureA.OnLeave(false)   // 先离开当前流程
        │
        ▼
ProcedureB.OnEnter()         // 再进入新流程
        │
        ▼
ProcedureB.OnUpdate(...)     // 持续更新
```

### 3.4 与 FSM 模块的关系

```
【Procedure 与 Fsm 的映射关系】

ProcedureModule                          FsmModule
    │                                        │
    ├── OnInit() 获取 FsmModule ──────────▶  │
    │                                        │
    ├── InitProcedures(procedures)  ──────▶  ├── CreateFsm(owner, states)
    │                                        │
    ├── StartProcedure<T>()         ──────▶  ├── Start<T>()
    │                                        │
    ├── HasProcedure<T>()           ──────▶  ├── HasState<T>()
    │                                        │
    ├── GetProcedure<T>()           ──────▶  ├── GetState<T>()
    │                                        │
    └── CurrentProcedure            ──────▶  └── CurrentStateBase

【ProcedureBase 与 FsmStateBase 的映射关系】

ProcedureBase                            FsmStateBase
    │                                        │
    ├── OnInit(procedureOwner)      ──────▶  ├── OnInit(fsm)
    │                                        │
    ├── OnEnter()                   ──────▶  ├── OnEnter()
    │                                        │
    ├── OnUpdate(...)               ──────▶  ├── OnUpdate(...)
    │                                        │
    ├── OnLeave(isShutdown)         ──────▶  ├── OnLeave(isShutdown)
    │                                        │
    ├── OnDestroy()                 ──────▶  ├── OnDestroy()
    │                                        │
    └── ChangeState<T>()            ──────▶  └── ChangeState<T>()
```

## 4. 核心类详细说明

### 4.1 ProcedureModule

流程管理模块，继承自 `ModuleBase`，在 `OnInit` 中获取 `FsmModule`，负责管理所有流程。

**核心功能：**

```csharp
public sealed class ProcedureModule : ModuleBase
{
    // 流程状态监控
    public ProcedureBase CurrentProcedure { get; }      // 获取当前流程
    public float CurrentProcedureTime { get; }          // 获取当前流程持续时间

    // 流程状态机管理
    public void InitProcedures(ProcedureBase[] procedure)  // 初始化流程状态机

    // 流程控制
    public void StartProcedure<T>() where T : ProcedureBase   // 开始指定流程
    public void StartProcedure(Type procedureType)            // 通过类型开始流程

    // 流程查询
    public bool HasProcedure<T>() where T : ProcedureBase     // 检查是否存在流程
    public bool HasProcedure(Type procedureType)              // 通过类型检查
    public ProcedureBase GetProcedure<T>() where T : ProcedureBase  // 获取流程
    public ProcedureBase GetProcedure(Type procedureType)     // 通过类型获取
}
```

**模块依赖：**
- 在 `OnInit` 中通过 `ModuleManager.GetModule<FsmModule>()` 获取 FSM 模块
- 初始化时必须确保 `FsmModule` 已注册（框架初始化顺序保证了这一点）
- 内部维护一个 `Fsm` 实例来管理流程状态

### 4.2 ProcedureBase

流程基类，继承自 `FsmStateBase`，所有自定义流程都需要继承此类。

**核心功能：**

```csharp
public abstract class ProcedureBase : FsmStateBase
{
#if UNITY_EDITOR
    // 优先级（仅在 Editor 下生效，用于 Inspector 展示排序）
    public virtual int Priority => 0;
#endif

    // 流程初始化 — protected internal override（非 protected override）
    protected internal override void OnInit(Fsm procedureOwner) => base.OnInit(procedureOwner);

    // 以下生命周期方法继承自 FsmStateBase：
    // protected internal virtual void OnEnter()                        — 进入流程
    // protected internal virtual void OnUpdate(float deltaTime,        — 流程更新
    //                                          float unscaledDeltaTime)
    // protected internal virtual void OnLeave(bool isShutdown)         — 离开流程
    // protected internal virtual void OnDestroy()                      — 流程销毁

    // 状态切换（继承自 FsmStateBase）
    // protected void ChangeState<TState>() where TState : FsmStateBase — 切换到指定流程
}
```

**继承的属性：**
- `Fsm` — 获取所属的状态机实例
- `Fsm.Owner` — 获取流程持有者（ProcedureModule 的 Type）
- `Fsm.CurrentStateTime` — 当前流程持续时间
- `Fsm.SetData<T>(name, data)` / `Fsm.GetData<T>(name)` — 通过 Fsm 存取流程间共享数据

## 5. 使用示例

### 5.1 定义自定义流程类

```csharp
using Hotfix.Framework.Procedure;
using Hotfix.Framework.FSM;
using Hotfix.Framework.Core;

/// <summary>
/// 大厅流程 — 玩家在大厅中进行匹配、查看背包等操作
/// </summary>
public class LobbyProcedure : ProcedureBase
{
#if UNITY_EDITOR
    public override int Priority => 100;
#endif

    protected internal override void OnInit(Fsm procedureOwner)
    {
        base.OnInit(procedureOwner);
        FuLogger.LogInfo("[LobbyProcedure] 初始化");
    }

    protected internal override void OnEnter()
    {
        base.OnEnter();
        FuLogger.LogInfo("[LobbyProcedure] 进入大厅");

        // 打开大厅 UI
        // UIModule.OpenUI<LobbyView>();
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(deltaTime, unscaledDeltaTime);

        // 玩家点击匹配按钮 → 切换到匹配流程
        if (IsMatchRequested)
        {
            ChangeState<MatchmakingProcedure>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        FuLogger.LogInfo(isShutdown
            ? "[LobbyProcedure] 流程销毁"
            : "[LobbyProcedure] 离开大厅");
    }

    private bool IsMatchRequested => false; // 实际匹配逻辑
}

/// <summary>
/// 匹配流程 — 等待匹配对手，匹配成功后进入战斗
/// </summary>
public class MatchmakingProcedure : ProcedureBase
{
#if UNITY_EDITOR
    public override int Priority => 90;
#endif

    private float m_Elapsed;

    protected internal override void OnEnter()
    {
        base.OnEnter();
        m_Elapsed = 0f;
        FuLogger.LogInfo("[MatchmakingProcedure] 开始匹配");

        // 显示匹配界面
        // UIModule.OpenUI<MatchmakingView>();
        // NetworkModule.SendMatchRequest();
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(deltaTime, unscaledDeltaTime);

        m_Elapsed += deltaTime;

        // 匹配成功 → 进入战斗（模拟：3 秒后匹配成功）
        if (m_Elapsed >= 3f)
        {
            FuLogger.LogInfo("[MatchmakingProcedure] 匹配成功，进入战斗");
            ChangeState<BattleProcedure>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        FuLogger.LogInfo("[MatchmakingProcedure] 离开匹配");

        // 如果流程被销毁（非正常切换），取消匹配请求
        if (isShutdown)
        {
            // NetworkModule.CancelMatchRequest();
        }
    }
}

/// <summary>
/// 战斗流程 — 管理一场战斗的生命周期
/// </summary>
public class BattleProcedure : ProcedureBase
{
#if UNITY_EDITOR
    public override int Priority => 80;
#endif

    protected internal override void OnEnter()
    {
        base.OnEnter();
        FuLogger.LogInfo("[BattleProcedure] 进入战斗");

        // 加载战斗场景 + 初始化战斗系统
        // SceneModule.LoadSceneAsync("BattleScene");
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(deltaTime, unscaledDeltaTime);

        // 战斗结束 → 进入结算
        if (BattleFinished)
        {
            // 将战斗结果存入 Fsm 数据，供后续流程读取
            Fsm.SetData("BattleResult", new BattleResult { Win = true, Score = 1500 });
            ChangeState<SettlementProcedure>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        FuLogger.LogInfo("[BattleProcedure] 离开战斗");
    }

    private bool BattleFinished => false; // 实际战斗逻辑
}

/// <summary>
/// 结算流程 — 展示战斗结果，然后返回大厅
/// </summary>
public class SettlementProcedure : ProcedureBase
{
#if UNITY_EDITOR
    public override int Priority => 70;
#endif

    private float m_DisplayTime;

    protected internal override void OnEnter()
    {
        base.OnEnter();
        m_DisplayTime = 0f;

        // 读取战斗结果
        var result = Fsm.GetData<BattleResult>("BattleResult");
        FuLogger.LogInfo($"[SettlementProcedure] 战斗结果 — 胜利:{result.Win}, 得分:{result.Score}");

        // 显示结算界面
        // UIModule.OpenUI<SettlementView>(result);
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(deltaTime, unscaledDeltaTime);

        m_DisplayTime += deltaTime;

        // 展示 5 秒后返回大厅
        if (m_DisplayTime >= 5f || PlayerTappedContinue())
        {
            ChangeState<LobbyProcedure>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        base.OnLeave(isShutdown);
        // 清理共享数据
        Fsm.RemoveData("BattleResult");
    }

    private bool PlayerTappedContinue() => false; // 点击继续
}

// 战斗结果数据结构
public class BattleResult
{
    public bool Win;
    public int Score;
}
```

### 5.2 初始化并启动流程

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Procedure;

public class GameLauncher
{
    private ProcedureModule m_ProcedureModule;

    public void Init()
    {
        // 获取流程管理模块
        m_ProcedureModule = ModuleManager.GetModule<ProcedureModule>();

        // 初始化流程状态机（一次性注册所有流程）
        m_ProcedureModule.InitProcedures(new ProcedureBase[]
        {
            new LobbyProcedure(),
            new MatchmakingProcedure(),
            new BattleProcedure(),
            new SettlementProcedure(),
        });

        // 启动第一个流程（大厅）
        m_ProcedureModule.StartProcedure<LobbyProcedure>();
    }
}
```

### 5.3 查询流程状态

```csharp
// 获取当前流程
var currentProc = m_ProcedureModule.CurrentProcedure;
FuLogger.LogInfo($"当前流程: {currentProc.GetType().Name}");
FuLogger.LogInfo($"已运行: {m_ProcedureModule.CurrentProcedureTime:F2}s");

// 检查某个流程是否已注册
if (m_ProcedureModule.HasProcedure<BattleProcedure>())
{
    var battleProc = m_ProcedureModule.GetProcedure<BattleProcedure>();
    FuLogger.LogInfo("战斗流程已就绪");
}
```

---

## 6. 编辑器功能

### 6.1 ProcedureModuleInspector

`ProcedureModule` 的 Inspector 扩展，提供运行时流程监控功能。

**功能：**
- **统计信息**：显示当前流程名称和持续时间
- **流程列表**：列出所有已注册的流程及其优先级（`Priority` 仅 Editor 下可用）

**使用方法：**
1. 在编辑器中运行游戏
2. 在 Hierarchy 中找到 `[FrameworkModule]` 下的 `ProcedureModule`
3. 选中后在 Inspector 面板查看流程状态

---

## 7. 目录结构

```text
Procedure/
├── Runtime/
│   ├── ProcedureModule.cs                           # 流程管理模块
│   ├── ProcedureBase.cs                             # 流程基类
├── Editor/
│   ├── Inspector/
│   │   └── ProcedureModuleInspector.cs              # ProcedureModule Inspector 扩展
└── README.md                                        # 本文档
```

---

## 8. 依赖

- **Unity**: 2021.3 LTS 或更高版本
- **Hotfix.Framework.Core**: 框架核心模块（ModuleBase、ModuleManager、FuLogger）
- **Hotfix.Framework.FSM**: 有限状态机模块（FsmModule、Fsm、FsmStateBase）

---

## 9. 最佳实践

### 9.1 流程设计原则

- **单一职责**：每个流程只负责一个游戏阶段（大厅、匹配、战斗、结算各司其职）
- **生命周期正确使用**：`OnInit` 中做一次性初始化，`OnEnter` 中做每次进入时的准备，`OnLeave` 中做清理
- **明确切换条件**：在 `OnUpdate` 中检查条件，条件满足时调用 `ChangeState<T>()`
- **共享数据**：流程间数据通过 `Fsm.SetData` / `Fsm.GetData` 存取，在 `OnLeave` 中清理

### 9.2 流程初始化

```csharp
// 推荐：在游戏启动时一次性注册所有流程
m_ProcedureModule.InitProcedures(new ProcedureBase[]
{
    new LobbyProcedure(),
    new MatchmakingProcedure(),
    new BattleProcedure(),
    new SettlementProcedure(),
});
```

### 9.3 流程切换

```csharp
protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
{
    base.OnUpdate(deltaTime, unscaledDeltaTime);

    if (ShouldSwitch)
    {
        ChangeState<NextProcedure>();
        return; // 切换后立即返回，避免后续代码在错误状态下执行
    }
}
```

### 9.4 数据传递

```csharp
// 在流程 A 中设置数据
Fsm.SetData("MatchInfo", matchInfo);

// 在流程 B 中读取数据
var matchInfo = Fsm.GetData<MatchInfo>("MatchInfo");

// 使用完毕后清理
Fsm.RemoveData("MatchInfo");
```

---

## 10. 注意事项

1. **切换时机**：只能在 `OnUpdate` 中调用 `ChangeState`，不要在 `OnEnter` 或 `OnLeave` 中切换
2. **模块初始化顺序**：`ProcedureModule.OnInit` 中通过 `ModuleManager.GetModule<FsmModule>()` 获取 FsmModule，确保 FsmModule 已先注册
3. **流程唯一性**：同一流程类型只能注册一次，重复添加会抛出异常
4. **OnInit 签名**：`ProcedureBase.OnInit` 是 `protected internal override`，不可改为 `protected override`（同程序集限制）
5. **OnEnter 无参**：`OnEnter()` 没有 `userData` 参数，流程间数据通过 `Fsm.SetData` / `Fsm.GetData` 传递
6. **异常处理**：流程方法中的异常会被模块层捕获记录，不会影响其他流程，但建议自行处理
