# FuFramework ReferencePool Module

## 1. 简介

FuFramework ReferencePool 模块是游戏框架的引用池管理系统，专门用于管理纯 C# 类对象的内存分配和回收。该模块通过引用池技术减少 GC（垃圾回收）的频率，显著提高游戏运行效率，特别适合频繁创建和销毁对象的场景。

## 2. 核心特性

- **内存优化**：减少 GC 频率，提高游戏性能
- **类型安全**：支持严格的类型检查和验证
- **统计监控**：提供详细的引用池使用统计信息
- **线程安全**：使用锁机制确保多线程环境下的安全性

## 3. 核心概念

### 3.1 类继承与实现体系

```
【接口体系】

IReference (引用接口)
    └── 方法: void Clear()          # 清理引用状态，准备重用


【类体系】

ModuleBase (框架模块基类)
    └── ReferencePoolModule (引用池管理模块，实例模块)

ReferencePoolModule (实例模块)
    ├── ReferenceCollection (内部类)   # 每个类型对应一个引用集合
    │   ├── m_FreeStack: Stack<IReference>  # 闲置引用栈
    │   ├── UsingReferenceCount            # 正在使用的引用数量
    │   ├── UnusedReferenceCount           # 闲置引用数量
    │   ├── AcquireReferenceCount          # 已获取引用数量
    │   └── ReleaseReferenceCount          # 已释放引用数量
    │
    └── m_ReferenceCollectionDict: Dictionary<Type, ReferenceCollection>
        # 类型到引用集合的映射


【数据结构】

ReferencePoolInfo (结构体)
    ├── Type: Type                # 引用池类型
    ├── UnusedReferenceCount      # 未使用引用数量
    ├── UsingReferenceCount       # 正在使用引用数量
    ├── AcquireReferenceCount     # 已获取引用数量
    ├── ReleaseReferenceCount     # 已释放引用数量
    ├── AddReferenceCount         # 新增引用数量
    └── RemoveReferenceCount      # 移除引用数量
```

### 3.2 引用池架构

```
┌─────────────────────────────────────────────────────────────┐
│                ReferencePoolModule                          │
│                    (ModuleBase)                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │   m_ReferenceCollectionDict: Dictionary<Type,       │   │
│  │                ReferenceCollection>                 │   │
│  │                                                       │   │
│  │  ┌─────────────────┐  ┌─────────────────┐           │   │
│  │  │ TypeA Collection│  │ TypeB Collection│           │   │
│  │  │                 │  │                 │           │   │
│  │  │ m_FreeStack     │  │ m_FreeStack     │           │   │
│  │  │ ┌───┬───┬───┐  │  │ ┌───┬───┬───┐  │           │   │
│  │  │ │Ref│Ref│Ref│  │  │ │Ref│Ref│Ref│  │           │   │
│  │  │ └───┴───┴───┘  │  │ └───┴───┴───┘  │           │   │
│  │  │                 │  │                 │           │   │
│  │  │ 统计信息:       │  │ 统计信息:       │           │   │
│  │  │ - Using: 5      │  │ - Using: 3      │           │   │
│  │  │ - Unused: 10    │  │ - Unused: 8     │           │   │
│  │  │ - Acquire: 100  │  │ - Acquire: 50   │           │   │
│  │  │ - Recycle: 95   │  │ - Recycle: 47   │           │   │
│  │  └─────────────────┘  └─────────────────┘           │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 引用池工作流程

```
【获取引用流程】

Acquire<T>()
    │
    ├── 获取/创建 ReferenceCollection
    │
    └── Acquire<T>() (ReferenceCollection)
        │
        ├── UsingReferenceCount++
        ├── AcquireReferenceCount++
        │
        ├── lock (m_FreeStack)
        │   ├── if (m_FreeStack.Count > 0)
        │   │       return m_FreeStack.Pop()
        │   └── else
        │           AddReferenceCount++
        │           return new T()
        │
        └── 返回引用对象


【释放引用流程】

Recycle(reference)
    │
    ├── 检查引用非空
    │
    ├── 获取 ReferenceCollection
    │
    └── Recycle(reference) (ReferenceCollection)
        │
        ├── lock (m_FreeStack)
        │   ├── if (m_FreeStack.Contains(reference))   # 无条件检测，重复释放即抛异常
        │   │       throw Exception("重复释放")
        │   ├── reference.Clear()           # 清理对象状态（查重之后）
        │   └── m_FreeStack.Push(reference)
        │
        ├── ReleaseReferenceCount++
        └── UsingReferenceCount--


【引用状态流转】

