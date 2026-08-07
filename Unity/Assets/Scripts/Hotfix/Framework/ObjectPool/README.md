# FuFramework ObjectPool Module

## 1. 简介

FuFramework ObjectPool 模块是游戏框架的对象池管理系统，用于管理 `ObjectBase` 包装的 Unity 场景资源的创建、销毁和复用。资源本体 `Target` 可为任意对象（如实体实例的 GameObject、UI 窗口的 WinBase 等）；对 GameObject 类资源，可显著减少实例化(Instantiate)和销毁(Destroy)的开销，降低 GC 压力，提高游戏性能。纯 C# 数据对象请使用引用池（ReferencePool）。

## 2. 核心特性

- **对象复用机制**：减少频繁的对象创建和销毁，降低 GC 压力
- **多类型支持**：支持任意继承自 ObjectBase 的对象类型
- **智能销毁策略**：基于优先级和最后使用时间的自动销毁机制
- **内存管理**：过期后/低内存自动销毁和手动销毁控制
- **生命周期管理**：完整的对象生成、回收、销毁生命周期
- **多池管理**：支持多个对象池并行管理

## 3. 核心概念

### 3.1 类继承与实现体系

```
【类继承体系】
ObjectPoolBase (对象池基类)
    └── ObjectPool<T> (泛型对象池实现)

ObjectBase (对象基类)
    └── 用户自定义对象类 (如 BulletObject, EnemyObject)

【接口实现体系】
ObjectBase 实现:
    └── IReference (引用池接口)
        └── 方法: Clear()

【销毁对象筛选函数】
DisposeObjectFilterCallback<T>
    └── 签名: List<T> Filter(List<T> candidateObjects, int toDisposeCount, DateTime? expireTimeThreshold)
    └── 用途: 自定义对象销毁筛选策略

【数据结构】
ObjectInfo (结构体)
    ├── 属性: Name, Locked, CustomCanDisposeFlag, Priority, LastUseTime, SpawnCount, IsInUse
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
┌─────────┐     获取 (Get)      ┌─────────┐
│  空闲   │ ───────────────────▶ │ 使用中  │
│ (In Pool)│                     │ (In Use)│
└─────────┘ ◀─────────────────── └─────────┘
    ▲         回收 (Recycle)
    │
    │ 销毁 (Dispose)
    ▼
┌─────────┐
│  销毁   │
│(Destroy)│
└─────────┘

【生命周期回调】

ObjectBase 生命周期:
    ├── OnSpawn()   - 对象被获取时调用
    ├── OnRecycle() - 对象被回收时调用
    └── OnDispose() - 对象被销毁时调用（抽象方法，必须实现）
```

### 3.4 销毁策略

```
【默认销毁筛选策略】

1. 过期对象优先销毁
   - 检查对象最后使用时间
   - 超过 ExpireTime 的对象优先销毁

2. 优先级排序销毁
   - 优先级低的对象先销毁
   - 优先级相同，最后使用时间早的先销毁

【销毁条件检查】

对象可被销毁的条件:
    ├── !IsInUse        - 对象不在使用中 (SpawnCount == 0)
    ├── !Locked         - 对象未被加锁
    └── CustomCanDisposeFlag - 自定义销毁标记为 true
```

## 4. 核心类详细说明

### 4.1 ObjectPoolModule

对象池管理模块，继承自 `ModuleBase`，通过 `GlobalModule.ObjectPoolModule` 提供全局的对象池创建、获取和销毁功能。

**核心功能：**

```csharp
public sealed partial class ObjectPoolModule : ModuleBase
{
    // 对象池管理
    public int Count { get; }                                           // 对象池数量

    // 查询对象池（泛型，必须指定池名）
    public bool HasObjectPool<T>(string poolName) where T : ObjectBase     // 检查指定名称对象池
    public ObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase // 获取指定名称对象池
    public ObjectPoolBase[] GetAllObjectPools(bool sort = false)           // 获取所有对象池（按优先级排序可选）
    public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results) // 获取所有对象池（填充到列表）

    // 创建对象池（3 个泛型重载，池必须命名）
    public ObjectPool<T> CreateObjectPool<T>(string poolName, bool allowSpawnInUse = false) where T : ObjectBase
    public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
    public ObjectPool<T> CreateObjectPool<T>(string poolName, float autoDisposeCheckInterval, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase

    // 销毁对象池
    public bool DisposeObjectPool<T>(string poolName) where T : ObjectBase          // 销毁指定名称对象池
    public bool DisposeObjectPool<T>(ObjectPool<T> objectPool) where T : ObjectBase // 销毁指定对象池实例
    public bool DisposeObjectPool(ObjectPoolBase objectPool)                        // 销毁指定对象池基类实例

    // 模块级销毁
    public void DisposeOverCapacity() // 销毁所有对象池中超过容量的可销毁对象
    public void DisposeAllUnused()    // 销毁所有对象池中的所有未使用对象
}
```

