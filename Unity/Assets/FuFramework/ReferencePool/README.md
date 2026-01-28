# FuFramework ReferencePool Module

## 概述

ReferencePool 模块是 FuFramework 中的引用池管理系统，专门用于管理纯 C# 类对象的内存分配和回收。该模块通过引用池技术减少 GC（垃圾回收）的频率，显著提高游戏运行效率，特别适合频繁创建和销毁对象的场景。

### 核心特性

- **内存优化**：减少 GC 频率，提高游戏性能
- **类型安全**：支持严格的类型检查和验证
- **统计监控**：提供详细的引用池使用统计信息
- **灵活配置**：支持多种严格检查模式
- **线程安全**：使用锁机制确保多线程环境下的安全性

## 系统架构

### 核心类说明

#### 1. IReference 接口
引用对象的基础接口，所有需要被引用池管理的类必须实现此接口。

**主要方法：**
- `Clear()`：清理引用对象的状态，准备重用

#### 2. ReferencePool 静态类
引用池的核心管理类，提供全局的引用获取、释放和管理功能。

**主要功能：**
- 管理所有类型的引用池
- 提供获取、释放、添加、移除等操作
- 统计引用池使用情况

#### 3. ReferenceCollection 内部类
特定类型的引用集合管理，每个类型对应一个 ReferenceCollection。

**主要属性：**
- `UsingReferenceCount`：正在使用的引用数量
- `UnusedReferenceCount`：闲置的引用数量
- `AcquireReferenceCount`：已获取的引用总数
- `ReleaseReferenceCount`：已释放的引用总数

#### 4. ReferencePoolManager 管理器
引用池管理器，继承自 FuModule，负责模块的生命周期管理。

**主要功能：**
- 控制严格检查模式的开关
- 管理模块的初始化和关闭

### 技术架构

```
ReferencePoolManager (管理器)
    ↓
ReferencePool (静态池)
    ↓
ReferenceCollection (类型集合)
    ↓
Queue<IReference> (对象队列)
```

## 快速开始

### 1. 实现 IReference 接口

首先，创建一个需要被引用池管理的类，并实现 IReference 接口：

```csharp
using FuFramework.ReferencePool.Runtime;

// 示例：网络消息类
public class NetworkMessage : IReference
{
    public int MessageId { get; private set; }
    public string Content { get; private set; }
    public DateTime Timestamp { get; private set; }
    
    /// <summary>
    /// 清理引用，准备重用
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

### 2. 基本使用示例

```csharp
using FuFramework.ReferencePool.Runtime;

public class ReferencePoolExample : MonoBehaviour
{
    private void Start()
    {
        // 从引用池获取消息对象
        var message = ReferencePool.Acquire<NetworkMessage>();
        
        // 初始化消息数据
        message.Initialize(1001, "Hello World");
        
        // 使用消息对象
        ProcessMessage(message);
        
        // 使用完毕后归还到引用池
        ReferencePool.Release(message);
    }
    
    private void ProcessMessage(NetworkMessage message)
    {
        Debug.Log($"处理消息: ID={message.MessageId}, 内容={message.Content}");
    }
}
```

## 详细使用指南

### 1. 引用池管理示例

#### 网络消息处理系统

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

#### 游戏事件系统

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

public class ItemPickupEvent : GameEvent
{
    public string ItemId { get; private set; }
    public int Quantity { get; private set; }
    
    public override void Clear()
    {
        base.Clear();
        ItemId = null;
        Quantity = 0;
    }
    
    public void Initialize(string source, string itemId, int quantity)
    {
        EventType = "ItemPickup";
        Source = source;
        Timestamp = Time.time;
        ItemId = itemId;
        Quantity = quantity;
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
    
    // 触发物品拾取事件
    public void TriggerItemPickupEvent(string source, string itemId, int quantity)
    {
        var pickupEvent = ReferencePool.Acquire<ItemPickupEvent>();
        pickupEvent.Initialize(source, itemId, quantity);
        m_CurrentFrameEvents.Add(pickupEvent);
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

### 2. 性能监控和调试

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
            Debug.Log($"类型: {info.TypeName}, " +
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
                Debug.Log($"优化 {info.TypeName} 引用池，移除 {removeCount} 个闲置对象");
            }
        }
    }
}
```

