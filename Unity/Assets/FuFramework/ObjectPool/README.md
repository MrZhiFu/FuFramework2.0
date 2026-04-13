# 1. FuFramework ObjectPool Module

## 1. 简介

FuFramework ObjectPool 模块是游戏框架的对象池管理系统，专门用于管理 Unity 游戏对象的创建、销毁和复用，目标是减少实例化(Instantiate)和销毁(Destroy)的开销，降低 GC 压力，提高游戏性能。

## 2. 核心特性

- **对象复用机制**：减少频繁的对象创建和销毁，降低 GC 压力
- **多类型支持**：支持任意继承自 ObjectBase 的对象类型
- **智能释放策略**：基于优先级和最后使用时间的自动释放机制
- **内存管理**：过期后/低内存自动释放和手动释放控制
- **生命周期管理**：完整的对象生成、回收、释放生命周期
- **多池管理**：支持多个对象池并行管理

## 3. 核心概念

### 3.1 类继承与实现体系

```
【类继承体系】
ObjectPoolBase (对象池基类)
    └── ObjectPool<T> (泛型对象池实现)
    
ObjectBase (对象基类)
    └── 用户自定义对象类 (如 BulletObject, EnemyObject)
    
ObjectPoolModule.Object<T> (内部包装类)
    └── 包装 ObjectBase 对象，管理生成计数


【接口实现体系】
ObjectBase 实现:
    └── IReference (引用池接口)
        └── 方法: Clear()

ObjectPoolModule.Object<T> 实现:
    └── IReference (引用池接口)
        └── 方法: Clear()


【释放对象筛选函数】
ReleaseObjectFilterCallback<T>
    └── 签名: List<T> Filter(List<T> candidates, int count, DateTime? expireThreshold)
    └── 用途: 自定义对象释放筛选策略

【数据结构】
ObjectInfo (结构体)
    ├── 属性: Name, Locked, CustomCanReleaseFlag, Priority, LastUseTime, SpawnCount, IsInUse
    └── 用途: 对象信息展示（Inspector 面板）

TypeNamePair (Core 模块)
    └── 用途: 对象池字典的 Key（类型+名称）
```

### 3.2 对象池架构

```
┌─────────────────────────────────────────────────────────────┐
│                   ObjectPoolModule                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ ObjectPool  │  │ ObjectPool  │  │ ObjectPool  │  ...     │
│  │ <Bullet>    │  │ <Enemy>     │  │ <Effect>    │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼────────────────┼────────────────┼─────────────────┘
          │                │                │
          ▼                ▼                ▼
    ┌──────────┐     ┌──────────┐     ┌──────────┐
    │  Object  │     │  Object  │     │  Object  │
    │ <Bullet> │     │ <Enemy>  │     │ <Effect> │
    └──────────┘     └──────────┘     └──────────┘
          │                │                │
          ▼                ▼                ▼
    ┌──────────┐     ┌──────────┐     ┌──────────┐
    │ GameObject│     │ GameObject│     │ GameObject│
    │ (Bullet)  │     │ (Enemy)   │     │ (Effect)  │
    └──────────┘     └──────────┘     └──────────┘
```

### 3.3 对象生命周期

```
【对象状态流转】

外部创建完成
    │
    ▼
注册 (Register)
    │
    ▼
┌─────────┐     获取 (Spawn)     ┌─────────┐
│  空闲   │ ───────────────────▶ │ 使用中  │
│ (In Pool)│                     │ (In Use)│
└─────────┘ ◀─────────────────── └─────────┘
    ▲         回收 (Recycle)
    │
    │ 释放 (Release)
    ▼
┌─────────┐
│  销毁   │
│(Destroy)│
└─────────┘

【生命周期回调】

ObjectBase 生命周期:
    ├── OnSpawn()   - 对象被获取时调用
    ├── OnRecycle() - 对象被回收时调用
    └── OnRelease() - 对象被释放时调用（抽象方法，必须实现）
```

### 3.4 释放策略

```
【默认释放筛选策略】

1. 过期对象优先释放
   - 检查对象最后使用时间
   - 超过 ExpireTime 的对象优先释放

2. 优先级排序释放
   - 优先级低的对象先释放
   - 优先级相同，最后使用时间早的先释放

【释放条件检查】

对象可被释放的条件:
    ├── !IsInUse        - 对象不在使用中 (SpawnCount == 0)
    ├── !Locked         - 对象未被加锁
    └── CustomCanReleaseFlag - 自定义释放标记为 true
```

