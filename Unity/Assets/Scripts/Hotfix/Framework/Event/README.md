# FuFramework Event Module

## 1. 简介

FuFramework Event 模块是一个高性能的事件管理系统。它提供了灵活的事件订阅/发布机制，集成引用池技术以减少 GC 压力。模块仅允许主线程访问；跨线程产生的事件（如网络 Socket 回调）由网络频道封送到主线程后再 Broadcast。

***

## 2. 特性

- 仅主线程   ：公共 API 仅允许主线程调用，开发期（UNITY_ASSERTIONS）有断言拦截跨线程误用
- 延迟处理   ：事件默认在下一帧统一处理，避免在处理事件时修改订阅列表导致的异常
- 多播支持   ：支持一个事件对应多个处理函数；同一处理函数被多个订阅者订阅时按引用计数每事件只调用一次
- 对象池集成 ：事件参数和事件节点都通过引用池管理，减少GC压力
- 模块级管理 ：`EventRegister` 提供模块级的事件订阅管理，自动处理生命周期

***

## 3. 核心概念

### 3.1 事件参数基类

BaseEventArgs

：所有事件参数的抽象基类

```csharp
public abstract class BaseEventArgs : EventArgs, IReference
{
    public abstract string Id { get; }      // 事件唯一标识
    public abstract void Clear();           // 清理引用（用于对象池重用）
}
```

GameEventArgs

：游戏逻辑事件基类，继承自 `BaseEventArgs`

EmptyEventArgs

：轻量级空事件，用于不需要携带数据的事件通信

***

## 4. 核心类说明

### 4.1 EventModule

事件管理模块，继承自 `ModuleBase`，是事件系统的核心管理类。

核心属性：

```csharp
int EventHandlerCount { get; }      // 已注册的事件处理函数总数
int EventCount { get; }             // 当前待处理的事件数量
```

主要方法：

```csharp
// 订阅/取消订阅事件
void Subscribe(string id, EventHandler<GameEventArgs> handler)
void Unsubscribe(string id, EventHandler<GameEventArgs> handler)

// 检查订阅状态
bool Check(string id, EventHandler<GameEventArgs> handler)
int Count(string id)                    // 获取指定事件的处理函数数量

// 抛出事件（延迟到下一帧主线程分发）
void Broadcast(object sender, GameEventArgs e)
void Broadcast(object sender, string eventId)   // 使用空事件包装事件ID

// 立即抛出事件（同步处理，仅限主线程调用）
void BroadcastNow(object sender, GameEventArgs e)

// 设置默认事件处理器
void SetDefaultHandler(EventHandler<GameEventArgs> handler)

// 遍历
void ForEachHandler(Action<string, EventHandler<GameEventArgs>> action)
void ForEachEvent(Action<object, GameEventArgs> action)
```

> `Subscribe` 不做「已存在即早退」的去重：同一 handler 可被多个订阅者（多个 `EventRegister`、多个模块）共享订阅，
> 由事件池按 `(id, handler)` 引用计数，`Unsubscribe` 只递减本订阅者那一份（详见 4.3）。

***

### 4.2 EventRegister

事件注册器，用于模块级的事件订阅管理，实现 `IReference` 接口支持引用池。

功能特点：

- 集中管理一个模块的所有事件订阅
- 支持一键取消所有订阅（`UnSubscribeAll`）
- 自动与 `EventModule` 交互

主要方法：

```csharp
// 创建
static EventRegister Create()

// 订阅/取消订阅
void Subscribe(string id, EventHandler<GameEventArgs> handler)
void UnSubscribe(string id, EventHandler<GameEventArgs> handler)
void UnSubscribeAll()                   // 取消所有订阅

// 派发事件
void Broadcast(object sender, GameEventArgs eventArgs)
void Broadcast(object sender, string eventId)
void BroadcastNow(object sender, GameEventArgs eventArgs)

// 释放
void Clear()                            // 清理（自动调用 UnSubscribeAll）
void Release()                          // 归还引用池
```

