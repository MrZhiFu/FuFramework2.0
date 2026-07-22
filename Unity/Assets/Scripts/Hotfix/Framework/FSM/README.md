# FuFramework FSM Module

## 1. 简介

**FuFramework FSM** 模块是一个高性能的有限状态机系统，专为 Unity 游戏开发设计。它提供了灵活的状态管理机制，支持状态切换、数据存储和生命周期管理，适用于游戏角色 AI、游戏流程控制等场景。

---

## 2. 特性

- **类型安全**：使用泛型确保状态机持有者类型安全
- **数据管理**：支持在状态机中存储和共享数据变量
- **生命周期管理**：完整的状态生命周期（Init、Enter、Update、Leave、Destroy）
- **状态切换**：便捷的状态切换接口
- **多状态机管理**：支持同时管理多个状态机
- **对象池集成**：状态机实例通过引用池管理，减少 GC 压力

---

## 3. 核心概念

### 3.1 状态机持有者 (Owner)

状态机持有者是状态机所关联的对象，通常是游戏实体（如角色、敌人等）。一个持有者可以拥有多个状态机（通过名称区分）。

### 3.2 状态 (State)

状态是状态机的基本组成单元，每个状态代表对象的一种行为模式。状态通过继承 `FsmStateBase` 实现自定义逻辑。

### 3.3 状态数据 (Data)

状态机可以存储数据变量，用于在状态之间共享信息。数据通过 `Variable` 系统管理，支持类型安全的数据访问。

---

## 4. 核心类说明

### 4.1 FsmModule

有限状态机管理模块，继承自 `ModuleBase`，是 FSM 系统的核心管理类。

**核心属性：**

```csharp
int Count { get; }      // 管理的有限状态机数量
```

**主要方法：**

```csharp
// 检查状态机是否存在
bool HasFsm<T>() where T : class
bool HasFsm(Type owner)
bool HasFsm<T>(string fsmName) where T : class
bool HasFsm(Type owner, string fsmName)

// 获取状态机
Fsm GetFsm<T>() where T : class
Fsm GetFsm(Type ownerType)
Fsm GetFsm<T>(string fsmName) where T : class
Fsm GetFsm(Type ownerType, string fsmName)
Fsm[] GetAllFsms()
void GetAllFsms(List<Fsm> results)

// 创建状态机
Fsm CreateFsm<T>(T owner, params FsmStateBase[] states) where T : class
Fsm CreateFsm<T>(string fsmName, T owner, params FsmStateBase[] states) where T : class
Fsm CreateFsm<T>(T owner, List<FsmStateBase> states) where T : class
Fsm CreateFsm<T>(string fsmName, T owner, List<FsmStateBase> states) where T : class

// 销毁状态机
bool DestroyFsm<T>() where T : class
bool DestroyFsm(Type ownerType)
bool DestroyFsm<T>(string fsmName) where T : class
bool DestroyFsm(Type ownerType, string fsmName)
bool DestroyFsm<T>(Fsm fsm) where T : class
bool DestroyFsm(Fsm fsm)
```

---

### 4.2 Fsm

有限状态机核心类，实现 `IReference` 接口支持引用池。

**核心属性：**

```csharp
string Name { get; }                    // 状态机名称
Type Owner { get; }                     // 状态机持有者类型
string FullName { get; }                // 状态机完整名称（持有者类型+名称）
bool IsDestroyed { get; }               // 是否已销毁
bool IsRunning { get; }                 // 是否正在运行
FsmStateBase CurrentStateBase { get; }  // 当前状态
string CurrentStateName { get; }        // 当前状态名称
float CurrentStateTime { get; }         // 当前状态持续时间
int FsmStateCount { get; }              // 状态数量
```

**主要方法：**

```csharp
// 创建状态机
static Fsm Create<T>(string name, T owner, params FsmStateBase[] states) where T : class
static Fsm Create<T>(string name, T owner, List<FsmStateBase> states) where T : class

// 启动状态机
void Start<TState>() where TState : FsmStateBase
void Start(Type stateType)

// 状态查询
bool HasState<TState>() where TState : FsmStateBase
bool HasState(Type stateType)
TState GetState<TState>() where TState : FsmStateBase
FsmStateBase GetState(Type stateType)
FsmStateBase[] GetAllStates()
void GetAllStates(List<FsmStateBase> results)

// 数据管理
bool HasData(string name)
TData GetData<TData>(string name) where TData : VariableBase
VariableBase GetData(string name)
void SetData<TData>(string name, TData data) where TData : VariableBase
void SetData(string name, VariableBase data)
bool RemoveData(string name)

// 状态切换（内部方法，通常在状态中调用）
internal void ChangeState<TState>() where TState : FsmStateBase
internal void ChangeState(Type stateType)

// 清理和释放
void Clear()
internal void Shutdown()
```