┌─────────────────┐     Acquire      ┌─────────────────┐
│   引用池栈      │ ─────────────────▶ │   正在使用      │
│  (m_FreeStack)  │                    │  (UsingCount)   │
│  (LIFO 后进先出) │ ◀───────────────── │                 │
└─────────────────┘      Recycle       └─────────────────┘
    │    ▲                                    │
    │    │                                    │
    │    └────────────────────────────────────┘
    │              (Clear后重用)
    │
    └── new T() (池为空时创建)
```

### 3.4 生命周期管理

```
【模块生命周期】

OnDispose()
    │
    └── ClearAll()
        ├── 遍历所有 ReferenceCollection
        ├── 每个 collection.RemoveAll()
        └── 清空 m_ReferenceCollectionDict


【引用集合生命周期】

创建 (首次获取某类型引用时)
    │
    ├── new ReferenceCollection(type)
    └── 添加到 m_ReferenceCollectionDict

使用 (Acquire/Recycle)
    │
    ├── Acquire: 从栈取出或创建新对象
    └── Recycle: 清理后压入栈

销毁 (RemoveAll/ClearAll)
    │
    ├── 清空 m_FreeStack
    └── 重置所有计数器
```

## 4. 核心类详细说明

### 4.1 IReference

引用接口，所有需要被引用池管理的类必须实现此接口。

**核心功能：**

```csharp
public interface IReference
{
    /// <summary>
    /// 清理引用。
    /// 在对象被归还到引用池时调用，用于重置对象状态。
    /// </summary>
    void Clear();
}
```

**实现要求：**
- 类必须实现 `Clear()` 方法
- `Clear()` 方法应重置对象的所有字段到初始状态
- 避免在 `Clear()` 中执行耗时操作

### 4.2 ReferencePoolModule (实例模块)

引用池的核心管理模块，继承自 `ModuleBase`，通过 `GlobalModule.ReferencePoolModule` 提供全局的引用获取、释放和管理功能。

**核心功能：**

```csharp
public sealed partial class ReferencePoolModule : ModuleBase
{
    // 引用池数量（管理的引用类型数量）
    public int Count { get; }

    // 获取引用
    public T Acquire<T>() where T : class, IReference, new()

    // 释放引用
    public void Recycle(IReference reference)

    // 添加引用到池
    public void Add<T>(int count) where T : class, IReference, new()

    // 从池移除引用
    public void Remove<T>(int count) where T : class, IReference
    public void RemoveAll<T>() where T : class, IReference

    // 清除所有引用池
    public void ClearAll()

    // 获取统计信息
    public ReferencePoolInfo[] GetAllReferencePoolInfos()
}
```

**实现细节：**
- 使用 `Dictionary<Type, ReferenceCollection>` 管理不同类型的引用池
- 使用 `lock` 确保线程安全

**访问入口：**

```csharp
// 所有操作统一通过 GlobalModule.ReferencePoolModule 访问
GlobalModule.ReferencePoolModule.Acquire<T>();
GlobalModule.ReferencePoolModule.Recycle(reference);
```

### 4.3 ReferenceCollection (内部类)

特定类型的引用集合管理，每个类型对应一个 ReferenceCollection。

**核心功能：**

```csharp
private sealed class ReferenceCollection
{
    // 类型信息
    public Type RefType { get; }

    // 引用栈
    private readonly Stack<IReference> m_FreeStack;

    // 统计信息
    public int UsingReferenceCount { get; }      // 正在使用
    public int UnusedReferenceCount { get; }     // 闲置未使用
    public int AcquireReferenceCount { get; }    // 已获取总数
    public int ReleaseReferenceCount { get; }    // 已释放总数
    public int AddReferenceCount { get; }        // 新增总数
    public int RemoveReferenceCount { get; }     // 移除总数