***

### 4.3 EventPool<T>

事件池，事件处理的核心容器，管理事件的订阅、发布和处理。

核心机制：

- 事件队列  ：使用 `Queue<Event>` 存储待处理事件，仅主线程访问
- 多值字典管理订阅  ：`FuMultiDictionary<string, EventHandler<T>>` 存储事件处理函数
- 延迟取消订阅  ：使用 `m_WaitRemoveHandlerList` 把退订延迟到下一帧生效（同一 (id, handler) 只登记一条，重新订阅即撤销登记）
- 引用计数订阅  ：同一 `(id, handler)` 被多个订阅者（多个 `EventRegister`、多个模块）共享时按份计数，`Subscribe` 计 +1、`Unsubscribe` 计 -1，归零才真正移除条目；单个订阅者退订不影响其他订阅者
- 退订契约  ：各订阅者只能退订自己登记的那一份；超额退订（次数超过自身订阅数）会继续消耗其他订阅者的计数，导致他人订阅被静默移除且无告警
- 主线程契约  ：无锁设计，公共 API 仅允许主线程调用，开发期（UNITY_ASSERTIONS）断言拦截跨线程误用

工作流程：

1. 订阅阶段  ：`Subscribe` 将处理函数添加到多值字典（已存在则引用计数 +1，不重复入字典）
2. 取消订阅阶段  ：`Unsubscribe` 引用计数递减，归零才把 handler 添加到待删除列表
3. 事件处理阶段  ：`Update` 从队列取出事件，先处理待删除列表，再调用处理函数
4. 清理阶段  ：事件处理完成后，通过引用池释放事件参数

> 注意：事件参数在分发结束（`Broadcast` 的下一帧分发或 `BroadcastNow` 立即分发）后即被回收并 `Clear`，
> 处理函数**不得转发或缓存收到的 `eArgs`**，否则后续处理函数会观测到已清空的数据；需要转发时请新建事件参数对象。

***

### 4.4 事件参数类

BaseEventArgs

：

```csharp
public abstract class BaseEventArgs : EventArgs, IReference
{
    public abstract string Id { get; }
    public abstract void Clear();
}
```

GameEventArgs

：

```csharp
public abstract class GameEventArgs : BaseEventArgs { }
```

EmptyEventArgs

：

```csharp
using Hotfix.Framework.Core;

public sealed class EmptyEventArgs : GameEventArgs
{
    public override string Id => m_EventId;
    // 实例字段：事件下一帧才分发，若用静态字段会被同帧抛出的其它事件编号覆盖
    private string m_EventId = typeof(EmptyEventArgs).FullName;

    public override void Clear() => m_EventId = typeof(EmptyEventArgs).FullName;

    public static EmptyEventArgs Create(string eventId)
    {
        var eventArgs = ReferencePool.Acquire<EmptyEventArgs>();
        eventArgs.m_EventId = eventId;
        return eventArgs;
    }
}
```

***

## 5. 使用示例

### 5.1 定义事件

```csharp
using Hotfix.Framework.Core;

// 定义事件ID（推荐使用常量或枚举）
public static class EventIds
{
    public const string PlayerDamage = "PlayerDamage";
    public const string PlayerLevelUp = "PlayerLevelUp";
    public const string GameStart = "GameStart";
    public const string GameOver = "GameOver";
}

// 创建自定义事件参数
public class PlayerDamageEventArgs : GameEventArgs
{
    public override string Id => EventIds.PlayerDamage;
    
    public int Damage { get; private set; }
    public GameObject Attacker { get; private set; }
    public Vector3 HitPosition { get; private set; }
    
    public override void Clear()
    {
        Damage = 0;
        Attacker = null;
        HitPosition = Vector3.zero;
    }
    
    public static PlayerDamageEventArgs Create(int damage, GameObject attacker, Vector3 hitPosition)
    {
        var args = ReferencePool.Acquire<PlayerDamageEventArgs>();
        args.Damage = damage;
        args.Attacker = attacker;
        args.HitPosition = hitPosition;
        return args;
    }
}

// 玩家升级事件参数
public class PlayerLevelUpEventArgs : GameEventArgs
{
    public override string Id => EventIds.PlayerLevelUp;
    
    public int NewLevel { get; private set; }
    public int OldLevel { get; private set; }
    
    public override void Clear()
    {
        NewLevel = 0;
        OldLevel = 0;
    }
    
    public static PlayerLevelUpEventArgs Create(int newLevel, int oldLevel)
    {
        var args = ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.NewLevel = newLevel;
        args.OldLevel = oldLevel;
        return args;
    }
}
```