**默认值说明：**

| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `capacity` | `int.MaxValue` | 对象池容量，默认不限制 |
| `autoDisposeCheckInterval` | `float.MaxValue` | 自动销毁检查间隔（秒），默认 `float.MaxValue` 即**默认不自动销毁** |
| `expireTime` | `float.MaxValue` | 对象过期时间（秒），默认 `float.MaxValue` 即**默认不过期** |
| `priority` | `0` | 对象池优先级，低优先级对象池优先被销毁 |
| `allowSpawnInUse` | `false` | 是否允许对象在使用中再次被获取 |

> 注意：`autoDisposeCheckInterval` 与 `expireTime` 是相互独立的两组配置。
> `autoDisposeCheckInterval` 控制模块轮询时是否触发“检查并销毁”动作（默认不自动检查）；
> `expireTime` 控制单个对象闲置多久后会被视为过期、纳入销毁候选（默认不过期）。
> 需要自动销毁时，两者需按业务场景配合设置。

**命名规则（硬性要求）：**

> 对象池与池内对象**必须命名**：
> - 对象池：`CreateObjectPool<T>(poolName, ...)` 必须提供非空的 `poolName`，用于区分同类型的多个池
> - 池内对象：`ObjectBase.Initialize(name, ...)` 必须提供非空的对象名，用于 `Get(name)` 按名获取
> - 空名在创建/注册时会抛出 `InvalidOperationException`，不允许存在无名池或无名对象

**访问入口：**

```csharp
// 所有操作统一通过 GlobalModule.ObjectPoolModule 访问
GlobalModule.ObjectPoolModule.CreateObjectPool<T>("池名称");
GlobalModule.ObjectPoolModule.GetObjectPool<T>("池名称");
```

**内存管理：**

- 监听 `Application.lowMemory` 事件
- 低内存时自动调用 `DisposeAllUnused()` 销毁资源

### 4.2 ObjectPoolBase

对象池基类，定义对象池的基本属性和抽象方法。

**核心属性：**

```csharp
public abstract class ObjectPoolBase
{
    public string Name { get; }                    // 对象池名称
    public string FullName { get; }                // 完整名称（类型+名称）

    // 抽象属性
    public abstract Type ObjectType { get; }       // 对象类型
    public abstract int Count { get; }             // 对象数量
    public abstract int CanDisposeCount { get; }   // 可销毁对象数量
    public abstract bool AllowSpawnInUse { get; }  // 是否允许多次获取
    public abstract float AutoDisposeCheckInterval { get; set; }  // 自动销毁检查间隔（秒）
    public abstract int Capacity { get; set; }     // 容量
    public abstract float ExpireTime { get; set; } // 过期时间（秒）
    public abstract int Priority { get; set; }     // 优先级
}
```

**抽象方法：**

```csharp
internal abstract void Update(float unscaledDeltaTime) // 轮询更新（自动销毁检查）
public abstract void DisposeOverCapacity()             // 销毁超过容量的可销毁对象
internal abstract void OnDispose()                     // 关闭并清理对象池
public abstract void DisposeAllUnused()                // 销毁所有未使用对象
public abstract ObjectInfo[] GetAllObjectInfos()       // 获取所有对象信息
```

> `Update`、`OnDispose` 为 `internal`，仅供模块内部调用。

### 4.3 ObjectPool<T>

泛型对象池实现，继承自 `ObjectPoolBase`。

**核心功能：**

```csharp
public sealed partial class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
{
    // 对象管理
    public void Register(T obj, bool inUse)         // 注册对象到池
    public T Spawn(string name)                     // 获取（生成）对象
    public void Recycle(T obj)                      // 回收对象
    public void Recycle(object target)              // 通过目标对象回收
    public bool CanSpawn(string name)               // 检查指定名称对象是否可获取

    // 销毁控制
    public bool DisposeObject(T obj)                // 销毁指定对象
    public bool DisposeObject(object target)        // 通过目标对象销毁
    public override void DisposeOverCapacity()      // 销毁超过容量的对象
    public void DisposeOverCapacity(DisposeObjectFilterCallback<T> callback)         // 使用自定义筛选函数销毁超容量对象
    public void Dispose(int toDisposeCount, DisposeObjectFilterCallback<T> callback) // 尝试销毁指定数量
    public override void DisposeAllUnused()         // 销毁所有未使用对象

    // 对象属性控制
    public void SetLocked(T obj, bool locked)       // 设置锁定状态
    public void SetLocked(object target, bool locked)
    public void SetPriority(T obj, int priority)    // 设置优先级
    public void SetPriority(object target, int priority)
    public override ObjectInfo[] GetAllObjectInfos() // 获取所有对象信息
}
```

