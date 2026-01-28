# FuFramework FSM Module

## 简介
FuFramework FSM 模块是一个高性能的有限状态机系统，专为Unity游戏开发设计。它提供了灵活的状态管理机制，支持状态切换、数据存储和生命周期管理，适用于游戏角色AI、游戏流程等场景。

## 核心类说明

### FsmManager
有限状态机管理器，继承自 `FuModule`。
- **职责**：
  1. 管理多个有限状态机的创建、销毁和轮询
  2. 提供状态机的全局访问接口
  3. 自动处理状态机的生命周期管理

### Fsm
有限状态机核心类，实现 `IReference` 接口。
- **职责**：
  1. 管理状态机的状态集合和数据变量
  2. 处理状态切换和生命周期回调
  3. 提供状态查询和数据管理功能

### FsmStateBase
状态基类，定义状态的基本接口和生命周期。
- **职责**：
  1. 定义状态的初始化、进入、轮询、离开、销毁生命周期
  2. 提供状态切换的便捷方法
  3. 管理状态与所属状态机的关系

## 使用指南

### 1. 定义状态类
```csharp
// 定义角色状态
public class PlayerIdleState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入空闲状态");
    }
    
    protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 检查是否需要切换到移动状态
        if (Input.GetKey(KeyCode.W))
        {
            ChangeState<PlayerMoveState>();
        }
    }
    
    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开空闲状态");
    }
}

public class PlayerMoveState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入移动状态");
    }
    
    protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 移动逻辑
        if (!Input.GetKey(KeyCode.W))
        {
            ChangeState<PlayerIdleState>();
        }
    }
    
    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开移动状态");
    }
}

public class PlayerAttackState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        FuLogger.LogInfo("进入攻击状态");
        
        // 设置攻击冷却时间
        Fsm.SetData("AttackCooldown", 1.0f);
    }
    
    protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 更新攻击冷却
        var cooldown = Fsm.GetData<float>("AttackCooldown") - elapseSeconds;
        Fsm.SetData("AttackCooldown", cooldown);
        
        if (cooldown <= 0)
        {
            ChangeState<PlayerIdleState>();
        }
    }
    
    protected internal override void OnLeave(bool isShutdown)
    {
        FuLogger.LogInfo("离开攻击状态");
    }
}
```

### 2. 创建和使用状态机
```csharp
public class PlayerController : MonoBehaviour
{
    private Fsm m_PlayerFsm;
    
    private void Start()
    {
        // 创建状态机
        m_PlayerFsm = Fsm.Create("PlayerFSM", this, 
            new PlayerIdleState(),
            new PlayerMoveState(), 
            new PlayerAttackState()
        );
        
        // 注册到管理器
        GlobalModule.FsmModule.CreateFsm(m_PlayerFsm);
        
        // 启动状态机
        m_PlayerFsm.Start<PlayerIdleState>();
    }
    
    private void Update()
    {
        // 处理攻击输入
        if (Input.GetKeyDown(KeyCode.Space) && m_PlayerFsm.CurrentStateBase is PlayerIdleState)
        {
            m_PlayerFsm.ChangeState<PlayerAttackState>();
        }
    }
    
    private void OnDestroy()
    {
        // 销毁状态机
        GlobalModule.FsmModule.DestroyFsm(m_PlayerFsm);
    }
}
```

### 3. 使用状态机数据
```csharp
public class EnemyAIState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        // 设置初始数据
        Fsm.SetData("TargetPosition", Vector3.zero);
        Fsm.SetData("DetectionRange", 10f);
        Fsm.SetData("AttackRange", 2f);
    }
    
    protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        // 获取数据
        var targetPos = Fsm.GetData<Vector3>("TargetPosition");
        var detectionRange = Fsm.GetData<float>("DetectionRange");
        var attackRange = Fsm.GetData<float>("AttackRange");
        
        // AI逻辑...
    }
}
```

### 4. 通过 GlobalModule 访问状态机模块
```csharp
// 创建状态机
var fsm = Fsm.Create("MyFSM", this, new MyState());
GlobalModule.FsmModule.CreateFsm(fsm);

// 获取状态机
var existingFsm = GlobalModule.FsmModule.GetFsm<PlayerController>();

// 检查状态机是否存在
bool hasFsm = GlobalModule.FsmModule.HasFsm<PlayerController>();

// 销毁状态机
GlobalModule.FsmModule.DestroyFsm(fsm);
```

### 状态机事件系统
```csharp
public class EventDrivenState : FsmStateBase
{
    protected internal override void OnEnter()
    {
        // 订阅事件
        GlobalModule.EventModule.Subscribe("PlayerDamage", OnPlayerDamage);
    }
    
    protected internal override void OnLeave(bool isShutdown)
    {
        // 取消订阅事件
        GlobalModule.EventModule.Unsubscribe("PlayerDamage", OnPlayerDamage);
    }
    
    private void OnPlayerDamage(object sender, GameEventArgs e)
    {
        // 处理伤害事件，可能触发状态切换
        ChangeState<PlayerHurtState>();
    }
}
```

### 复杂状态切换逻辑
```csharp
public class BattleState : FsmStateBase
{
    protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        var enemy = Fsm.GetData<GameObject>("TargetEnemy");
        var health = Fsm.GetData<float>("Health");
        
        if (health <= 0)
        {
            ChangeState<DeathState>();
        }
        else if (enemy == null)
        {
            ChangeState<IdleState>();
        }
        else if (Vector3.Distance(transform.position, enemy.transform.position) > 10f)
        {
            ChangeState<ChaseState>();
        }
        else if (Vector3.Distance(transform.position, enemy.transform.position) <= 2f)
        {
            ChangeState<AttackState>();
        }
    }
}
```

## 编辑器扩展
`FsmManagerInspector` 提供了可视化的状态机监控功能：
- **实时统计**：显示当前管理的状态机数量
- **状态信息**：展示每个状态机的名称、当前状态和运行时间
- **运行状态**：清晰标识状态机的运行、未运行或被销毁状态

## 适用场景

1. **游戏角色AI**：管理角色的移动、攻击、死亡等状态
2. **游戏流程控制**：管理游戏的启动、进行、暂停、结束等状态

## 依赖模块

- **FuFramework.Core**：基础框架模块
- **FuFramework.ReferencePool**：对象池模块
- **FuFramework.Variable**：变量管理模块（用于状态机数据存储）