## 4. 核心类详细说明

### 4.1 ObjectPoolModule

对象池管理模块，继承自 `FuModule`，负责管理所有对象池。

**核心功能：**

```csharp
public sealed partial class ObjectPoolModule : FuModule
{
    // 对象池管理
    public int Count { get; }                                           // 对象池数量
    public bool HasObjectPool<T>() where T : ObjectBase                 // 检查对象池是否存在
    public bool HasObjectPool<T>(string poolName)                       // 检查指定名称对象池
    public ObjectPool<T> GetObjectPool<T>()                             // 获取对象池
    public ObjectPool<T> GetObjectPool<T>(string poolName)              // 获取指定名称对象池
    public ObjectPoolBase[] GetAllObjectPools(bool sort)                // 获取所有对象池
    
    // 对象池生命周期
    public ObjectPool<T> CreateObjectPool<T>(...)                       // 创建对象池
    public bool DestroyObjectPool<T>()                                  // 销毁对象池
    public void ReleaseAllUnused()                                      // 释放所有未使用对象
}
```

**内存管理：**

- 监听 `Application.lowMemory` 事件
- 低内存时自动调用 `ReleaseAllUnused()` 释放资源

### 4.2 ObjectPoolBase

对象池基类，定义对象池的基本属性和抽象方法。

**核心属性：**

```csharp
public abstract class ObjectPoolBase
{
    public string Name { get; }                    // 对象池名称
    public string FullName { get; }                // 完整名称（类型+名称）
    public abstract Type ObjectType { get; }       // 对象类型
    public abstract int Count { get; }             // 对象数量
    public abstract int CanReleaseCount { get; }   // 可释放对象数量
    public abstract bool AllowSpawnInUse { get; }  // 是否允许多次获取
    public abstract float AutoReleaseInterval { get; set; }  // 自动释放间隔
    public abstract int Capacity { get; set; }     // 容量
    public abstract float ExpireTime { get; set; } // 过期时间（秒）
    public abstract int Priority { get; set; }     // 优先级
}
```

**抽象方法：**

- `Update()` - 轮询更新（自动释放检查）
- `Shutdown()` - 关闭清理
- `Release()` - 释放对象
- `Release(int)` - 释放指定数量对象
- `ReleaseAllUnused()` - 释放所有未使用对象
- `GetAllObjectInfos()` - 获取所有对象信息

### 4.3 ObjectPool<T>

泛型对象池实现，继承自 `ObjectPoolBase`。

**核心功能：**

```csharp
public sealed class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
{
    // 对象管理
    public void Register(T obj, bool spawned)       // 注册对象到池
    public T Spawn(string name)                     // 获取对象
    public void Recycle(T obj)                      // 回收对象
    public void Recycle(object target)              // 通过目标对象回收
    
    // 释放控制
    public bool ReleaseObject(T obj)                // 释放指定对象
    public override void Release()                  // 释放超过容量的对象
    public override void Release(int toReleaseCount)// 释放指定数量
    public override void ReleaseAllUnused()         // 释放所有未使用
    public void Release(ReleaseObjectFilterCallback<T> callback)  // 自定义释放
    
    // 对象属性控制
    public void SetLocked(T obj, bool locked)       // 设置锁定状态
    public void SetPriority(T obj, int priority)    // 设置优先级
    public bool CanSpawn(string name)               // 检查对象是否可获取
}
```

**内部数据结构：**

- `FuMultiDictionary<string, Object<T>>` - 按名称存储对象（多值字典，支持同名多对象）
- `Dictionary<object, Object<T>>` - 按目标对象快速查找

### 4.4 ObjectBase

对象池内的对象基类，所有放入对象池的对象必须继承此类。

**核心属性：**

```csharp
public abstract class ObjectBase : IReference
{
    public string Name { get; private set; }       // 对象名称
    public object Target { get; private set; }     // 目标真实对象（如 GameObject）
    public bool Locked { get; set; }               // 是否被加锁
    public int Priority { get; set; }              // 优先级
    public DateTime LastUseTime { get; internal set; }  // 最后使用时间
    public virtual bool CustomCanReleaseFlag => true;  // 自定义释放标记
}
```

**生命周期方法：**

