# FuFramework ObjectPool Module

## 概述

ObjectPool 模块是 FuFramework 的对象池管理系统，专门用于管理 Unity 游戏对象的创建、销毁和复用，目标是减少实例化(Instantiate)和销毁(Destroy)的开销，提高游戏性能。

### 核心特性

- **对象复用机制**：减少频繁的对象创建和销毁，降低GC压力
- **多类型支持**：支持任意继承自 ObjectBase 的对象类型
- **智能释放策略**：基于优先级和最后使用时间的自动释放机制
- **内存管理**：低内存自动释放和手动释放控制
- **生命周期管理**：完整的对象生成、回收、释放生命周期
- **多池管理**：支持多个对象池并行管理
- **线程安全**：安全的对象池操作

## 核心类说明

### ObjectPoolManager

对象池管理器，负责管理所有对象池和对象生命周期。

```csharp
public sealed partial class ObjectPoolManager : FuModule
```

**主要功能：**
- 对象池的创建、销毁和管理
- 对象池的轮询和自动释放
- 低内存自动清理
- 对象池统计信息获取

### ObjectPoolBase

对象池基类，定义对象池的基本操作接口。

```csharp
public abstract class ObjectPoolBase
```

**主要属性：**
- `string Name` - 对象池名称
- `Type ObjectType` - 对象池中的对象类型
- `int Count` - 对象池中对象的数量
- `int CanReleaseCount` - 可释放的对象数量
- `bool AllowSpawnInUse` - 是否允许获取正在使用的对象

### ObjectBase

对象池内的对象基类，实现引用对象接口。

```csharp
public abstract class ObjectBase : IReference
```

**主要属性：**
- `string Name` - 对象名称
- `object Target` - 目标真实对象（如GameObject）
- `bool Locked` - 对象是否被加锁
- `int Priority` - 对象优先级
- `DateTime LastUseTime` - 对象上次使用时间

### ObjectPool<T>

具体存放 T 类型对象的对象池实现。

```csharp
public sealed class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
```

**池类型说明：**
- **单实例池** (`AllowSpawnInUse = false`)：一个对象每次只能被取出一次
- **多实例池** (`AllowSpawnInUse = true`)：一个对象可以被同时取出多次（引用计数）

## 技术架构

### 依赖关系

```
ObjectPoolManager → ObjectPoolBase → ObjectBase
ObjectPoolManager → ReferencePool (IReference接口)
```

### 对象生命周期

1. **对象生成流程：**
   - 检查对象池中是否有可用对象
   - 如果有，从池中取出并调用 `OnSpawn()`
   - 如果没有，创建新对象并调用 `OnSpawn()`

2. **对象回收流程：**
   - 调用对象的 `OnRecycle()` 方法
   - 将对象放回对象池
   - 更新对象的使用时间

3. **自动释放流程：**
   - 定时检查过期对象
   - 根据优先级和最后使用时间排序
   - 释放符合条件的对象

### 内存管理策略

- **容量控制**：设置对象池最大容量
- **过期时间**：设置对象闲置过期时间
- **优先级管理**：根据优先级决定释放顺序
- **低内存处理**：系统低内存时自动释放资源

## 使用指南

### 1. 基础使用

#### 创建自定义对象类

```csharp
// 定义自定义对象类，继承自 ObjectBase
public class BulletObject : ObjectBase
{
    private GameObject m_BulletGameObject;
    
    // 必须实现的构造函数
    public BulletObject()
    {
        // 可以在这里初始化默认值
    }
    
    // 创建对象实例
    public static BulletObject Create(string name, GameObject bulletPrefab)
    {
        var bulletObject = ReferencePool.Acquire<BulletObject>();
        var bulletInstance = GameObject.Instantiate(bulletPrefab);
        bulletObject.Initialize(name, bulletInstance);
        return bulletObject;
    }
    
    // 对象生成时的回调
    protected internal override void OnSpawn()
    {
        base.OnSpawn();
        if (Target is GameObject gameObject)
        {
            gameObject.SetActive(true);
        }
    }
    
    // 对象回收时的回调
    protected internal override void OnRecycle()
    {
        base.OnRecycle();
        if (Target is GameObject gameObject)
        {
            gameObject.SetActive(false);
        }
    }
    
    // 清理对象
    public override void Clear()
    {
        base.Clear();
        if (Target is GameObject gameObject)
        {
            GameObject.Destroy(gameObject);
        }
        m_BulletGameObject = null;
    }
}
```

#### 创建对象池