### 5.2 订阅事件

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Event;

public class PlayerController : MonoBehaviour
{
    private EventModule m_EventModule;
    
    private void Start()
    {
        // 获取事件模块
        m_EventModule = ModuleManager.GetModule<EventModule>();
        
        // 订阅自定义事件
        m_EventModule.Subscribe(EventIds.PlayerDamage, OnPlayerDamage);
        m_EventModule.Subscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
        
        // 订阅空事件（无数据事件）
        m_EventModule.Subscribe(EventIds.GameStart, OnGameStart);
    }
    
    private void OnPlayerDamage(object sender, GameEventArgs e)
    {
        if (e is PlayerDamageEventArgs damageArgs)
        {
            Debug.Log($"玩家受到 {damageArgs.Damage} 点伤害");
            Debug.Log($"攻击者: {damageArgs.Attacker?.name}");
            Debug.Log($"受击位置: {damageArgs.HitPosition}");
            
            // 扣血逻辑...
        }
    }
    
    private void OnPlayerLevelUp(object sender, GameEventArgs e)
    {
        if (e is PlayerLevelUpEventArgs levelArgs)
        {
            Debug.Log($"玩家升级！{levelArgs.OldLevel} -> {levelArgs.NewLevel}");
            
            // 升级逻辑...
        }
    }
    
    private void OnGameStart(object sender, GameEventArgs e)
    {
        Debug.Log("游戏开始！");
        // 游戏开始逻辑...
    }
    
    private void OnDestroy()
    {
        // 取消订阅（重要！避免内存泄漏）
        if (m_EventModule != null)
        {
            m_EventModule.Unsubscribe(EventIds.PlayerDamage, OnPlayerDamage);
            m_EventModule.Unsubscribe(EventIds.PlayerLevelUp, OnPlayerLevelUp);
            m_EventModule.Unsubscribe(EventIds.GameStart, OnGameStart);
        }
    }
}
```

### 5.3 发布事件

```csharp
public class EnemyController : MonoBehaviour
{
    private EventModule m_EventModule;
    
    private void Start()
    {
        m_EventModule = ModuleManager.GetModule<EventModule>();
    }
    
    private void AttackPlayer(PlayerController player, int damage)
    {
        // 创建并发布自定义事件
        var damageArgs = PlayerDamageEventArgs.Create(
            damage: damage,
            attacker: gameObject,
            hitPosition: player.transform.position
        );
        
        m_EventModule.Broadcast(this, damageArgs);
        // 注意：事件参数会在处理完成后自动归还引用池，无需手动释放
    }
}

public class ExperienceSystem : MonoBehaviour
{
    private EventModule m_EventModule;
    private int m_CurrentLevel = 1;
    
    private void Start()
    {
        m_EventModule = ModuleManager.GetModule<EventModule>();
    }
    
    public void AddExperience(int exp)
    {
        int oldLevel = m_CurrentLevel;
        
        // 计算新等级...
        m_CurrentLevel = CalculateNewLevel(exp);
        
        if (m_CurrentLevel > oldLevel)
        {
            // 发布升级事件
            var levelArgs = PlayerLevelUpEventArgs.Create(m_CurrentLevel, oldLevel);
            m_EventModule.Broadcast(this, levelArgs);
        }
    }
    