```csharp
// 初始化方法（多种重载）
protected void Initialize(object target)
protected void Initialize(string name, object target)
protected void Initialize(string name, object target, bool locked)
protected void Initialize(string name, object target, int priority)

// 生命周期回调
protected internal virtual void OnSpawn() { }      // 对象被获取时
protected internal virtual void OnRecycle() { }    // 对象被回收时
protected internal abstract void OnRelease(bool isShutdown);  // 对象被释放时

// 引用池接口
public virtual void Clear()                        // 清理对象
```

### 4.5 ObjectPoolModule.Object<T>

内部对象包装类，包装 `ObjectBase` 对象并管理生成计数。

**核心属性：**

```csharp
private sealed class Object<T> : IReference where T : ObjectBase
{
    public string Name { get; }           // 对象名称
    public bool Locked { get; set; }      // 是否被加锁
    public int Priority { get; set; }     // 优先级
    public bool CustomCanReleaseFlag { get; }  // 自定义释放标记
    public DateTime LastUseTime { get; }  // 最后使用时间
    public bool IsInUse => SpawnCount > 0;  // 是否正在使用
    public int SpawnCount { get; private set; }  // 生成计数（引用计数）
}
```

**核心方法：**

- `Create(T obj, bool spawned)` - 创建内部对象
- `Spawn()` - 获取对象（计数+1）
- `Recycle()` - 回收对象（计数-1）
- `Release(bool isShutdown)` - 释放对象
- `Peek()` - 查看对象

### 4.6 ObjectInfo

对象信息结构体，用于外部获取对象信息，如 Inspector 面板展示。

```csharp
public readonly struct ObjectInfo
{
    public string Name { get; }                    // 对象名称
    public bool Locked { get; }                    // 是否被加锁
    public bool CustomCanReleaseFlag { get; }      // 自定义释放标记
    public int Priority { get; }                   // 优先级
    public DateTime LastUseTime { get; }           // 最后使用时间
    public int SpawnCount { get; }                 // 生成计数
    public bool IsInUse => SpawnCount > 0;         // 是否正在使用
}
```

### 4.7 ReleaseObjectFilterCallback<T>

释放对象筛选委托，用于自定义释放策略。

```csharp
public delegate List<T> ReleaseObjectFilterCallback<T>(
    List<T> candidateObjects,      // 候选对象列表
    int toReleaseCount,            // 需要释放的数量
    DateTime? expireTimeThreshold  // 过期时间阈值
) where T : ObjectBase;
```

## 5. 使用示例

### 5.1 定义自定义对象类

```csharp
using FuFramework.ObjectPool.Runtime;
using FuFramework.ReferencePool.Runtime;
using UnityEngine;

// 定义子弹对象类
public class BulletObject : ObjectBase
{
    private GameObject m_BulletGameObject;
    private Rigidbody m_Rigidbody;
    
    /// <summary>
    /// 创建子弹对象
    /// </summary>
    public static BulletObject Create(string name, GameObject bulletPrefab)
    {
        var bulletObject = ReferencePool.Acquire<BulletObject>();
        var bulletInstance = Object.Instantiate(bulletPrefab);
        bulletInstance.name = name;
        
        bulletObject.Initialize(name, bulletInstance);
        bulletObject.m_BulletGameObject = bulletInstance;
        bulletObject.m_Rigidbody = bulletInstance.GetComponent<Rigidbody>();
        
        return bulletObject;
    }
    
    /// <summary>
    /// 对象被获取时的回调
    /// </summary>
    protected internal override void OnSpawn()
    {
        base.OnSpawn();
        if (Target is GameObject gameObject)
        {
            gameObject.SetActive(true);
        }
        
        // 重置子弹状态
        m_Rigidbody?.Sleep();
    }
    
    /// <summary>
    /// 对象被回收时的回调
    /// </summary>
    protected internal override void OnRecycle()
    {
        base.OnRecycle();
        if (Target is GameObject gameObject)
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 释放对象（销毁 GameObject）
    /// </summary>
    protected internal override void Release(bool isShutdown)
    {
        if (Target is GameObject gameObject)
        {
            Object.Destroy(gameObject);
        }
        m_BulletGameObject = null;
        m_Rigidbody = null;
    }
    
    /// <summary>
    /// 清理对象（返回引用池时）
    /// </summary>
    public override void Clear()
    {
        base.Clear();
        m_BulletGameObject = null;
        m_Rigidbody = null;
    }
    
    /// <summary>
    /// 发射子弹
    /// </summary>
    public void Fire(Vector3 position, Vector3 direction, float speed)
    {
        if (m_BulletGameObject != null)
        {
            m_BulletGameObject.transform.position = position;
            m_BulletGameObject.transform.rotation = Quaternion.LookRotation(direction);
            m_Rigidbody?.WakeUp();
            m_Rigidbody?.AddForce(direction * speed, ForceMode.Impulse);
        }
    }
}
```