**`Register` 的 `inUse` 参数说明：**

`Register(T obj, bool inUse)` 把对象放进池里，`inUse` 决定它注册后是"空闲待命"还是"已在用"：

| `inUse` | 含义 | 注册后状态 | 适用场景 |
|---|---|---|---|
| `false` | 放进池里当**库存** | `SpawnCount = 0`，空闲待命，等 `Spawn` 来取 | **预加载**：提前造好一批备用 |
| `true` | 放进池里**已经被人用了** | `SpawnCount = 1`，触发 `OnSpawn()`，调用方可直接使用 | **现造现用**：`Spawn` 没取到时空新建一个 |

```csharp
// 预加载（inUse = false）：造好放池里当库存
for (int i = 0; i < 10; i++)
    m_BulletPool.Register(BulletObject.Create($"Bullet_{i}", prefab), false);

// 现造现用（inUse = true）：Spawn 取不到 → 新建并标记为在用
var bullet = m_BulletPool.Spawn("Bullet");
if (bullet == null)
{
    bullet = BulletObject.Create($"Bullet_{Time.time}", prefab);
    m_BulletPool.Register(bullet, true);
}
```

> 注意：`inUse = true` 注册的对象 `SpawnCount > 0`（使用中），**不会被再次 `Spawn` 取走、也不会被自动销毁**。用完后必须 `Recycle` 一次（计数归零）它才回到空闲状态。预加载误用 `true` 会导致池里永远没有空闲对象。

**内部数据结构：**

- `FuMultiDictionary<string, T>` - 按名称存储对象（多值字典，支持同名多对象）
- `Dictionary<object, T>` - 按目标对象快速查找

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
    public virtual bool CustomCanDisposeFlag => true;   // 自定义销毁标记
    public int SpawnCount { get; private set; }       // 获取计数（引用计数）
    public bool IsInUse => SpawnCount > 0;            // 是否正在使用
}
```

**生命周期方法：**

```csharp
// 初始化方法（必须提供非空名称）
protected void Initialize(string name, object target)
protected void Initialize(string name, object target, bool locked)
protected void Initialize(string name, object target, int priority)

// 生命周期回调
protected virtual void OnSpawn() { }              // 对象被获取时
protected virtual void OnRecycle() { }            // 对象被回收时
protected internal abstract void OnDispose();     // 对象被销毁时

// 引用池接口
public virtual void Clear()                        // 清理对象
```

### 4.5 ObjectInfo

对象信息结构体，用于外部获取对象信息，如 Inspector 面板展示。

```csharp
public readonly struct ObjectInfo
{
    public string Name { get; }                    // 对象名称
    public bool Locked { get; }                    // 是否被加锁
    public bool CustomCanDisposeFlag { get; }      // 自定义销毁标记
    public int Priority { get; }                   // 优先级
    public DateTime LastUseTime { get; }           // 最后使用时间
    public int SpawnCount { get; }                 // 生成计数
    public bool IsInUse => SpawnCount > 0;         // 是否正在使用
}
```

### 4.6 DisposeObjectFilterCallback<T>

销毁对象筛选委托，用于自定义销毁策略。

```csharp
public delegate List<T> DisposeObjectFilterCallback<T>(
    List<T> candidateObjects,      // 候选对象列表
    int toDisposeCount,            // 需要销毁的数量
    DateTime? expireTimeThreshold  // 过期时间阈值（为空表示不限制）
) where T : ObjectBase;
```

## 5. 使用示例

### 5.1 定义自定义对象类

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.ObjectPool;
using Hotfix.Framework.ReferencePool;
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
        var bulletObject = GlobalModule.ReferencePoolModule.Acquire<BulletObject>();
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
    /// 销毁对象（销毁 GameObject）
    /// </summary>
    protected internal override void OnDispose()
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
using Hotfix.Framework.Core;
using Hotfix.Framework.ObjectPool;

public class BulletManager
{
    private ObjectPool<BulletObject> m_BulletPool;
    private GameObject m_BulletPrefab;

    public void Init()
    {
        // 获取对象池模块
        var objectPoolModule = GlobalModule.ObjectPoolModule;

        // 创建子弹对象池
        m_BulletPool = objectPoolModule.CreateObjectPool<BulletObject>(
            poolName: "BulletPool",
            autoDisposeCheckInterval: 10f,         // 每10秒检查一次自动销毁
            capacity: 50,                     // 最大容量50个
            expireTime: 60f,                  // 60秒未使用则过期
            priority: 1,                      // 优先级1
            allowSpawnInUse: false            // 一个子弹只能被一个使用者持有
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
        m_BulletPool.DisposeAllUnused();
    }
}
```