    public void StartGame()
    {
        // 发布空事件（无数据）
        m_EventModule.Broadcast(this, EventIds.GameStart);
    }
}
```

### 5.4 使用 EventRegister 进行模块级事件管理

```csharp
public class UIModule : MonoBehaviour
{
    private EventRegister m_EventRegister;
    
    private void Start()
    {
        // 创建事件注册器
        m_EventRegister = EventRegister.Create();
        
        // 使用 EventRegister 订阅事件
        m_EventRegister.Subscribe(EventIds.PlayerDamage, OnPlayerDamageUI);
        m_EventRegister.Subscribe(EventIds.PlayerLevelUp, OnPlayerLevelUpUI);
        m_EventRegister.Subscribe(EventIds.GameStart, OnGameStartUI);
        m_EventRegister.Subscribe(EventIds.GameOver, OnGameOverUI);
    }
    
    private void OnPlayerDamageUI(object sender, GameEventArgs e)
    {
        if (e is PlayerDamageEventArgs damageArgs)
        {
            // 显示伤害数字
            ShowDamageNumber(damageArgs.Damage, damageArgs.HitPosition);
            // 更新血条
            UpdateHealthBar();
        }
    }
    
    private void OnPlayerLevelUpUI(object sender, GameEventArgs e)
    {
        if (e is PlayerLevelUpEventArgs levelArgs)
        {
            // 显示升级特效
            ShowLevelUpEffect(levelArgs.NewLevel);
        }
    }
    
    private void OnGameStartUI(object sender, GameEventArgs e)
    {
        // 显示游戏开始界面
        ShowGameStartPanel();
    }
    
    private void OnGameOverUI(object sender, GameEventArgs e)
    {
        // 显示游戏结束界面
        ShowGameOverPanel();
    }
    
    private void OnDestroy()
    {
        // 一键取消所有订阅
        if (m_EventRegister != null)
        {
            m_EventRegister.UnSubscribeAll();
            m_EventRegister.Release();  // 归还引用池
            m_EventRegister = null;
        }
    }
}
```

### 5.5 使用默认事件处理器

```csharp
public class EventDebugger : MonoBehaviour
{
    private EventModule m_EventModule;
    
    private void Start()
    {
        m_EventModule = ModuleManager.GetModule<EventModule>();
        
        // 设置默认事件处理器（处理未被订阅的事件）
        m_EventModule.SetDefaultHandler(OnDefaultEvent);
    }
    
    private void OnDefaultEvent(object sender, GameEventArgs e)
    {
        Debug.LogWarning($"未处理的事件: {e.Id}, 发送者: {sender}");
    }
    
    private void OnDestroy()
    {
        m_EventModule?.SetDefaultHandler(null);
    }
}
```

### 5.6 立即处理事件（同步处理）

```csharp
public class CriticalSystem : MonoBehaviour
{
    private EventModule m_EventModule;
    
    private void Start()
    {
        m_EventModule = ModuleManager.GetModule<EventModule>();
    }
    
    public void HandleCriticalError(string errorMessage)
    {
        var errorArgs = ErrorEventArgs.Create(errorMessage);
        
        // 使用 BroadcastNow 立即处理（同步执行）
        // 注意：此方法同步执行，仅限主线程调用
        m_EventModule.BroadcastNow(this, errorArgs);
        
        // 事件处理完成后才会执行到这里
        Debug.Log("错误事件已处理完成");
    }
}
```

***

## 6. 事件系统

### 6.1 事件处理流程

```
1. 订阅阶段
   Subscribe(id, handler) -> 添加到 m_EventHandlerMultiDict

2. 取消订阅阶段
   Unsubscribe(id, handler) -> 引用计数递减，归零才登记到 m_WaitRemoveHandlerList