### 5.2 创建和使用对象池

```csharp
using FuFramework.Core.Runtime;
using FuFramework.ObjectPool.Runtime;

public class BulletManager
{
    private ObjectPool<BulletObject> m_BulletPool;
    private GameObject m_BulletPrefab;
    
    public void Init()
    {
        // 获取对象池模块
        var objectPoolModule = ModuleManager.GetModule<ObjectPoolModule>();
        
        // 创建子弹对象池
        m_BulletPool = objectPoolModule.CreateObjectPool<BulletObject>(
            poolName: "BulletPool",
            allowSpawnInUse: false,           // 一个子弹只能被一个使用者持有
            autoReleaseInterval: 10f,         // 每10秒检查一次自动释放
            capacity: 50,                     // 最大容量50个
            expireTime: 60f,                  // 60秒未使用则过期
            priority: 1                       // 优先级1
        );
        
        // 预创建一些子弹对象
        for (int i = 0; i < 10; i++)
        {
            var bullet = BulletObject.Create($"Bullet_{i}", m_BulletPrefab);
            m_BulletPool.Register(bullet, false);  // 注册到池，不允许在使用中再次获取
        }
    }
    
    /// <summary>
    /// 发射子弹
    /// </summary>
    public void FireBullet(Vector3 position, Vector3 direction)
    {
        // 尝试从池中获取子弹
        var bullet = m_BulletPool.Spawn("Bullet");
        
        if (bullet == null)
        {
            // 池中没有可用子弹，创建新的
            bullet = BulletObject.Create($"Bullet_{Time.time}", m_BulletPrefab);
            m_BulletPool.Register(bullet, true);  // 注册并标记为已生成
        }
        
        // 发射子弹
        bullet.Fire(position, direction, 100f);
    }
    
    /// <summary>
    /// 回收子弹
    /// </summary>
    public void RecycleBullet(BulletObject bullet)
    {
        m_BulletPool.Recycle(bullet);
    }
    
    /// <summary>
    /// 清理所有子弹
    /// </summary>
    public void ClearAllBullets()
    {
        m_BulletPool.ReleaseAllUnused();
    }
}
```

### 5.3 使用引用计数模式

```csharp
// 创建可在使用中再次获取的特效对象池（AllowSpawnInUse = true）
var effectPool = objectPoolModule.CreateObjectPool<EffectObject>(
    poolName: "EffectPool",
    allowSpawnInUse: true, // 可在使用中再次获取
    capacity: 20,
    expireTime: 30f
);

// 同一个特效可以被多次获取（引用计数++）
var effect1 = effectPool.Spawn("Explosion");  // SpawnCount = 1
var effect2 = effectPool.Spawn("Explosion");  // SpawnCount = 2（同一个对象）
var effect3 = effectPool.Spawn("Explosion");  // SpawnCount = 3（同一个对象）

// 每次回收引用计数--
effectPool.Recycle(effect1);  // SpawnCount = 2
effectPool.Recycle(effect2);  // SpawnCount = 1
effectPool.Recycle(effect3);  // SpawnCount = 0（可以被释放）
```

### 5.4 自定义释放策略

```csharp
// 定义自定义释放策略（优先释放低优先级对象）
ReleaseObjectFilterCallback<BulletObject> customFilter = (candidates, count, expireThreshold) =>
{
    // 按优先级排序（低优先级在前）
    candidates.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    
    // 返回需要释放的对象
    return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
};

// 使用自定义策略释放对象
m_BulletPool.Release(customFilter);
```

### 5.5 对象池监控和管理