### 5.3 使用引用计数模式（AllowSpawnInUse = true）

> **适用场景**：一个对象被多个独立持有者**同时共享引用**，且要等**全部释放**才销毁。
> **典型例子**：塔防中多座塔锁定同一个敌人——"被锁定"标记是同一个对象，
> 任何一座塔用完都不该销毁标记（否则破坏其他塔的瞄准），只有**所有塔都释放**标记才消失。

```csharp
// 创建"锁定标记"池（allowSpawnInUse: true = 多座塔共享同一个标记）
var lockPool = objectPoolModule.CreateObjectPool<TargetLockObject>(
    poolName: "TargetLockPool",
    allowSpawnInUse: true
);

// 三座塔同时锁定同一个敌人——拿到的都是同一个标记对象
var lockA = lockPool.Spawn("Lock");   // SpawnCount = 1（塔A持有）
var lockB = lockPool.Spawn("Lock");   // SpawnCount = 2（塔B也持有，同一个对象）
var lockC = lockPool.Spawn("Lock");   // SpawnCount = 3（塔C也持有，同一个对象）
// 敌人身上只显示一个"被锁定"标记，计数为 3

// 塔A被打掉/换目标：释放自己的锁定
lockPool.Recycle(lockA);   // SpawnCount = 2（标记不消失，塔B、塔C还在锁）

lockPool.Recycle(lockB);   // SpawnCount = 1

lockPool.Recycle(lockC);   // SpawnCount = 0 → 所有塔都释放，标记才消失并回收
```

> **为什么必须用引用计数**：`false` 模式下三座塔 `Spawn` 会拿到**三个不同**的标记；
> `true` 保证共享**同一个**，用 `SpawnCount` 记录"还有几座塔在锁"，归零才销毁。
> 注意：`Spawn` 与 `Recycle` 必须严格配对（拿几次还几次），多还会抛异常、少会内存泄漏。

### 5.4 自定义销毁策略

```csharp
// 定义自定义销毁策略（优先销毁低优先级对象）
DisposeObjectFilterCallback<BulletObject> customFilter = (candidates, count, expireThreshold) =>
{
    // 按优先级排序（低优先级在前）
    candidates.Sort((a, b) => a.Priority.CompareTo(b.Priority));

    // 返回需要销毁的对象
    return candidates.GetRange(0, Mathf.Min(count, candidates.Count));
};

// 使用自定义策略销毁对象
m_BulletPool.Dispose(customFilter);
```

### 5.5 对象池监控和管理

```csharp
// 获取对象池统计信息
Debug.Log($"对象池数量: {m_BulletPool.Count}");
Debug.Log($"可销毁数量: {m_BulletPool.CanDisposeCount}");
Debug.Log($"容量: {m_BulletPool.Capacity}");

// 获取所有对象信息
ObjectInfo[] infos = m_BulletPool.GetAllObjectInfos();
foreach (var info in infos)
{
    Debug.Log($"对象: {info.Name}, 使用中: {info.IsInUse}, 锁定: {info.Locked}");
}

// 锁定重要对象（防止被销毁）
var importantBullet = m_BulletPool.Spawn("ImportantBullet");
m_BulletPool.SetLocked(importantBullet, true);

// 设置对象优先级
m_BulletPool.SetPriority(importantBullet, 100);

// 手动触发销毁
m_BulletPool.DisposeOverCapacity();  // 销毁超过容量的对象
m_BulletPool.DisposeAllUnused();  // 销毁所有未使用对象

// 尝试销毁指定数量（需提供筛选函数，这里简单取前 N 个）
m_BulletPool.Dispose(5, (candidates, count, expireThreshold) =>
    candidates.GetRange(0, Mathf.Min(count, candidates.Count)));
```