## 高级用法

### 1. 自定义引用池策略

```csharp
public class CustomReferencePool<T> where T : class, IReference, new()
{
    private readonly int m_MaxPoolSize;
    private readonly int m_PreAllocateCount;
    
    public CustomReferencePool(int maxPoolSize = 100, int preAllocateCount = 10)
    {
        m_MaxPoolSize = maxPoolSize;
        m_PreAllocateCount = preAllocateCount;
        
        // 预分配对象
        ReferencePool.Add<T>(m_PreAllocateCount);
    }
    
    public T Acquire()
    {
        var obj = ReferencePool.Acquire<T>();
        
        // 检查引用池大小，如果太小则自动扩容
        var poolInfo = GetPoolInfo();
        if (poolInfo.UnusedReferenceCount < 5 && poolInfo.UsingReferenceCount < m_MaxPoolSize)
        {
            ReferencePool.Add<T>(10);
        }
        
        return obj;
    }
    
    public void Release(T obj)
    {
        ReferencePool.Release(obj);
        
        // 定期清理过多的闲置对象
        var poolInfo = GetPoolInfo();
        if (poolInfo.UnusedReferenceCount > m_MaxPoolSize)
        {
            ReferencePool.Remove<T>(poolInfo.UnusedReferenceCount - m_MaxPoolSize);
        }
    }
    
    private ReferencePoolInfo GetPoolInfo()
    {
        var infos = ReferencePool.GetAllReferencePoolInfos();
        return infos.FirstOrDefault(info => info.Type == typeof(T));
    }
}

// 使用自定义引用池
public class AdvancedMessageSystem : MonoBehaviour
{
    private CustomReferencePool<NetworkMessage> m_MessagePool;
    
    private void Start()
    {
        m_MessagePool = new CustomReferencePool<NetworkMessage>(maxPoolSize: 200, preAllocateCount: 50);
    }
    
    public void ProcessMessage(byte[] data)
    {
        var message = m_MessagePool.Acquire();
        try
        {
            // 使用消息
            message.Initialize(/* 参数 */);
            HandleMessage(message);
        }
        finally
        {
            m_MessagePool.Release(message);
        }
    }
}
```

### 2. 带生命周期的对象管理

```csharp
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

// 使用 using 语句自动管理生命周期
public class SafeObjectUsage : MonoBehaviour
{
    public void ProcessData()
    {
        using (var pooledMessage = new PooledObject<NetworkMessage>())
        {
            var message = pooledMessage.Value;
            message.Initialize(1001, "Safe Message");
            
            // 使用 message
            // 退出 using 块时自动释放
        }
    }
    
    // 批量处理
    public void ProcessMultipleData(List<byte[]> dataList)
    {
        var pooledObjects = new List<PooledObject<NetworkMessage>>();
        
        try
        {
            foreach (var data in dataList)
            {
                var pooledObj = new PooledObject<NetworkMessage>();
                var message = pooledObj.Value;
                
                // 初始化消息
                message.Initialize(/* 参数 */);
                pooledObjects.Add(pooledObj);
                
                // 处理消息
                ProcessSingleMessage(message);
            }
        }
        finally
        {
            foreach (var pooledObj in pooledObjects)
            {
                pooledObj.Dispose();
            }
        }
    }
}
```

### 3. 异步引用池操作