    // 核心操作
    public T Acquire<T>() where T : class, IReference, new()
    public void Recycle(IReference reference)
    public void Add<T>(int count) where T : class, IReference, new()
    public void Remove(int count)
    public void RemoveAll()
}
```

**实现细节：**
- 使用 `Stack<IReference>` 存储闲置引用（LIFO）
- 所有操作使用 `lock (m_FreeStack)` 确保线程安全
- `Recycle` 时自动调用 `reference.Clear()` 清理对象
- 无条件执行重复释放检测，一旦发现即抛出异常

### 4.4 ReferencePoolModule

引用池管理模块，继承自 `ModuleBase`，负责模块的生命周期管理。

**核心功能：**

```csharp
public sealed partial class ReferencePoolModule : ModuleBase
{
    // 生命周期
    protected internal override void OnDispose()  // 清除所有引用池
}
```

### 4.5 ReferencePoolInfo

引用池信息结构体，用于外部查询引用池的统计信息。

**核心功能：**

```csharp
[StructLayout(LayoutKind.Auto)]
public readonly struct ReferencePoolInfo
{
    public Type Type { get; }                    // 引用池类型
    public int UnusedReferenceCount { get; }     // 未使用引用数量
    public int UsingReferenceCount { get; }      // 正在使用引用数量
    public int AcquireReferenceCount { get; }    // 已获取引用数量
    public int ReleaseReferenceCount { get; }    // 释放引用数量
    public int AddReferenceCount { get; }        // 新增引用数量
    public int RemoveReferenceCount { get; }     // 移除引用数量
}
```

## 5. 使用示例

### 5.1 实现 IReference 接口

创建一个需要被引用池管理的类：

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;

/// <summary>
/// 网络消息类 - 实现 IReference 接口支持引用池
/// </summary>
public class NetworkMessage : IReference
{
    public int MessageId { get; private set; }
    public string Content { get; private set; }
    public DateTime Timestamp { get; private set; }
    
    /// <summary>
    /// 清理引用，准备重用（必须实现）
    /// </summary>
    public void Clear()
    {
        MessageId = 0;
        Content = null;
        Timestamp = default;
    }
    
    /// <summary>
    /// 初始化消息数据
    /// </summary>
    public void Initialize(int messageId, string content)
    {
        MessageId = messageId;
        Content = content;
        Timestamp = DateTime.Now;
    }
}
```

### 5.2 基本使用示例

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;
using UnityEngine;

public class ReferencePoolExample : MonoBehaviour
{
    private void Start()
    {
        // 从引用池获取消息对象
        var message = GlobalModule.ReferencePoolModule.Acquire<NetworkMessage>();
        
        // 初始化消息数据
        message.Initialize(1001, "Hello World");
        
        // 使用消息对象
        Debug.Log($"消息ID: {message.MessageId}, 内容: {message.Content}");
        
        // 使用完毕后归还到引用池
        GlobalModule.ReferencePoolModule.Recycle(message);
    }
}
```

### 5.3 网络消息处理系统

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;
using UnityEngine;

public class NetworkMessageSystem : MonoBehaviour
{
    private void OnEnable()
    {
        // 预创建一些消息对象，减少运行时分配
        GlobalModule.ReferencePoolModule.Add<NetworkMessage>(10);
    }
    
    // 处理接收到的网络数据包
    public void ProcessNetworkPacket(byte[] packetData)
    {
        // 从引用池获取消息对象
        var message = GlobalModule.ReferencePoolModule.Acquire<NetworkMessage>();
        
        try
        {
            // 解析数据包并初始化消息
            var parsedData = ParsePacket(packetData);
            message.Initialize(parsedData.MessageId, parsedData.Content);
            
            // 处理消息
            HandleMessage(message);
        }
        finally
        {
            // 确保消息对象被归还
            GlobalModule.ReferencePoolModule.Recycle(message);
        }
    }
    
    // 批量处理多个消息
    public void ProcessMultipleMessages(List<byte[]> packets)
    {
        var messages = new List<NetworkMessage>();
        
        try
        {
            foreach (var packet in packets)
            {
                var message = GlobalModule.ReferencePoolModule.Acquire<NetworkMessage>();
                var parsedData = ParsePacket(packet);
                message.Initialize(parsedData.MessageId, parsedData.Content);
                messages.Add(message);
            }
            
            // 批量处理消息
            BatchHandleMessages(messages);
        }
        finally
        {
            // 批量归还所有消息对象
            foreach (var message in messages)
            {
                GlobalModule.ReferencePoolModule.Recycle(message);
            }
        }
    }
    
    private void OnDisable()
    {
        // 清理引用池（可选，通常由系统自动管理）
        GlobalModule.ReferencePoolModule.RemoveAll<NetworkMessage>();
    }
}
```

### 5.4 游戏事件系统

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;
using UnityEngine;

// 游戏事件基类
public abstract class GameEvent : IReference
{
    public string EventType { get; protected set; }
    public object Source { get; protected set; }
    public float Timestamp { get; protected set; }
    
    public virtual void Clear()
    {
        EventType = null;
        Source = null;
        Timestamp = 0f;
    }
}

// 具体事件类型
public class PlayerDamageEvent : GameEvent
{
    public int DamageAmount { get; private set; }
    public string DamageType { get; private set; }
    
    public override void Clear()
    {
        base.Clear();
        DamageAmount = 0;
        DamageType = null;
    }
    
