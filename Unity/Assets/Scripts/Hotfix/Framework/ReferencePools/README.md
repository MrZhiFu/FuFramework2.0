# FuFramework ReferencePool Module

## 1. 简介

FuFramework ReferencePool 模块是游戏框架的引用池管理系统，专门用于管理纯 C# 类对象的内存分配和回收。该模块通过引用池技术减少 GC（垃圾回收）的频率，显著提高游戏运行效率，特别适合频繁创建和销毁对象的场景。

## 2. 核心特性

- **内存优化**：减少 GC 频率，提高游戏性能
- **类型安全**：支持严格的类型检查和验证
- **统计监控**：提供详细的引用池使用统计信息
- **灵活配置**：支持多种严格检查模式
- **线程安全**：使用锁机制确保多线程环境下的安全性

## 3. 核心概念

### 3.1 类继承与实现体系

```
【接口体系】

IReference (引用接口)
    └── 方法: void Clear()          # 清理引用状态，准备重用


【类体系】

ModuleBase (框架模块基类)
    └── ReferencePoolModule (引用池管理模块)
        └── 管理严格检查模式配置

ReferencePool (静态类)
    ├── ReferenceCollection (内部类)   # 每个类型对应一个引用集合
    │   ├── m_FreeQueue: Queue<IReference>  # 闲置引用队列
    │   ├── UsingReferenceCount            # 正在使用的引用数量
    │   ├── UnusedReferenceCount           # 闲置引用数量
    │   ├── AcquireReferenceCount          # 已获取引用数量
    │   └── ReleaseReferenceCount          # 已释放引用数量
    │
    └── ReferenceCollectionDict: Dictionary<Type, ReferenceCollection>
        # 类型到引用集合的映射


【枚举】

EReferenceStrictCheckType (严格检查类型)
    ├── AlwaysEnable              # 总是启用
    ├── OnlyEnableWhenDevelopment # 仅在开发模式启用
    ├── OnlyEnableInEditor        # 仅在编辑器启用
    └── AlwaysDisable             # 总是禁用


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
│                    (ModuleBase)                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           m_EnableStrictCheck                       │   │
│  │     EReferenceStrictCheckType 枚举                  │   │
│  │     - 控制是否启用类型严格检查                      │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   ReferencePool (静态类)                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │         ReferenceCollectionDict                     │   │
│  │    Dictionary<Type, ReferenceCollection>            │   │
│  │                                                       │   │
│  │  ┌─────────────────┐  ┌─────────────────┐           │   │
│  │  │ TypeA Collection│  │ TypeB Collection│           │   │
│  │  │                 │  │                 │           │   │
│  │  │ m_FreeQueue     │  │ m_FreeQueue     │           │   │
│  │  │ ┌───┬───┬───┐  │  │ ┌───┬───┬───┐  │           │   │
│  │  │ │Ref│Ref│Ref│  │  │ │Ref│Ref│Ref│  │           │   │
│  │  │ └───┴───┴───┘  │  │ └───┴───┴───┘  │           │   │
│  │  │                 │  │                 │           │   │
│  │  │ 统计信息:       │  │ 统计信息:       │           │   │
│  │  │ - Using: 5      │  │ - Using: 3      │           │   │
│  │  │ - Unused: 10    │  │ - Unused: 8     │           │   │
│  │  │ - Acquire: 100  │  │ - Acquire: 50   │           │   │
│  │  │ - Release: 95   │  │ - Release: 47   │           │   │
│  │  └─────────────────┘  └─────────────────┘           │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 引用池工作流程

```
【获取引用流程】

Acquire<T>()
    │
    ├── 检查类型 (EnableStrictCheck)
    │
    ├── 获取/创建 ReferenceCollection
    │
    └── Acquire<T>() (ReferenceCollection)
        │
        ├── UsingReferenceCount++
        ├── AcquireReferenceCount++
        │
        ├── lock (m_FreeQueue)
        │   ├── if (m_FreeQueue.Count > 0)
        │   │       return m_FreeQueue.Dequeue()
        │   └── else
        │           AddReferenceCount++
        │           return new T()
        │
        └── 返回引用对象


【释放引用流程】

Release(reference)
    │
    ├── 检查引用非空
    ├── 检查类型 (EnableStrictCheck)
    │
    ├── 获取 ReferenceCollection
    │
    └── Release(reference) (ReferenceCollection)
        │
        ├── reference.Clear()           # 清理对象状态
        │
        ├── lock (m_FreeQueue)
        │   ├── if (m_FreeQueue.Contains(reference))
        │   │       throw Exception("重复释放")
        │   └── m_FreeQueue.Enqueue(reference)
        │
        ├── ReleaseReferenceCount++
        └── UsingReferenceCount--


【引用状态流转】