```csharp
// 获取对象池统计信息
Debug.Log($"对象池数量: {m_BulletPool.Count}");
Debug.Log($"可释放数量: {m_BulletPool.CanReleaseCount}");
Debug.Log($"容量: {m_BulletPool.Capacity}");

// 获取所有对象信息
ObjectInfo[] infos = m_BulletPool.GetAllObjectInfos();
foreach (var info in infos)
{
    Debug.Log($"对象: {info.Name}, 使用中: {info.IsInUse}, 锁定: {info.Locked}");
}

// 锁定重要对象（防止被释放）
var importantBullet = m_BulletPool.Spawn("ImportantBullet");
m_BulletPool.SetLocked(importantBullet, true);

// 设置对象优先级
m_BulletPool.SetPriority(importantBullet, 100);

// 手动触发释放
m_BulletPool.Release();           // 释放超过容量的对象
m_BulletPool.Release(5);          // 尝试释放5个对象
m_BulletPool.ReleaseAllUnused();  // 释放所有未使用对象
```

## 6. 目录结构

```
Assets/FuFramework/ObjectPool/
├── Editor/
│   ├── FuFramework.ObjectPool.Editor.asmdef    # 编辑器程序集定义
│   └── ObjectPoolModuleInspector.cs            # 对象池模块 Inspector
├── Runtime/
│   ├── FuFramework.ObjectPool.Runtime.asmdef   # 运行时程序集定义
│   ├── ObjectPoolModule.cs                     # 对象池管理模块
│   ├── ObjectPoolModule.Object.cs              # 内部对象包装类
│   ├── ObjectPoolModule.ObjectPool.cs          # 泛型对象池实现
│   ├── Base/
│   │   ├── ObjectBase.cs                       # 对象基类
│   │   └── ObjectPoolBase.cs                   # 对象池基类
│   └── Misc/
│       ├── ObjectInfo.cs                       # 对象信息结构体
│       └── ReleaseObjectFilterCallback.cs      # 释放筛选委托
└── README.md                                   # 本文档
```

## 7. 依赖

| 模块                        | 说明                                               |
| ------------------------- | ------------------------------------------------ |
| FuFramework.Core          | 提供 FuModule 基类、TypeNamePair、FuException、FuLogger |
| FuFramework.ReferencePool | 提供 IReference 接口和 ReferencePool                  |

## 8. 最佳实践

### 8.1 对象池设计规范

```csharp
// 1. 使用 ReferencePool 创建对象（避免 GC）
var obj = ReferencePool.Acquire<MyObject>();

// 2. 在 Release 中销毁 GameObject，在 Clear 中清理引用
protected internal override void Release(bool isShutdown)
{
    if (Target is GameObject go)
        Object.Destroy(go);
}

public override void Clear()
{
    base.Clear();
    m_Component = null;  // 清理引用
}

// 3. 在 OnSpawn/OnRecycle 中控制 GameObject 显隐
protected internal override void OnSpawn()
{
    base.OnSpawn();
    (Target as GameObject)?.SetActive(true);
}

protected internal override void OnRecycle()
{
    base.OnRecycle();
    (Target as GameObject)?.SetActive(false);
}
```

### 8.2 对象池管理器封装

```csharp
public static class GameObjectPool
{
    private static ObjectPoolModule s_Module;
    
    public static void Init()
    {
        s_Module = ModuleManager.GetModule<ObjectPoolModule>();
    }
    
    /// <summary>
    /// 获取或创建对象池
    /// </summary>
    public static ObjectPool<T> GetOrCreatePool<T>(string poolName, int capacity = 50) 
        where T : ObjectBase
    {
        if (s_Module.HasObjectPool<T>(poolName))
            return s_Module.GetObjectPool<T>(poolName);
        
        return s_Module.CreateObjectPool<T>(
            poolName: poolName,
            allowSpawnInUse: false,
            autoReleaseInterval: 10f,
            capacity: capacity,
            expireTime: 60f,
            priority: 0
        );
    }
    
    /// <summary>
    /// 预加载对象
    /// </summary>
    public static void Preload<T>(string poolName, int count, Func<T> factory) 
        where T : ObjectBase
    {
        var pool = GetOrCreatePool<T>(poolName);
        for (int i = 0; i < count; i++)
        {
            var obj = factory();
            pool.Register(obj, false);
        }
    }
}
```

### 8.3 注意事项

1. **对象生命周期**：确保对象在回收后不再被使用，避免空引用异常
2. **容量设置**：合理设置容量，避免内存占用过大或频繁创建销毁
3. **过期时间**：根据对象使用频率设置合适的过期时间
4. **锁定机制**：重要对象使用 `SetLocked` 防止被自动释放
5. **引用计数**：多实例池注意正确回收，避免内存泄漏
6. **线程安全**：对象池操作应在主线程进行