    public void Initialize(string source, int damage, string damageType)
    {
        EventType = "PlayerDamage";
        Source = source;
        Timestamp = Time.time;
        DamageAmount = damage;
        DamageType = damageType;
    }
}

public class EventSystem : MonoBehaviour
{
    private readonly List<GameEvent> m_CurrentFrameEvents = new();
    
    private void Update()
    {
        // 处理当前帧的事件
        ProcessFrameEvents();
        
        // 清理事件列表
        ClearFrameEvents();
    }
    
    // 触发玩家伤害事件
    public void TriggerDamageEvent(string source, int damage, string damageType)
    {
        var damageEvent = GlobalModule.ReferencePoolModule.Acquire<PlayerDamageEvent>();
        damageEvent.Initialize(source, damage, damageType);
        m_CurrentFrameEvents.Add(damageEvent);
    }
    
    private void ProcessFrameEvents()
    {
        foreach (var gameEvent in m_CurrentFrameEvents)
        {
            // 分发事件给监听者
            EventDispatcher.Dispatch(gameEvent);
        }
    }
    
    private void ClearFrameEvents()
    {
        // 归还所有事件对象到引用池
        foreach (var gameEvent in m_CurrentFrameEvents)
        {
            GlobalModule.ReferencePoolModule.Recycle(gameEvent);
        }
        m_CurrentFrameEvents.Clear();
    }
}
```

### 5.5 性能监控

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;
using UnityEngine;

public class ReferencePoolMonitor : MonoBehaviour
{
    [SerializeField] private bool m_ShowDebugInfo = true;
    
    private void Update()
    {
        if (!m_ShowDebugInfo) return;
        
        // 获取所有引用池的统计信息
        var poolInfos = GlobalModule.ReferencePoolModule.GetAllReferencePoolInfos();
        
        foreach (var info in poolInfos)
        {
            Debug.Log($"类型: {info.Type.Name}, " +
                     $"使用中: {info.UsingReferenceCount}, " +
                     $"闲置: {info.UnusedReferenceCount}, " +
                     $"获取总数: {info.AcquireReferenceCount}, " +
                     $"释放总数: {info.ReleaseReferenceCount}");
        }
    }
    
    // 手动优化引用池
    public void OptimizePools()
    {
        var poolInfos = GlobalModule.ReferencePoolModule.GetAllReferencePoolInfos();
        
        foreach (var info in poolInfos)
        {
            // 闲置对象过多，提示可手动清理
            if (info.UnusedReferenceCount > 50)
            {
                Debug.Log($"提示: {info.Type.Name} 引用池闲置 {info.UnusedReferenceCount} 个，可调用 Remove<T>() 清理");
            }
        }
        
        // 移除指定类型的闲置对象（Remove 需显式指定泛型类型）
        GlobalModule.ReferencePoolModule.Remove<NetworkMessage>(10);
    }
}
```

### 5.6 使用 using 语句管理生命周期

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;
using UnityEngine;

/// <summary>
/// 包装器类，支持 using 语句自动释放
/// </summary>
public class PooledObject<T> : IDisposable where T : class, IReference, new()
{
    public T Value { get; private set; }
    
    public PooledObject()
    {
        Value = GlobalModule.ReferencePoolModule.Acquire<T>();
    }
    
    public void Dispose()
    {
        if (Value != null)
        {
            GlobalModule.ReferencePoolModule.Recycle(Value);
            Value = null;
        }
    }
}

// 使用示例
public class SafeObjectUsage : MonoBehaviour
{
    public void ProcessData()
    {
        using (var pooledMessage = new PooledObject<NetworkMessage>())
        {
            var message = pooledMessage.Value;
            message.Initialize(1001, "Safe Message");
            
            // 使用 message
            HandleMessage(message);
            
            // 退出 using 块时自动释放
        }
    }
}
```

## 6. 目录结构

```
ReferencePool/
├── ReferencePoolModule.cs                       # 引用池管理模块
├── ReferencePoolModule.ReferenceCollection.cs   # 引用集合实现（Stack）
├── IReference.cs                                # 引用接口
├── ReferencePoolInfo.cs                         # 引用池信息结构体
└── README.md                                    # 本文档
```

## 7. 依赖

| 模块 | 说明 |
|------|------|
| Hotfix.Framework.Core | 提供 ModuleBase 基类 |

> 使用引用池前必须先注册 `ReferencePoolModule`（`HotfixLauncher.RegisterBaseModules()` 已保证），通过 `GlobalModule.ReferencePoolModule` 访问。

## 8. 最佳实践

### 8.1 实现 IReference 规范

```csharp
using Hotfix.Framework.ReferencePool;