```csharp
// 获取对象池管理器
var objectPoolManager = ModuleManager.GetModule<ObjectPoolManager>();

// 创建对象池（单实例池）
var bulletPool = objectPoolManager.CreateObjectPool<BulletObject>("BulletPool", false);

// 配置对象池参数
bulletPool.Capacity = 100;           // 最大容量100
bulletPool.ExpireTime = 30f;         // 30秒后过期
bulletPool.AutoReleaseInterval = 5f; // 每5秒自动释放一次
bulletPool.Priority = 1;              // 优先级1
```

#### 对象获取和回收

```csharp
// 获取对象
var bulletObject = bulletPool.Spawn("Bullet_001");
if (bulletObject != null)
{
    // 使用对象
    var bulletGameObject = bulletObject.Target as GameObject;
    // ... 设置子弹位置、速度等
}

// 回收对象
bulletPool.Recycle(bulletObject);

// 或者使用 using 语句自动回收
using (var tempBullet = bulletPool.Spawn("Bullet_002"))
{
    // 使用临时对象
    // 离开作用域时自动回收
}
```

### 2. 高级配置

#### 多实例池配置

```csharp
// 创建多实例池（允许同一个对象被多次获取）
var effectPool = objectPoolManager.CreateObjectPool<EffectObject>("EffectPool", true);

// 配置多实例池参数
effectPool.Capacity = 50;
effectPool.ExpireTime = 10f;
effectPool.Priority = 2;

// 获取多个相同对象
var effect1 = effectPool.Spawn("ExplosionEffect");
var effect2 = effectPool.Spawn("ExplosionEffect"); // 可以获取同名对象

// 分别回收
effectPool.Recycle(effect1);
effectPool.Recycle(effect2);
```

#### 对象锁定机制

```csharp
// 锁定重要对象，防止被自动释放
var importantObject = bulletPool.Spawn("ImportantBullet");
importantObject.Locked = true; // 锁定对象

// 使用对象...

// 解锁并回收
importantObject.Locked = false;
bulletPool.Recycle(importantObject);
```

#### 自定义释放策略

```csharp
// 自定义释放筛选函数
ReleaseObjectFilterCallback<BulletObject> customFilter = (candidateObjects, releaseCount, expireTime) =>
{
    // 自定义释放逻辑：优先释放低优先级且长时间未使用的对象
    var toReleaseObjects = new List<BulletObject>();
    
    foreach (var bullet in candidateObjects)
    {
        if (bullet.Priority < 5 && 
            (DateTime.UtcNow - bullet.LastUseTime).TotalSeconds > expireTime)
        {
            toReleaseObjects.Add(bullet);
            if (toReleaseObjects.Count >= releaseCount)
                break;
        }
    }
    
    return toReleaseObjects.ToArray();
};

// 设置自定义释放策略
bulletPool.SetReleaseObjectFilterCallback(customFilter);
```

### 3. 池管理操作

#### 查询对象池信息

```csharp
// 检查对象池是否存在
bool hasPool = objectPoolManager.HasObjectPool<BulletObject>("BulletPool");

// 获取对象池
var pool = objectPoolManager.GetObjectPool<BulletObject>("BulletPool");

// 获取所有对象池
var allPools = objectPoolManager.GetAllObjectPools();

// 获取特定条件的对象池
var highPriorityPools = objectPoolManager.GetObjectPools(pool => pool.Priority > 5);
```

#### 手动释放控制

```csharp
// 释放单个对象池中的可释放对象
bulletPool.Release();

// 释放指定数量的对象
bulletPool.Release(10); // 释放10个对象

// 释放所有对象池中的可释放对象
objectPoolManager.ReleaseAllUnused();

// 强制释放所有对象（包括正在使用的）
objectPoolManager.ReleaseAll();
```

#### 统计信息获取

```csharp
// 获取对象池统计信息
var poolCount = objectPoolManager.Count;
var bulletCount = bulletPool.Count;
var canReleaseCount = bulletPool.CanReleaseCount;

// 打印统计信息
Debug.Log($"总对象池数量: {poolCount}");
Debug.Log($"子弹池对象数量: {bulletCount}");
Debug.Log($"可释放子弹数量: {canReleaseCount}");
```

## 高级用法

### 1. 复杂对象管理

#### 游戏实体对象池