---

### 4.3 FsmStateBase

状态基类，定义状态的基本接口和生命周期。

**核心属性：**

```csharp
protected Fsm Fsm { get; private set; }      // 所属状态机
```

**生命周期方法（可重写）：**

```csharp
// 状态初始化（创建时调用一次）
protected internal virtual void OnInit(Fsm fsm)

// 状态进入
protected internal virtual void OnEnter()

// 状态轮询（每帧调用）
protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)

// 状态离开
protected internal virtual void OnLeave(bool isShutdown)

// 状态销毁（状态机销毁时调用）
protected internal virtual void OnDestroy()
```

**状态切换方法：**

```csharp
// 切换到指定状态
protected void ChangeState<TState>() where TState : FsmStateBase
protected void ChangeState(Type state)
```

---

## 5. 使用示例

### 5.1 定义状态类

```csharp
using Hotfix.Framework.FSM;
using Hotfix.Framework.Core;
using UnityEngine;

// 空闲状态
public class PlayerIdleState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入空闲状态");
        // 播放待机动画
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 检查输入，决定是否切换状态
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
        {
            ChangeState<PlayerMoveState>();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeState<PlayerAttackState>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开空闲状态");
    }
}

// 移动状态
public class PlayerMoveState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入移动状态");
        // 播放移动动画
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 移动逻辑
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical);

        // 如果没有输入，返回空闲状态
        if (moveDirection.magnitude < 0.01f)
        {
            ChangeState<PlayerIdleState>();
            return;
        }

        // 执行移动...

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeState<PlayerAttackState>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开移动状态");
    }
}

// 攻击状态
public class PlayerAttackState : FsmStateBase
{
    private float m_AttackDuration = 0.5f;
    private float m_Timer = 0f;

    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入攻击状态");
        m_Timer = 0f;
        // 播放攻击动画
        // 执行攻击逻辑...
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        m_Timer += deltaTime;

        // 攻击结束，返回空闲状态
        if (m_Timer >= m_AttackDuration)
        {
            ChangeState<PlayerIdleState>();
        }
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开攻击状态");
    }
}

// 死亡状态
public class PlayerDeathState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入死亡状态");
        // 播放死亡动画
        // 禁用输入...
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 死亡状态通常不切换
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开死亡状态");
    }
}
```

### 5.2 创建和使用状态机

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.FSM;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private FsmModule m_FsmModule;
    private Fsm m_PlayerFsm;

    private void Start()
    {
        // 获取 FSM 模块
        m_FsmModule = ModuleManager.GetModule<FsmModule>();

        // 创建状态机
        m_PlayerFsm = m_FsmModule.CreateFsm<PlayerController>(
            "PlayerFSM",                                    // 状态机名称
            this,                                           // 持有者
            new PlayerIdleState(),                          // 状态集合
            new PlayerMoveState(),
            new PlayerAttackState(),
            new PlayerDeathState()
        );

        // 启动状态机，进入空闲状态
        m_PlayerFsm.Start<PlayerIdleState>();
    }

    private void Update()
    {
        // 其他游戏逻辑...
    }

    public void TakeDamage(float damage)
    {
        // 获取当前血量
        float health = m_PlayerFsm.GetData<VarFloat>("Health")?.Value ?? 100f;
        health -= damage;

        // 更新血量
        VarFloat healthVar = health;
        m_PlayerFsm.SetData("Health", healthVar);

        // 血量归零，切换到死亡状态
        if (health <= 0)
        {
            m_PlayerFsm.ChangeState<PlayerDeathState>();
        }
    }

    private void OnDestroy()
    {
        // 销毁状态机
        if (m_FsmModule != null && m_PlayerFsm != null)
        {
            m_FsmModule.DestroyFsm(m_PlayerFsm);
        }
    }
}
```

### 5.3 使用状态机数据

```csharp
// 定义状态数据
public class EnemyAIState : FsmStateBase
{
    protected internal override void OnInit(Fsm fsm)
    {
        base.OnInit(fsm);

        // 初始化数据
        VarFloat detectionRange = 10f;
        VarFloat attackRange = 2f;
        VarObject patrolPoints = ReferencePool.Acquire<VarObject>();
        patrolPoints.Value = new GameObject[4];

        Fsm.SetData("DetectionRange", detectionRange);
        Fsm.SetData("AttackRange", attackRange);
        Fsm.SetData("PatrolPoints", patrolPoints);
    }

