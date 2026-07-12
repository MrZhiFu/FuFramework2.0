# FuFramework Event Module

## 1. 简介

FuFramework Event

&#x20;模块是一个高性能、线程安全的事件管理系统。它提供了灵活的事件订阅/发布机制，支持多种事件池模式，并集成了引用池技术以提高性能。

***

## 2. 特性

- 线程安全  ：`Broadcast` 方法支持跨线程调用，事件会在下一帧主线程中分发
- 延迟处理  ：事件默认在下一帧统一处理，避免在处理事件时修改订阅列表导致的异常
- 多播支持  ：支持一个事件对应多个处理函数
- 对象池集成  ：事件参数和事件节点都通过引用池管理，减少GC压力
- 灵活配置  ：支持多种事件池模式（无处理器、多处理器、重复处理器等）
- 模块级管理  ：`EventRegister` 提供模块级的事件订阅管理，自动处理生命周期

***

## 3. 核心概念

### 3.1 事件池模式 (EEventPoolMode)

```csharp
[Flags]
public enum EEventPoolMode : byte
{
    Default = 0,                    // 默认模式：必须存在有且只有一个事件处理函数
    AllowNoHandler = 1,             // 允许不存在事件处理函数
    AllowMultiHandler = 2,          // 允许存在多个事件处理函数
    AllowDuplicateHandler = 4       // 允许存在重复的事件处理函数
}
```

模式说明：

- Default  ：严格模式，每个事件必须有且只有一个处理函数
- AllowNoHandler  ：宽松模式，允许事件没有处理函数（不会抛出异常）
- AllowMultiHandler  ：多播模式，允许一个事件有多个处理函数（观察者模式）
- AllowDuplicateHandler  ：允许同一个处理函数多次订阅同一事件

组合使用示例：

```csharp
// 允许无处理器 + 允许多处理器
EEventPoolMode.AllowNoHandler | EEventPoolMode.AllowMultiHandler
```

### 3.2 事件参数基类

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

// 抛出事件（线程安全，延迟到下一帧处理）
void Broadcast(object sender, GameEventArgs e)
void Broadcast(object sender, string eventId)   // 使用空事件包装事件ID

// 立即抛出事件（非线程安全，立即处理）
void BroadcastNow(object sender, GameEventArgs e)

// 设置默认事件处理器
void SetDefaultHandler(EventHandler<GameEventArgs> handler)

// 遍历
void ForEachHandler(Action<string, EventHandler<GameEventArgs>> action)
void ForEachEvent(Action<object, GameEventArgs> action)
```

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

- 线程安全的事件队列  ：使用 `Queue<Event>` 存储待处理事件
- 多值字典管理订阅  ：`FuMultiDictionary<string, EventHandler<T>>` 存储事件处理函数
- 延迟取消订阅  ：使用 `m_WaitRemoveHandlerList` 实现线程安全的取消订阅
- 锁机制  ：使用 `m_EventHandlerLock` 保证线程安全

工作流程：

1. 订阅阶段  ：`Subscribe` 将处理函数添加到多值字典
2. 取消订阅阶段  ：`Unsubscribe` 将待取消的handler添加到待删除列表
3. 事件处理阶段  ：`Update` 从队列取出事件，先处理待删除列表，再调用处理函数
4. 清理阶段  ：事件处理完成后，通过引用池释放事件参数

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
public sealed class EmptyEventArgs : GameEventArgs
{
    public override string Id => m_EventId;
    private static string m_EventId = typeof(EmptyEventArgs).FullName;
    
    public override void Clear() { }
    
    public static EmptyEventArgs Create(string eventId)
    {
        var eventArgs = ReferencePool.Runtime.ReferencePool.Acquire<EmptyEventArgs>();
        m_EventId = eventId;
        return eventArgs;
    }
}
```

***

## 5. 使用示例

### 5.1 定义事件

```csharp
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
        var args = ReferencePool.Runtime.ReferencePool.Acquire<PlayerDamageEventArgs>();
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
        var args = ReferencePool.Runtime.ReferencePool.Acquire<PlayerLevelUpEventArgs>();
        args.NewLevel = newLevel;
        args.OldLevel = oldLevel;
        return args;
    }
}
```

### 5.2 订阅事件

```csharp
using FuFramework.Core.Runtime;
using FuFramework.Event.Runtime;

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

### 5.6 立即处理事件（非线程安全）

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
        // 注意：此方法非线程安全，只能在主线程调用
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
   Unsubscribe(id, handler) -> 添加到 m_WaitRemoveHandlerList

3. 发布阶段
   Broadcast(sender, args) -> 创建 Event 节点 -> 加入 m_EventQueue

4. 处理阶段（Update）
   从 m_EventQueue 取出事件
   -> ProcessWaitRemoveHandlers() 处理待删除列表
   -> 调用所有匹配的 handler
   -> ReferencePool.Release(args) 释放事件参数
```

### 6.2 线程安全说明

| 方法             | 线程安全 | 处理时机 | 适用场景      |
| -------------- | ---- | ---- | --------- |
| `Broadcast`    | 是    | 下一帧  | 通用场景，推荐   |
| `BroadcastNow` | 否    | 立即   | 需要同步处理的场景 |

线程安全实现原理：

- 使用 `lock (m_EventQueue)` 保护事件队列
- 使用 `lock (m_EventHandlerLock)` 保护事件处理器字典
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
├── Editor/                          # 编辑器扩展代码
│   ├── Inspector/
│   │   └── EventModuleInspector.cs  # EventModule Inspector 扩展
│   └── FuFramework.Event.Editor.asmdef
├── Runtime/                         # 运行时核心代码
│   ├── EventModule.cs               # 事件管理模块
│   ├── EventRegister.cs             # 事件注册器
│   ├── EventPool/                   # 事件池
│   │   ├── EventPool.cs             # 事件池核心实现
│   │   ├── EventPool.Event.cs       # 事件节点定义
│   │   └── EEventPoolMode.cs        # 事件池模式枚举
│   ├── Event/                       # 事件参数
│   │   ├── BaseEventArgs.cs         # 事件参数基类
│   │   ├── GameEventArgs.cs         # 游戏事件参数基类
│   │   └── EmptyEventArgs.cs        # 空事件参数
│   └── FuFramework.Event.Runtime.asmdef
└── README.md                        # 本文档
```

***

## 9. 依赖

- Unity  : 2021.3 LTS 或更高版本
- FuFramework.Core  : 框架核心模块
- FuFramework.ReferencePool  : 引用池模块

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
        var args = ReferencePool.Runtime.ReferencePool.Acquire<MyEventArgs>();
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
2. 线程安全  ：`Broadcast` 是线程安全的，但 `BroadcastNow` 只能在主线程调用
3. 事件处理顺序  ：同一事件的多个处理函数按订阅顺序调用
4. 异常处理  ：事件处理函数中的异常会被捕获并记录，不会影响其他处理函数
5. 对象池  ：事件参数对象会自动通过引用池管理，无需手动释放，但需正确实现 `Clear` 方法
6. 延迟取消订阅  ：取消订阅会在下一帧事件处理前生效，当前帧仍会收到事件
7. 空事件ID  ：使用 `EmptyEventArgs` 时，事件ID会被静态变量共享，注意并发问题