┌─────────────────┐     Acquire      ┌─────────────────┐
│   引用池队列    │ ─────────────────▶ │   正在使用      │
│  (m_FreeQueue)  │                    │  (UsingCount)   │
│                 │ ◀───────────────── │                 │
└─────────────────┘      Release       └─────────────────┘
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

OnInit()
    │
    ├── 根据 m_EnableStrictCheck 设置 EnableStrictCheck
    │   ├── AlwaysEnable              → true
    │   ├── OnlyEnableWhenDevelopment → Debug.isDebugBuild
    │   ├── OnlyEnableInEditor        → Application.isEditor
    │   └── AlwaysDisable             → false
    │
    └── 输出严格检查启用日志

OnDispose()
    │
    └── ReferencePool.ClearAll()
        ├── 遍历所有 ReferenceCollection
        ├── 每个 collection.RemoveAll()
        └── 清空 ReferenceCollectionDict


【引用集合生命周期】

创建 (首次获取某类型引用时)
    │
    ├── new ReferenceCollection(type)
    └── 添加到 ReferenceCollectionDict

使用 (Acquire/Release)
    │
    ├── Acquire: 从队列取出或创建新对象
    └── Release: 清理后放回队列

销毁 (RemoveAll/ClearAll)
    │
    ├── 清空 m_FreeQueue
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

### 4.2 ReferencePool (静态类)

引用池的核心管理类，提供全局的引用获取、释放和管理功能。

**核心功能：**

```csharp
public static partial class ReferencePool
{
    // 严格检查开关
    public static bool EnableStrictCheck { get; set; }
    
    // 引用池数量
    public static int Count { get; }
    
    // 获取引用
    public static T Acquire<T>() where T : class, IReference, new()
    public static IReference Acquire(Type refType)
    
    // 释放引用
    public static void Release(IReference reference)
    
    // 添加引用到池
    public static void Add<T>(int count) where T : class, IReference, new()
    public static void Add(Type refType, int count)
    
    // 从池移除引用
    public static void Remove<T>(int count) where T : class, IReference
    public static void Remove(Type refType, int count)
    public static void RemoveAll<T>() where T : class, IReference
    public static void RemoveAll(Type refType)
    
    // 清除所有引用池
    public static void ClearAll()
    
    // 获取统计信息
    public static ReferencePoolInfo[] GetAllReferencePoolInfos()
}
```

**实现细节：**
- 使用 `Dictionary<Type, ReferenceCollection>` 管理不同类型的引用池
- 使用 `lock` 确保线程安全
- 严格检查模式验证类型是否合法（非空、非抽象、实现 IReference）

### 4.3 ReferenceCollection (内部类)

特定类型的引用集合管理，每个类型对应一个 ReferenceCollection。

**核心功能：**

```csharp
private sealed class ReferenceCollection
{
    // 类型信息
    public Type RefType { get; }
    
    // 引用队列
    private readonly Queue<IReference> m_FreeQueue;
    
    // 统计信息
    public int UsingReferenceCount { get; }      // 正在使用
    public int UnusedReferenceCount { get; }     // 闲置未使用
    public int AcquireReferenceCount { get; }    // 已获取总数
    public int ReleaseReferenceCount { get; }    // 已释放总数
    public int AddReferenceCount { get; }        // 新增总数
    public int RemoveReferenceCount { get; }     // 移除总数
    
    // 核心操作
    public T Acquire<T>() where T : class, IReference, new()
    public IReference Acquire()
    public void Release(IReference reference)
    public void Add<T>(int count) where T : class, IReference, new()
    public void Add(int count)
    public void Remove(int count)
    public void RemoveAll()
}
```

**实现细节：**
- 使用 `Queue<IReference>` 存储闲置引用（FIFO）
- 所有操作使用 `lock (m_FreeQueue)` 确保线程安全
- `Release` 时自动调用 `reference.Clear()` 清理对象
- 检测重复释放，防止逻辑错误

### 4.4 ReferencePoolModule

引用池管理模块，继承自 `ModuleBase`，负责模块的生命周期管理。

**核心功能：**

```csharp
public sealed class ReferencePoolModule : ModuleBase
{
    [SerializeField]
    private EReferenceStrictCheckType m_EnableStrictCheck = EReferenceStrictCheckType.OnlyEnableInEditor;
    
    // 严格检查开关
    public static bool EnableStrictCheck { get; set; }
    
    // 生命周期
    protected override void OnInit()     // 初始化严格检查模式
    protected override void OnDispose()  // 清除所有引用池
}
```

**严格检查模式：**
- `AlwaysEnable`：总是启用类型检查
- `OnlyEnableWhenDevelopment`：仅在开发模式启用
- `OnlyEnableInEditor`：仅在编辑器启用（默认）
- `AlwaysDisable`：总是禁用

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
using Hotfix.Framework.ReferencePools;

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
using Hotfix.Framework.ReferencePools;
using UnityEngine;