    protected internal override void OnEnter()
    {
        // 获取数据
        float detectionRange = Fsm.GetData<VarFloat>("DetectionRange")?.Value ?? 10f;
        FuLogger.LogInfo($"检测范围: {detectionRange}");
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 获取和更新数据
        var detectionRange = Fsm.GetData<VarFloat>("DetectionRange");
        var target = Fsm.GetData<VarGameObject>("Target")?.Value;

        // AI 逻辑...
        if (target != null)
        {
            float distance = Vector3.Distance(// 通过其他方式获取 Transform: position, target.transform.position);

            if (distance <= detectionRange?.Value)
            {
                ChangeState<EnemyChaseState>();
            }
        }
    }
}

// 使用示例
public class EnemyController : MonoBehaviour
{
    private Fsm m_EnemyFsm;

    private void Start()
    {
        var fsmModule = ModuleManager.GetModule<FsmModule>();

        m_EnemyFsm = fsmModule.CreateFsm<EnemyController>(
            "EnemyFSM",
            this,
            new EnemyIdleState(),
            new EnemyPatrolState(),
            new EnemyChaseState(),
            new EnemyAttackState()
        );

        // 设置初始数据
        VarFloat health = 100f;
        VarFloat speed = 5f;

        m_EnemyFsm.SetData("Health", health);
        m_EnemyFsm.SetData("Speed", speed);

        m_EnemyFsm.Start<EnemyIdleState>();
    }
}
```

### 5.4 游戏流程状态机

```csharp
// 游戏流程状态
public class GameStartState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("游戏开始");
        // 加载初始资源
        // 显示开始界面
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 等待玩家点击开始
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ChangeState<GamePlayState>();
        }
    }
}

public class GamePlayState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("游戏进行中");
        // 初始化游戏场景
        // 生成玩家和敌人
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 检查游戏结束条件
        bool isPlayerDead = Fsm.GetData<VarBoolean>("IsPlayerDead")?.Value ?? false;
        bool isLevelComplete = Fsm.GetData<VarBoolean>("IsLevelComplete")?.Value ?? false;

        if (isPlayerDead)
        {
            ChangeState<GameOverState>();
        }
        else if (isLevelComplete)
        {
            ChangeState<GameVictoryState>();
        }
    }
}

public class GamePauseState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("游戏暂停");
        Time.timeScale = 0f;
        // 显示暂停菜单
    }

    protected internal override void OnLeave(bool isShutdown)
    {
        Time.timeScale = 1f;
    }
}

public class GameOverState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("游戏结束");
        // 显示游戏结束界面
        // 保存分数
    }
}

public class GameVictoryState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("游戏胜利");
        // 显示胜利界面
        // 解锁下一关
    }
}

// 游戏管理器
public class GameManager : MonoBehaviour
{
    private FsmModule m_FsmModule;
    private Fsm m_GameFsm;

    private void Start()
    {
        m_FsmModule = ModuleManager.GetModule<FsmModule>();

        // 创建游戏流程状态机
        m_GameFsm = m_FsmModule.CreateFsm<GameManager>(
            "GameFSM",
            this,
            new GameStartState(),
            new GamePlayState(),
            new GamePauseState(),
            new GameOverState(),
            new GameVictoryState()
        );

        m_GameFsm.Start<GameStartState>();
    }

    private void Update()
    {
        // 处理暂停输入
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_GameFsm.CurrentStateBase is GamePlayState)
            {
                m_GameFsm.ChangeState<GamePauseState>();
            }
            else if (m_GameFsm.CurrentStateBase is GamePauseState)
            {
                m_GameFsm.ChangeState<GamePlayState>();
            }
        }
    }

    public void SetPlayerDead()
    {
        VarBoolean isDead = true;
        m_GameFsm.SetData("IsPlayerDead", isDead);
    }

    public void SetLevelComplete()
    {
        VarBoolean isComplete = true;
        m_GameFsm.SetData("IsLevelComplete", isComplete);
    }
}
```

### 5.5 层级状态机（多个状态机）

```csharp
public class ComplexEnemyController : MonoBehaviour
{
    private FsmModule m_FsmModule;
    private Fsm m_MovementFsm;      // 移动状态机
    private Fsm m_CombatFsm;        // 战斗状态机

    private void Start()
    {
        m_FsmModule = ModuleManager.GetModule<FsmModule>();

        // 创建移动状态机
        m_MovementFsm = m_FsmModule.CreateFsm<ComplexEnemyController>(
            "MovementFSM",          // 名称区分
            this,
            new IdleState(),
            new PatrolState(),
            new ChaseState()
        );

        // 创建战斗状态机
        m_CombatFsm = m_FsmModule.CreateFsm<ComplexEnemyController>(
            "CombatFSM",            // 名称区分
            this,
            new ReadyState(),
            new AttackState(),
            new DefendState(),
            new FleeState()
        );

        // 启动两个状态机
        m_MovementFsm.Start<IdleState>();
        m_CombatFsm.Start<ReadyState>();
    }