## 6. 目录结构

```
ObjectPool/
├── Base/
│   ├── ObjectBase.cs                       # 对象基类
│   └── ObjectPoolBase.cs                   # 对象池基类
├── Misc/
│   ├── ObjectInfo.cs                       # 对象信息结构体
│   └── DisposeObjectFilterCallback.cs      # 销毁筛选委托
├── Pool/
│   ├── ObjectPool.cs                       # 泛型对象池定义（字段/属性/构造/生命周期）
│   ├── ObjectPool.Manage.cs                # 对象存取与管理
│   └── ObjectPool.Dispose.cs               # 对象销毁与筛选
├── ObjectPoolModule.cs                     # 对象池管理模块（生命周期、低内存、内部方法）
├── ObjectPoolModule.API.cs                 # 对象池管理模块公共 API（创建/查询/销毁）
└── README.md                               # 本文档
```

## 7. 依赖

| 模块                        | 说明                                               |
| ------------------------- | ------------------------------------------------ |
| Hotfix.Framework.Core          | 提供 ModuleBase 基类、TypeNamePair、FuLogger |
| Hotfix.Framework.ReferencePool | 提供 IReference 接口，通过 `GlobalModule.ReferencePoolModule` 访问引用池 |

> 使用对象池前必须先注册 `ObjectPoolModule`（`HotfixLauncher.RegisterBaseModules()` 已保证），通过 `GlobalModule.ObjectPoolModule` 访问。

## 8. 最佳实践

### 8.1 对象池设计规范

```csharp
using Hotfix.Framework.Core;

// 1. 使用引用池模块创建对象（避免 GC）
var obj = GlobalModule.ReferencePoolModule.Acquire<MyObject>();

// 2. 在 OnDispose 中销毁 GameObject，在 Clear 中清理引用
protected internal override void OnDispose()
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
        s_Module = GlobalModule.ObjectPoolModule;
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
            autoDisposeCheckInterval: 10f,
            capacity: capacity,
            expireTime: 60f,
            priority: 0,
            allowSpawnInUse: false
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
4. **自动销毁检查间隔**：默认不自动销毁（`float.MaxValue`），需要自动销毁时显式设置 `autoDisposeCheckInterval`
5. **锁定机制**：重要对象使用 `SetLocked` 防止被自动销毁
6. **引用计数**：多实例池注意正确回收，避免内存泄漏
7. **线程安全**：对象池操作应在主线程进行

## 9. 与引用池（ReferencePool）的差异

对象池与引用池是两个**目的不同**的池化系统，边界区分如下。

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
| 存储结构 | `Dictionary<Type, ReferenceCollection>` + `Stack<IReference>` | `FuMultiDictionary` + `Dictionary<object, T>` |
| 使用状态 | 无（Recycle 即回池） | `SpawnCount`/`IsInUse`、`Locked`、`Priority`、`LastUseTime` |
| 生命周期管理 | 仅 OnDispose 清空 | 容量、过期时间、自动销毁、优先级、锁定 |
| 重复检测 | 无条件 `Stack.Contains` 检测 | Recycle 检测 `SpawnCount <= 0` |
| 对象接口 | `IReference`（`Clear()`） | `ObjectBase`（`OnSpawn`/`OnRecycle`/`OnDispose`） |

### 9.3 使用方式差异

```csharp
// 引用池：Acquire 获取（池空自建）、Recycle 回收
var msg = GlobalModule.ReferencePoolModule.Acquire<NetworkMessage>();
GlobalModule.ReferencePoolModule.Recycle(msg);

// 对象池：Register 注册、Get 获取（池空返回 null）、Recycle 回收
pool.Register(entityInstanceObject, true);
var obj = pool.Spawn("EntityName");
pool.Recycle(obj);
```

### 9.4 判断标准

1. **对象是"数据"还是"实体"**：纯 C# 类 → 引用池；GameObject → 对象池
2. **创建成本**：`new` 便宜但频繁 → 引用池（省 GC）；`Instantiate` 昂贵 → 对象池（省实例化）
3. **是否需要使用状态**：需要计数/锁定/过期 → 对象池；不需要 → 引用池

### 9.5 为什么对象池不直接 new

引用池对象是无参可造的纯数据（`new T()`）；对象池对象创建需要外部资源上下文（预制体/资源句柄/Helper），对象池不持有这些信息。因此对象由创建方模块（如 EntityModule/UIModule）实例化后 `Register` 进池，对象池只负责**复用真实场景对象**。