```csharp
// 游戏实体对象
public class EntityObject : ObjectBase
{
    public EntityComponent EntityComponent { get; private set; }
    
    public static EntityObject Create(string name, GameObject entityPrefab)
    {
        var entityObject = ReferencePool.Acquire<EntityObject>();
        var entityInstance = GameObject.Instantiate(entityPrefab);
        entityObject.Initialize(name, entityInstance);
        entityObject.EntityComponent = entityInstance.GetComponent<EntityComponent>();
        return entityObject;
    }
    
    protected internal override void OnSpawn()
    {
        base.OnSpawn();
        EntityComponent?.OnSpawn();
    }
    
    protected internal override void OnRecycle()
    {
        base.OnRecycle();
        EntityComponent?.OnRecycle();
    }
    
    public override void Clear()
    {
        if (Target is GameObject gameObject)
        {
            GameObject.Destroy(gameObject);
        }
        EntityComponent = null;
        base.Clear();
    }
}

// 实体管理器
public class EntityManager
{
    private ObjectPool<EntityObject> m_EntityPool;
    
    public void Initialize()
    {
        var objectPoolManager = ModuleManager.GetModule<ObjectPoolManager>();
        m_EntityPool = objectPoolManager.CreateObjectPool<EntityObject>("EntityPool", false);
        m_EntityPool.Capacity = 200;
        m_EntityPool.ExpireTime = 60f;
    }
    
    public EntityObject SpawnEntity(string entityName, GameObject prefab)
    {
        var entityObject = m_EntityPool.Spawn(entityName);
        if (entityObject == null)
        {
            // 池中没有对象，创建新对象
            entityObject = EntityObject.Create(entityName, prefab);
        }
        return entityObject;
    }
    
    public void RecycleEntity(EntityObject entityObject)
    {
        m_EntityPool.Recycle(entityObject);
    }
}
```

### 2. 性能优化策略

#### 预热对象池

```csharp
// 预先创建对象，避免运行时创建的开销
public void PrewarmPool<T>(ObjectPool<T> pool, int count, Func<T> createFunc) where T : ObjectBase
{
    var tempList = new List<T>();
    
    // 预先创建对象
    for (int i = 0; i < count; i++)
    {
        var obj = createFunc();
        tempList.Add(obj);
    }
    
    // 立即回收所有对象到池中
    foreach (var obj in tempList)
    {
        pool.Recycle(obj);
    }
    
    tempList.Clear();
}

// 使用预热
var bulletPrefab = Resources.Load<GameObject>("BulletPrefab");
PrewarmPool(bulletPool, 50, () => BulletObject.Create("PreWarmBullet", bulletPrefab));
```

#### 分层对象池

```csharp
// 创建不同优先级的对象池
public class HierarchicalObjectPool
{
    private Dictionary<int, ObjectPool<BulletObject>> m_PriorityPools = new();
    
    public void Initialize()
    {
        var objectPoolManager = ModuleManager.GetModule<ObjectPoolManager>();
        
        // 高优先级池（重要对象）
        var highPriorityPool = objectPoolManager.CreateObjectPool<BulletObject>("HighPriorityBullets", false);
        highPriorityPool.Priority = 10;
        highPriorityPool.ExpireTime = 300f; // 5分钟
        m_PriorityPools[10] = highPriorityPool;
        
        // 中优先级池（普通对象）
        var mediumPriorityPool = objectPoolManager.CreateObjectPool<BulletObject>("MediumPriorityBullets", false);
        mediumPriorityPool.Priority = 5;
        mediumPriorityPool.ExpireTime = 60f; // 1分钟
        m_PriorityPools[5] = mediumPriorityPool;
        
        // 低优先级池（临时对象）
        var lowPriorityPool = objectPoolManager.CreateObjectPool<BulletObject>("LowPriorityBullets", true);
        lowPriorityPool.Priority = 1;
        lowPriorityPool.ExpireTime = 10f; // 10秒
        m_PriorityPools[1] = lowPriorityPool;
    }
    
    public BulletObject SpawnBullet(int priority, string name, GameObject prefab)
    {
        if (m_PriorityPools.TryGetValue(priority, out var pool))
        {
            return pool.Spawn(name) ?? BulletObject.Create(name, prefab);
        }
        return null;
    }
}
```

### 3. 内存监控和调试

#### 内存使用监控

```csharp
// 对象池监控器
public class ObjectPoolMonitor
{
    private ObjectPoolManager m_ObjectPoolManager;
    
    public void Initialize()
    {
        m_ObjectPoolManager = ModuleManager.GetModule<ObjectPoolManager>();
    }
    
    public void LogMemoryUsage()
    {
        var allPools = m_ObjectPoolManager.GetAllObjectPools();
        
        Debug.Log("=== 对象池内存使用报告 ===");
        foreach (var pool in allPools)
        {
            Debug.Log($"池名: {pool.FullName}");
            Debug.Log($"  对象数量: {pool.Count}");
            Debug.Log($"  可释放数量: {pool.CanReleaseCount}");
            Debug.Log($"  容量: {pool.Capacity}");
            Debug.Log($"  优先级: {pool.Priority}");
        }
    }
    
    public void MonitorLowMemory()
    {
        // 订阅低内存事件
        Application.lowMemory += OnLowMemory;
    }
    
    private void OnLowMemory()
    {
        Debug.LogWarning("系统低内存，强制释放对象池资源");
        
        // 按优先级从低到高释放对象
        var pools = m_ObjectPoolManager.GetAllObjectPools(true); // 按优先级排序
        
        foreach (var pool in pools)
        {
            if (pool.Priority <= 3) // 只释放低优先级对象
            {
                var releasedCount = pool.Release();
                Debug.Log($"释放池 {pool.FullName} 中的 {releasedCount} 个对象");
            }
        }
    }
}
```