3. 发布阶段
   Broadcast(sender, args) -> 创建 Event 节点 -> 加入 m_EventQueue

4. 处理阶段（Update）
   从 m_EventQueue 取出事件
   -> ProcessWaitRemoveHandlers() 处理待删除列表
   -> 调用所有匹配的 handler
   -> ReferencePool.Recycle(args) 释放事件参数
```

### 6.2 主线程契约说明

事件池仅允许主线程访问：公共 API 入口均有 `AssertMainThread` 断言（`[Conditional("UNITY_ASSERTIONS")]`，仅 Editor/Development 构建生效，发布版被剥离、零运行时开销），跨线程调用在开发期立即报错定位。

| 方法                                                                                                       | 处理时机 | 说明 |
| -------------------------------------------------------------------------------------------------------- | ---- | --------- |
| `Broadcast`    | 下一帧  | 事件入队，`Update` 时统一分发，通用场景，推荐   |
| `BroadcastNow` | 立即（同步）   | 仅限主线程，需要同步处理的场景 |
| `Subscribe` / `Unsubscribe` / `Check` / `Count` / `SetDefaultHandler` / `EventCount` / `EventHandlerCount` | 立即    | 仅限主线程   |
| `ForEachHandler` / `ForEachEvent`                                                                          | 立即    | 仅限主线程；回调内可重入触发分发/遍历，重入时使用局部快照互不干扰 |

跨线程产生的事件（如网络 Socket 回调）不得直接调用事件池，由网络频道封送到主线程后（channel.Update 排水）再 `Broadcast`。

实现机制：

- 无锁设计  ：单线程契约下无需加锁，事件队列与处理器字典均仅主线程访问
- 取消订阅使用延迟删除机制，避免在处理事件时修改集合

***

## 7. 编辑器功能

### 7.1 EventModuleInspector

`EventModule` 的 Inspector 扩展，提供运行时事件监控功能。

功能：

- 统计信息  ：显示已注册的事件处理函数数量和当前待处理的事件数量
- 处理器列表  ：列出所有已注册的事件ID和对应的处理函数
- 事件队列  ：显示当前帧待处理的事件列表

使用方法：

1. 在编辑器中运行游戏
2. 在 Hierarchy 中找到 `[FrameworkModule]` 下的 `EventModule`
3. 选中后在 Inspector 面板查看事件统计信息

***

## 8. 目录结构说明

```text
Event/
├── EventModule.cs               # 事件管理模块
├── EventRegister.cs             # 事件注册器
├── EventPool/                   # 事件池
│   ├── EventPool.cs             # 事件池核心实现
│   └── EventPool.Event.cs       # 事件节点定义
├── Event/                       # 事件参数
│   ├── BaseEventArgs.cs         # 事件参数基类
│   ├── GameEventArgs.cs         # 游戏事件参数基类
│   └── EmptyEventArgs.cs        # 空事件参数
└── README.md                    # 本文档
```

***

## 9. 依赖

- Unity  : 2021.3 LTS 或更高版本
- Hotfix.Framework.Core  : 框架核心模块
- Hotfix.Framework.ReferencePool  : 引用池模块

***

## 10. 最佳实践

### 10.1 事件ID管理

推荐使用常量类或枚举管理事件ID，避免硬编码：

```csharp
// 方式1：常量类
public static class EventIds
{
    public const string PlayerDamage = "PlayerDamage";
    public const string PlayerDeath = "PlayerDeath";
}

// 方式2：枚举（配合 EmptyEventArgs 使用）
public enum GameEvents
{
    GameStart,
    GamePause,
    GameResume,
    GameOver
}

// 使用枚举发布事件
m_EventModule.Broadcast(this, GameEvents.GameStart.ToString());
```

### 10.2 事件参数对象池

自定义事件参数应正确实现 `Clear` 方法，确保对象池正确重用：

```csharp
using Hotfix.Framework.Core;