```csharp
public class AsyncReferencePool
{
    // 异步获取对象（适用于需要等待对象可用的场景）
    public static async System.Threading.Tasks.Task<T> AcquireAsync<T>() 
        where T : class, IReference, new()
    {
        return await System.Threading.Tasks.Task.Run(() =>
        {
            return ReferencePool.Acquire<T>();
        });
    }
    
    // 批量异步获取
    public static async System.Threading.Tasks.Task<List<T>> AcquireMultipleAsync<T>(int count)
        where T : class, IReference, new()
    {
        var tasks = new System.Threading.Tasks.Task<T>[count];
        
        for (int i = 0; i < count; i++)
        {
            tasks[i] = AcquireAsync<T>();
        }
        
        var results = await System.Threading.Tasks.Task.WhenAll(tasks);
        return results.ToList();
    }
}

// 使用异步引用池
public class AsyncMessageProcessor : MonoBehaviour
{
    public async System.Threading.Tasks.Task ProcessMessagesAsync(List<byte[]> packets)
    {
        // 异步获取消息对象
        var messages = await AsyncReferencePool.AcquireMultipleAsync<NetworkMessage>(packets.Count);
        
        try
        {
            // 并行处理消息
            var processingTasks = new System.Threading.Tasks.Task[packets.Count];
            
            for (int i = 0; i < packets.Count; i++)
            {
                var message = messages[i];
                var packet = packets[i];
                
                processingTasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    // 初始化并处理消息
                    var parsedData = ParsePacket(packet);
                    message.Initialize(parsedData.MessageId, parsedData.Content);
                    ProcessMessage(message);
                });
            }
            
            await System.Threading.Tasks.Task.WhenAll(processingTasks);
        }
        finally
        {
            // 批量释放
            foreach (var message in messages)
            {
                ReferencePool.Release(message);
            }
        }
    }
}
```

## 性能优化建议

### 1. 引用池大小优化

```csharp
public class ReferencePoolOptimizer : MonoBehaviour
{
    [SerializeField] private int m_MaxPoolSize = 100;
    [SerializeField] private int m_MinPoolSize = 10;
    [SerializeField] private float m_OptimizeInterval = 30f;
    
    private float m_LastOptimizeTime;
    
    private void Update()
    {
        // 定期优化引用池
        if (Time.time - m_LastOptimizeTime >= m_OptimizeInterval)
        {
            OptimizeAllPools();
            m_LastOptimizeTime = Time.time;
        }
    }
    
    private void OptimizeAllPools()
    {
        var poolInfos = ReferencePool.GetAllReferencePoolInfos();
        
        foreach (var info in poolInfos)
        {
            OptimizePool(info.Type, info.UnusedReferenceCount);
        }
    }
    
    private void OptimizePool(Type type, int currentSize)
    {
        if (currentSize > m_MaxPoolSize)
        {
            // 移除过多的闲置对象
            var removeCount = currentSize - m_MaxPoolSize;
            ReferencePool.Remove(type, removeCount);
            Debug.Log($"优化 {type.Name} 引用池: 移除 {removeCount} 个闲置对象");
        }
        else if (currentSize < m_MinPoolSize)
        {
            // 补充不足的对象
            var addCount = m_MinPoolSize - currentSize;
            ReferencePool.Add(type, addCount);
            Debug.Log($"优化 {type.Name} 引用池: 补充 {addCount} 个对象");
        }
    }
}
```

### 2. 内存使用监控

```csharp
public class MemoryMonitor : MonoBehaviour
{
    [SerializeField] private bool m_EnableMonitoring = true;
    [SerializeField] private float m_CheckInterval = 5f;
    
    private float m_LastCheckTime;
    
    private void Update()
    {
        if (!m_EnableMonitoring) return;
        
        if (Time.time - m_LastCheckTime >= m_CheckInterval)
        {
            CheckMemoryUsage();
            m_LastCheckTime = Time.time;
        }
    }
    
    private void CheckMemoryUsage()
    {
        var poolInfos = ReferencePool.GetAllReferencePoolInfos();
        long totalMemory = 0;
        
        foreach (var info in poolInfos)
        {
            // 估算对象内存使用（根据实际情况调整）
            var estimatedSize = EstimateObjectSize(info.Type);
            var totalSize = (info.UsingReferenceCount + info.UnusedReferenceCount) * estimatedSize;
            totalMemory += totalSize;
            
            if (totalSize > 1024 * 1024) // 超过 1MB
            {
                Debug.LogWarning($"{info.TypeName} 引用池占用内存较大: {totalSize / 1024} KB");
            }
        }
        
        Debug.Log($"引用池总内存使用: {totalMemory / 1024} KB");
    }
    
    private long EstimateObjectSize(Type type)
    {
        // 简单的对象大小估算（根据实际情况实现）
        if (type == typeof(NetworkMessage)) return 100; // 估算 100 字节
        if (type == typeof(GameEvent)) return 200;      // 估算 200 字节
        return 50; // 默认估算
    }
}
```