## 性能优化建议

### 1. 容量规划

```csharp
// 根据游戏阶段动态调整容量
public class DynamicCapacityManager
{
    private ObjectPool<BulletObject> m_BulletPool;
    
    public void AdjustCapacityBasedOnGameState(GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Menu:
                m_BulletPool.Capacity = 10; // 菜单阶段，少量对象
                break;
            case GameState.Normal:
                m_BulletPool.Capacity = 100; // 正常游戏阶段
                break;
            case GameState.BossBattle:
                m_BulletPool.Capacity = 300; // BOSS战，需要大量对象
                break;
        }
    }
}
```

### 2. 过期时间优化

```csharp
// 根据对象使用频率动态调整过期时间
public class SmartExpireTimeManager
{
    private Dictionary<string, float> m_ObjectUsageFrequency = new();
    
    public float GetOptimalExpireTime(string objectName)
    {
        if (m_ObjectUsageFrequency.TryGetValue(objectName, out var frequency))
        {
            // 使用频率高的对象，设置较长的过期时间
            if (frequency > 10f) return 300f; // 5分钟
            if (frequency > 5f) return 120f;  // 2分钟
            if (frequency > 1f) return 60f;   // 1分钟
        }
        
        return 30f; // 默认30秒
    }
    
    public void RecordUsage(string objectName)
    {
        // 记录对象使用频率
        if (m_ObjectUsageFrequency.ContainsKey(objectName))
        {
            m_ObjectUsageFrequency[objectName] += 1f;
        }
        else
        {
            m_ObjectUsageFrequency[objectName] = 1f;
        }
    }
}
```

### 3. 批量操作优化

```csharp
// 批量生成和回收对象
public class BatchObjectOperator
{
    private ObjectPool<BulletObject> m_BulletPool;
    
    public List<BulletObject> SpawnMultiple(string baseName, int count, GameObject prefab)
    {
        var bullets = new List<BulletObject>();
        
        for (int i = 0; i < count; i++)
        {
            var bulletName = $"{baseName}_{i}";
            var bullet = m_BulletPool.Spawn(bulletName) ?? BulletObject.Create(bulletName, prefab);
            bullets.Add(bullet);
        }
        
        return bullets;
    }
    
    public void RecycleMultiple(List<BulletObject> bullets)
    {
        foreach (var bullet in bullets)
        {
            m_BulletPool.Recycle(bullet);
        }
        bullets.Clear();
    }
}
```

## 注意事项

### 1. 对象生命周期

- 确保在 `OnSpawn()` 中正确初始化对象状态
- 在 `OnRecycle()` 中清理对象状态
- 在 `Clear()` 中释放所有资源

### 2. 线程安全

- 对象池操作主要在 Unity 主线程执行
- 多线程访问需要适当的同步机制
- 避免在对象生命周期回调中进行耗时操作

### 3. 内存管理

- 及时释放不再使用的对象池
- 监控对象池的内存使用情况
- 合理设置对象池容量和过期时间

### 4. 性能考虑

- 避免频繁创建和销毁对象池
- 使用预热机制减少运行时开销
- 根据实际使用情况优化池参数

## API 参考

### ObjectPoolManager 主要方法

| 方法 | 说明 |
|------|------|
| `CreateObjectPool` | 创建对象池 |
| `DestroyObjectPool` | 销毁对象池 |
| `GetObjectPool` | 获取对象池 |
| `HasObjectPool` | 检查对象池是否存在 |
| `GetAllObjectPools` | 获取所有对象池 |
| `ReleaseAllUnused` | 释放所有未使用的对象 |
| `ReleaseAll` | 强制释放所有对象 |

### ObjectPool<T> 主要方法

| 方法 | 说明 |
|------|------|
| `Spawn` | 获取对象 |
| `Recycle` | 回收对象 |
| `Release` | 释放可释放对象 |
| `SetReleaseObjectFilterCallback` | 设置释放筛选函数 |

### ObjectBase 主要方法

| 方法 | 说明 |
|------|------|
| `OnSpawn` | 对象生成时回调 |
| `OnRecycle` | 对象回收时回调 |
| `Clear` | 清理对象资源 |

## 示例项目

参考 FuFramework 示例项目中的对象池使用示例，了解完整的使用场景和最佳实践。

---

**注意：** 本模块需要依赖 ReferencePool 模块进行引用计数管理，请确保 ReferencePool 模块已正确初始化。