public class ReferencePoolExample : MonoBehaviour
{
    private void Start()
    {
        // 从引用池获取消息对象
        var message = ReferencePool.Acquire<NetworkMessage>();
        
        // 初始化消息数据
        message.Initialize(1001, "Hello World");
        
        // 使用消息对象
        Debug.Log($"消息ID: {message.MessageId}, 内容: {message.Content}");
        
        // 使用完毕后归还到引用池
        ReferencePool.Release(message);
    }
}
```

### 5.3 网络消息处理系统

```csharp
public class NetworkMessageSystem : MonoBehaviour
{
    private void OnEnable()
    {
        // 预创建一些消息对象，减少运行时分配
        ReferencePool.Add<NetworkMessage>(10);
    }
    
    // 处理接收到的网络数据包
    public void ProcessNetworkPacket(byte[] packetData)
    {
        // 从引用池获取消息对象
        var message = ReferencePool.Acquire<NetworkMessage>();
        
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
            ReferencePool.Release(message);
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
                var message = ReferencePool.Acquire<NetworkMessage>();
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
                ReferencePool.Release(message);
            }
        }
    }
    
    private void OnDisable()
    {
        // 清理引用池（可选，通常由系统自动管理）
        ReferencePool.RemoveAll<NetworkMessage>();
    }
}
```

### 5.4 游戏事件系统

```csharp
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
        var damageEvent = ReferencePool.Acquire<PlayerDamageEvent>();
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
            ReferencePool.Release(gameEvent);
        }
        m_CurrentFrameEvents.Clear();
    }
}
```

### 5.5 性能监控

```csharp
public class ReferencePoolMonitor : MonoBehaviour
{
    [SerializeField] private bool m_ShowDebugInfo = true;
    
    private void Update()
    {
        if (!m_ShowDebugInfo) return;
        
        // 获取所有引用池的统计信息
        var poolInfos = ReferencePool.GetAllReferencePoolInfos();
        
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
        var poolInfos = ReferencePool.GetAllReferencePoolInfos();
        
        foreach (var info in poolInfos)
        {
            // 如果闲置对象过多，移除一部分
            if (info.UnusedReferenceCount > 50)
            {
                var removeCount = info.UnusedReferenceCount - 20;
                ReferencePool.Remove(info.Type, removeCount);
                Debug.Log($"优化 {info.Type.Name} 引用池，移除 {removeCount} 个闲置对象");
            }
        }
    }
}
```

### 5.6 使用 using 语句管理生命周期

```csharp
/// <summary>
/// 包装器类，支持 using 语句自动释放
/// </summary>
public class PooledObject<T> : IDisposable where T : class, IReference, new()
{
    public T Value { get; private set; }
    
    public PooledObject()
    {
        Value = ReferencePool.Acquire<T>();
    }
    
    public void Dispose()
    {
        if (Value != null)
        {
            ReferencePool.Release(Value);
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
ReferencePools/
├── 
│   ├── ReferencePoolModule.cs                       # 引用池管理模块
│   ├── ReferencePool.cs                             # 引用池静态类
│   ├── ReferencePool.ReferenceCollection.cs         # 引用集合实现
│   ├── IReference.cs                                # 引用接口
│   ├── ReferencePoolInfo.cs                         # 引用池信息结构体
│   └── EReferenceStrictCheckType.cs                 # 严格检查类型枚举
└── README.md                                        # 本文档
```

## 7. 依赖

| 模块 | 说明 |
|------|------|
| Hotfix.Framework.Core | 提供 ModuleBase 基类、FuException、FuLogger |

## 8. 最佳实践

### 8.1 实现 IReference 规范

```csharp
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
public class ObjectPoolManager : MonoBehaviour
{
    [Header("预分配配置")]
    [SerializeField] private int m_PreAllocateCount = 20;
    
    private void Start()
    {
        // 在游戏启动时预分配常用对象
        ReferencePool.Add<NetworkMessage>(m_PreAllocateCount);
        ReferencePool.Add<GameEvent>(m_PreAllocateCount);
        ReferencePool.Add<RedDotNode>(m_PreAllocateCount);
    }
}
```

### 8.3 注意事项

1. **必须实现 Clear 方法**：所有实现 `IReference` 的类必须正确实现 `Clear()` 方法，重置所有字段状态
2. **避免重复释放**：同一对象不能多次释放，否则会抛出异常
3. **线程安全**：引用池操作是线程安全的，但获取的对象本身不是线程安全的
4. **引用类型限制**：引用池只适用于类（class），不适用于结构体（struct）
5. **构造函数要求**：类型必须有默认构造函数（`new()` 约束）
6. **严格检查性能**：启用严格检查会影响性能，建议仅在编辑器或开发模式启用
7. **及时释放**：使用完毕后应及时释放对象，避免长时间占用
8. **避免持有引用**：不要在对象释放后继续持有或使用该对象