    private void Update()
    {
        // 两个状态机独立更新
        // 移动状态机控制位置
        // 战斗状态机控制攻击行为
    }

    private void OnDestroy()
    {
        // 销毁所有状态机
        m_FsmModule.DestroyFsm<ComplexEnemyController>("MovementFSM");
        m_FsmModule.DestroyFsm<ComplexEnemyController>("CombatFSM");
    }
}
```

---

## 6. 编辑器功能

### 6.1 FsmModuleInspector

`FsmModule` 的 Inspector 扩展，提供运行时状态机监控功能。

**功能：**
- **统计信息**：显示当前管理的状态机数量
- **状态机列表**：列出所有状态机的完整名称
- **运行状态**：显示每个状态机的当前状态和运行时间
- **状态标识**：清晰标识状态机的运行、未运行或被销毁状态

**使用方法：**
1. 在编辑器中运行游戏
2. 在 Hierarchy 中找到 `[FrameworkModule]` 下的 `FsmModule`
3. 选中后在 Inspector 面板查看状态机统计信息

---

## 7. 目录结构说明

```text
FSM/
├── FsmModule.cs                 # 有限状态机管理模块
├── Fsm.cs                       # 有限状态机核心类
├── FsmStateBase.cs              # 状态基类
└── README.md                    # 本文档
```

---

## 8. 依赖

- **Unity**: 2021.3 LTS 或更高版本
- **Hotfix.Framework.Core**: 框架核心模块
- **Hotfix.Framework.ReferencePools**: 引用池模块
- **Hotfix.Framework.Variable**: 变量管理模块（用于状态机数据存储）

---

## 9. 最佳实践

### 9.1 状态设计原则

```csharp
// 单一职责：每个状态只负责一种行为
public class AttackState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        // 只处理攻击开始逻辑
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 只处理攻击中的逻辑
        // 不要在这里处理移动、防御等其他逻辑
    }
}
```

### 9.2 状态切换条件

```csharp
public class ChaseState : FsmStateBase
{
    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 获取必要数据
        var target = Fsm.GetData<VarGameObject>("Target")?.Value;
        var attackRange = Fsm.GetData<VarFloat>("AttackRange")?.Value ?? 2f;

        // 目标丢失，返回空闲
        if (target == null)
        {
            ChangeState<IdleState>();
            return;
        }

        float distance = Vector3.Distance(// 通过其他方式获取 Transform: position, target.transform.position);

        // 进入攻击范围
        if (distance <= attackRange)
        {
            ChangeState<AttackState>();
            return;
        }

        // 目标太远，放弃追击
        if (distance > 20f)
        {
            ChangeState<IdleState>();
            return;
        }

        // 执行追击...
    }
}
```

### 9.3 数据管理

```csharp
public class AIStateBase : FsmStateBase
{
    // 定义数据键常量，避免硬编码
    protected const string KEY_TARGET = "Target";
    protected const string KEY_HEALTH = "Health";
    protected const string KEY_SPEED = "Speed";

    protected T GetData<T>(string key) where T : VariableBase
    {
        return Fsm.GetData<T>(key);
    }

    protected void SetData<T>(string key, T value) where T : VariableBase
    {
        Fsm.SetData(key, value);
    }
}
```

### 9.4 状态机生命周期管理

```csharp
public class CharacterController : MonoBehaviour
{
    private Fsm m_Fsm;

    private void OnEnable()
    {
        // 启用时恢复状态机
        if (m_Fsm != null && !m_Fsm.IsRunning)
        {
            m_Fsm.Start<IdleState>();
        }
    }

    private void OnDisable()
    {
        // 禁用时清理状态
        if (m_Fsm != null && m_Fsm.IsRunning)
        {
            // 可以选择暂停或保持当前状态
        }
    }

    private void OnDestroy()
    {
        // 销毁时清理状态机
        var fsmModule = ModuleManager.GetModule<FsmModule>();
        fsmModule?.DestroyFsm(m_Fsm);
    }
}
```

---

## 10. 注意事项

1. **状态机名称唯一性**：同一持有者的状态机名称必须唯一
2. **状态切换**：只能在状态的 `OnUpdate` 中调用 `ChangeState`，不要在 `OnEnter` 或 `OnLeave` 中切换
3. **数据清理**：状态机销毁时会自动清理数据，但建议在 `OnDestroy` 中手动清理复杂数据
4. **空状态检查**：在切换状态前确保目标状态已添加到状态机
5. **性能考虑**：避免在 `OnUpdate` 中进行昂贵的计算，可以将结果缓存到状态机数据中
6. **异常处理**：状态方法中的异常会被捕获，但建议自行处理以避免状态异常