public class MyReference : IReference
{
    // 1. 所有字段都需要在 Clear 中重置
    public int Id { get; private set; }
    public string Name { get; private set; }
    public List<int> Items { get; private set; } = new();
    
    // 2. 提供 Initialize 方法设置数据
    public void Initialize(int id, string name)
    {
        Id = id;
        Name = name;
    }
    
    // 3. Clear 方法必须重置所有状态
    public void Clear()
    {
        Id = 0;
        Name = null;
        Items.Clear();  // 清空集合，但不设为 null
    }
}
```

### 8.2 预分配策略

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    [Header("预分配配置")]
    [SerializeField] private int m_PreAllocateCount = 20;
    
    private void Start()
    {
        // 在游戏启动时预分配常用对象
        GlobalModule.ReferencePoolModule.Add<NetworkMessage>(m_PreAllocateCount);
        GlobalModule.ReferencePoolModule.Add<GameEvent>(m_PreAllocateCount);
        GlobalModule.ReferencePoolModule.Add<RedDotNode>(m_PreAllocateCount);
    }
}
```

### 8.3 注意事项

1. **必须实现 Clear 方法**：所有实现 `IReference` 的类必须正确实现 `Clear()` 方法，重置所有字段状态
2. **避免重复释放**：同一对象不能多次释放，否则会抛出异常
3. **线程安全**：引用池操作是线程安全的，但获取的对象本身不是线程安全的
4. **引用类型限制**：引用池只适用于类（class），不适用于结构体（struct）
5. **构造函数要求**：类型必须有默认构造函数（`new()` 约束）
6. **及时释放**：使用完毕后应及时释放对象，避免长时间占用
7. **避免持有引用**：不要在对象释放后继续持有或使用该对象

## 9. 与对象池（ObjectPool）的差异

引用池与对象池是两个**目的不同**的池化系统，边界区分如下。

### 9.1 本质区别

| | 引用池 ReferencePool | 对象池 ObjectPool |
|---|---|---|
| 管理对象 | 纯 C# 数据对象（class） | Unity 场景对象（GameObject） |
| 核心目的 | 减少 **GC 分配**频率 | 减少 **Instantiate/Destroy** 开销 |
| 类比 | 复用"计算器"（用完清屏复用） | 复用"汽车"（用完停车库复用） |

### 9.2 实现原理差异

| 维度 | 引用池 | 对象池 |
|---|---|---|
| 对象来源 | 池空时 `Acquire<T>()` 自建（`new T()`） | 外部 `Register` 注册已创建实例，池**不创建**对象 |
| 存储结构 | `Dictionary<Type, ReferenceCollection>` + `Stack<IReference>` | `FuMultiDictionary` + `Dictionary<object, Object<T>>` |
| 使用状态 | 无（Recycle 即回池） | `SpawnCount`/`IsInUse`、`Locked`、`Priority`、`LastUseTime` |
| 生命周期管理 | 仅 OnDispose 清空 | 容量、过期时间、自动释放、优先级、锁定 |
| 重复检测 | 无条件 `Stack.Contains` 检测 | Recycle 检测 `SpawnCount <= 0` |
| 对象接口 | `IReference`（`Clear()`） | `ObjectBase`（`OnSpawn`/`OnRecycle`/`OnRelease`） |

### 9.3 使用方式差异

```csharp
// 引用池：Acquire 获取（池空自建）、Recycle 回收
var msg = GlobalModule.ReferencePoolModule.Acquire<NetworkMessage>();
GlobalModule.ReferencePoolModule.Recycle(msg);

// 对象池：Register 注册、Get 获取（池空返回 null）、Recycle 回收
pool.Register(entityInstanceObject, true);
var obj = pool.Get("EntityName");
pool.Recycle(obj);
```

### 9.4 判断标准

1. **对象是"数据"还是"实体"**：纯 C# 类 → 引用池；GameObject → 对象池
2. **创建成本**：`new` 便宜但频繁 → 引用池（省 GC）；`Instantiate` 昂贵 → 对象池（省实例化）
3. **是否需要使用状态**：需要计数/锁定/过期 → 对象池；不需要 → 引用池

### 9.5 为什么对象池不直接 new

引用池对象是无参可造的纯数据（`new T()`）；对象池对象创建需要外部资源上下文（预制体/资源句柄/Helper），对象池不持有这些信息。因此对象由创建方模块（如 EntityModule/UIModule）实例化后 `Register` 进池，对象池只负责**复用真实场景对象**。