public class MyEventArgs : GameEventArgs
{
    public override string Id => "MyEvent";
    
    public int IntValue { get; set; }
    public string StringValue { get; set; }
    public List<int> ListValue { get; set; }  // 引用类型
    
    public override void Clear()
    {
        // 值类型重置
        IntValue = 0;
        StringValue = null;
        
        // 引用类型清理（避免内存泄漏）
        ListValue?.Clear();
        ListValue = null;
    }
    
    public static MyEventArgs Create(int intValue, string stringValue)
    {
        var args = ReferencePool.Acquire<MyEventArgs>();
        args.IntValue = intValue;
        args.StringValue = stringValue;
        return args;
    }
}
```

### 10.3 使用 EventRegister 管理生命周期

对于UI模块等需要频繁订阅/取消订阅的场景，使用 `EventRegister`：

```csharp
public class GamePanel : MonoBehaviour
{
    private EventRegister m_EventRegister;
    
    private void OnEnable()
    {
        m_EventRegister = EventRegister.Create();
        m_EventRegister.Subscribe(EventIds.UpdateUI, OnUpdateUI);
    }
    
    private void OnDisable()
    {
        m_EventRegister?.UnSubscribeAll();
        m_EventRegister?.Release();
        m_EventRegister = null;
    }
}
```

### 10.4 避免内存泄漏

```csharp
public class Example : MonoBehaviour
{
    private void Start()
    {
        // 错误：使用匿名方法订阅，无法取消订阅
        m_EventModule.Subscribe(EventIds.SomeEvent, (s, e) => { /* ... */ });
    }
}

// 正确：使用实例方法订阅
public class Example : MonoBehaviour
{
    private void Start()
    {
        m_EventModule.Subscribe(EventIds.SomeEvent, OnSomeEvent);
    }
    
    private void OnSomeEvent(object sender, GameEventArgs e)
    {
        // 处理事件
    }
    
    private void OnDestroy()
    {
        m_EventModule.Unsubscribe(EventIds.SomeEvent, OnSomeEvent);
    }
}
```

### 10.5 事件处理器性能

```csharp
// 避免在事件处理器中执行耗时操作
private void OnPlayerDamage(object sender, GameEventArgs e)
{
    // 错误：耗时操作会阻塞事件处理
    // var result = HeavyCalculation();
    
    // 正确：只记录状态，耗时操作延后处理
    m_DamageQueue.Enqueue(e);
}

// 在 Update 中处理耗时操作
private void Update()
{
    while (m_DamageQueue.Count > 0 && m_ProcessedCount < MaxPerFrame)
    {
        ProcessDamage(m_DamageQueue.Dequeue());
        m_ProcessedCount++;
    }
}
```

***

## 11. 注意事项

1. 取消订阅  ：在对象销毁时务必取消事件订阅，避免内存泄漏
2. 主线程限制  ：所有公共 API 仅允许主线程调用（开发期有断言拦截跨线程误用）；跨线程产生的事件由网络频道封送主线程后再 Broadcast
3. 事件处理顺序  ：同一事件的多个处理函数按订阅顺序调用
4. 异常处理  ：事件处理函数中的异常会被捕获并记录，不会影响其他处理函数
5. 对象池  ：事件参数对象会自动通过引用池管理，无需手动释放，但需正确实现 `Clear` 方法
6. 延迟取消订阅  ：取消订阅会在下一帧事件处理前生效，当前帧仍会收到事件
7. 空事件ID  ：`EmptyEventArgs` 的事件ID为实例字段，同帧多个不同ID不会互相覆盖
8. 生命周期边界  ：事件池关停（模块 `OnDispose`）后不得再调用 `Broadcast`——事件节点「取出 → 入队」之间存在极小竞态窗口，滞留事件将永不分发、永不回收
9. 遍历接口  ：`ForEachHandler` / `ForEachEvent` 仅限主线程调用；`ForEachEvent` 回调内不得修改或回收未分发的事件参数