## 注意事项

### 1. 内存管理
- **及时释放**：使用完毕后及时调用 `ReferencePool.Release()`
- **避免泄漏**：确保在异常情况下也能正确释放对象
- **合理预分配**：根据使用频率合理预分配对象数量

### 2. 性能考虑
- **严格检查**：在生产环境中考虑关闭严格检查以提高性能
- **池大小**：避免引用池过大占用过多内存
- **使用模式**：根据实际使用模式调整引用池策略

### 3. 线程安全
- **锁机制**：引用池内部使用锁确保线程安全
- **并发访问**：支持多线程环境下的并发访问
- **性能影响**：高并发场景下锁可能成为性能瓶颈

### 4. 错误处理
- **类型检查**：确保使用的类型实现了 IReference 接口
- **空值检查**：释放对象前检查是否为 null
- **重复释放**：避免重复释放同一个对象

## API 参考

### ReferencePool 静态类

#### 静态属性

##### Count
```csharp
public static int Count { get; }
```
**功能**：获取引用池的数量

##### EnableStrictCheck
```csharp
public static bool EnableStrictCheck { get; set; }
```
**功能**：获取或设置是否开启引用类型严格检查

#### 静态方法

##### Acquire<T>()
```csharp
public static T Acquire<T>() where T : class, IReference, new()
```
**功能**：从引用池获取指定类型的引用对象

**类型参数**：
- `T`：引用类型，必须实现 IReference 接口且有默认构造函数

**返回值**：
- `T`：获取到的引用对象

**示例**：
```csharp
var message = ReferencePool.Acquire<NetworkMessage>();
```

##### Release(IReference reference)
```csharp
public static void Release(IReference reference)
```
**功能**：将引用对象归还到引用池

**参数**：
- `reference` (IReference)：要归还的引用对象

**示例**：
```csharp
ReferencePool.Release(message);
```

##### Add<T>(int count)
```csharp
public static void Add<T>(int count) where T : class, IReference, new()
```
**功能**：向指定类型的引用池中追加指定数量的引用对象

**参数**：
- `count` (int)：要追加的对象数量

**示例**：
```csharp
ReferencePool.Add<NetworkMessage>(10);
```

##### RemoveAll<T>()
```csharp
public static void RemoveAll<T>() where T : class, IReference
```
**功能**：从指定类型的引用池中移除所有的引用对象

**示例**：
```csharp
ReferencePool.RemoveAll<NetworkMessage>();
```

##### GetAllReferencePoolInfos()
```csharp
public static ReferencePoolInfo[] GetAllReferencePoolInfos()
```
**功能**：获取所有引用池的统计信息

**返回值**：
- `ReferencePoolInfo[]`：引用池信息数组

**示例**：
```csharp
var infos = ReferencePool.GetAllReferencePoolInfos();
```

## 常见问题解答

### Q: 什么类型的对象适合使用引用池？
A: 频繁创建和销毁的纯 C# 类对象，如网络消息、游戏事件、临时数据结构等。

### Q: 引用池会影响性能吗？
A: 正确使用引用池可以显著提高性能，减少 GC 压力。但引用池过大或使用不当可能占用过多内存。

### Q: 如何避免引用池内存泄漏？
A: 确保每个 Acquire 都有对应的 Release 调用，使用 try-finally 或 using 语句确保释放。

### Q: 引用池支持多线程吗？
A: 支持，引用池内部使用锁机制确保线程安全。

### Q: 什么时候应该清理引用池？
A: 通常在场景切换或内存紧张时清理，但大多数情况下引用池可以自动管理。

## 总结

ReferencePool 模块为 FuFramework 提供了高效的引用池管理系统，通过减少 GC 频率显著提升游戏性能。该模块设计合理，功能完善，支持类型安全、线程安全和性能监控，是游戏开发中对象管理的理想解决方案。

通过合理使用引用池，可以显著优化频繁创建和销毁对象的场景，提升游戏的整体性能和稳定性。