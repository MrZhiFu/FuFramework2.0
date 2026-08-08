# FuFramework Entity Module

## 1. 简介

FuFramework Entity 模块是游戏框架的实体管理系统，提供实体的创建、显示、隐藏、回收全生命周期管理。该模块采用显示层（`Entity`）与逻辑层（`EntityLogic`）分离的设计模式，配合对象池技术优化实体实例的复用效率，并支持实体间的父子依附关系。

## 2. 核心特性

- **显示逻辑分离**：`Entity` (MonoBehaviour) 负责显示，`EntityLogic` 负责逻辑，通过委托模式解耦
- **对象池管理**：实体实例通过 `EntityObject` + `ObjectPool` 实现高效复用
- **完整生命周期**：覆盖从 Unknown 到 Recycled 的 9 个状态阶段
- **实体分组**：通过 `EntityGroup` 管理组内实体和对象池
- **父子依附**：支持实体间的父子依附关系（AttachEntity / DetachEntity）
- **异步加载**：`ShowEntityAsync` 异步加载实体，不阻塞主线程
- **事件通知**：显示成功/失败、隐藏完成等事件

## 3. 核心概念

### 3.1 实体架构

```
┌─────────────────────────────────────────────────────────────┐
│                     EntityModule                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_EntityGroupDict (Dictionary<string, EntityGroup>)│   │
│  │  - 按名称管理所有实体组                              │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  m_EntityDict (Dictionary<int, EntityInfo>)         │   │
│  │  - 按编号管理所有实体信息                            │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┼─────────────┐
                ▼             ▼             ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │ Entity   │  │ Entity   │  │ Entity   │
        │ (显示层)  │  │ (显示层)  │  │ (显示层)  │
        │    ↕      │  │    ↕      │  │    ↕      │
        │EntityLogic│  │EntityLogic│  │EntityLogic│
        │ (逻辑层)  │  │ (逻辑层)  │  │ (逻辑层)  │
        └──────────┘  └──────────┘  └──────────┘
```

### 3.2 实体生命周期状态

```
Unknown → WillInit → Inited → WillShow → Showed → WillHide → Hidden → WillRecycle → Recycled
```

## 4. 核心类说明

### 4.1 EntityModule

实体管理模块，继承自 `ModuleBase`。

**静态属性：**

```csharp
EntityModule Instance { get; }   // 模块单例
```

**属性：**

```csharp
int EntityCount { get; }         // 实体数量
int EntityGroupCount { get; }    // 实体组数量
```

**实体组管理方法：**

```csharp
// 实体组查询
bool HasEntityGroup(string entityGroupName)
EntityGroup GetEntityGroup(string entityGroupName)

// 获取所有实体组
EntityGroup[] GetAllEntityGroups()
void GetAllEntityGroups(List<EntityGroup> results)

// 添加实体组
bool AddEntityGroup(EntityGroupCfg row)
```

**实体查询方法：**

```csharp
// 检查实体是否存在
bool HasEntity(int entityId)
bool HasEntity(string entityAssetName)

// 获取单个实体
Entity GetEntity(int entityId)
Entity GetEntity(string entityAssetName)

// 获取多个实体
Entity[] GetEntities(string entityAssetName)
void GetEntities(string entityAssetName, List<Entity> results)

// 获取所有已加载实体
Entity[] GetAllLoadedEntities()
void GetAllLoadedEntities(List<Entity> results)

// 获取加载中的实体编号
int[] GetAllLoadingEntityIds()
void GetAllLoadingEntityIds(List<int> results)

// 检查是否正在加载
bool IsLoadingEntity(int entityId)

// 检查实体是否有效
bool IsValidEntity(Entity entity)
```

**显示/隐藏实体方法：**

```csharp
// 显示实体（泛型）
UniTask<Entity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName)
    where T : EntityLogic

// 显示实体（Type 参数 + userData）
UniTask<Entity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName,
    string entityGroupName, object userData = null)

// 隐藏实体
void HideEntity(int entityId)
void HideEntity(int entityId, object userData)
void HideEntity(Entity entity)
void HideEntity(Entity entity, object userData)

// 隐藏所有
void HideAllLoadedEntities(object userData = null)
void HideAllLoadingEntities()
```

**父子依附方法：**

```csharp
// 附加子实体
void AttachEntity(Entity childEntity, Entity parentEntity, object userData,
    Transform parentTransform = null)
void AttachEntity(Entity childEntity, Entity parentEntity, object userData,
    string parentTransformPath = "")
void AttachEntity(int childEntityId, int parentEntityId, object userData,
    string parentTransformPath = "")
void AttachEntity(int childEntityId, int parentEntityId, object userData,
    Transform parentTransform = null)

// 解除子实体
void DetachEntity(int childEntityId)
void DetachEntity(int childEntityId, object userData)
void DetachEntity(Entity childEntity)
void DetachEntity(Entity childEntity, object userData)

// 解除所有子实体
void DetachChildEntities(int parentEntityId)
void DetachChildEntities(int parentEntityId, object userData)
void DetachChildEntities(Entity parentEntity)
void DetachChildEntities(Entity parentEntity, object userData)
```

**父实体/子实体查询：**

```csharp
Entity GetParentEntity(int childEntityId)
Entity GetParentEntity(Entity childEntity)

int GetChildEntityCount(int parentEntityId)

Entity GetChildEntity(int parentEntityId)
Entity GetChildEntity(Entity parentEntity)

Entity[] GetChildEntities(int parentEntityId)
void GetChildEntities(int parentEntityId, List<Entity> results)
Entity[] GetChildEntities(Entity parentEntity)
void GetChildEntities(Entity parentEntity, List<Entity> results)
```

### 4.2 Entity

实体显示类（MonoBehaviour），将生命周期事件委托给 `EntityLogic`。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 实体唯一编号 |
| `EntityAssetName` | `string` | 实体资源名称 |
| `EntityGroup` | `EntityGroup` | 所属实体组 |
| `Logic` | `EntityLogic` | 关联的逻辑对象 |
| `Go` | `object` | 实体 GameObject |

**生命周期方法（由 EntityModule 调用）：**

```csharp
void OnInit(int entityId, string entityAssetName, EntityGroup entityGroup,
    bool isNewEntity, ShowEntityInfoEx showEntityInfoEx)
void OnUpdate(float deltaTime, float unscaledDeltaTime)
void OnShow(ShowEntityInfoEx entityInfoEx)
void OnHide(bool isShutdown, object userData)
void OnRecycle()
void OnAttached(Entity childEntity, object userData)
void OnDetached(Entity childEntity, object userData)
void OnAttachTo(Entity parentEntity, object userData)
void OnDetachFrom(Entity parentEntity, object userData)
```

### 4.3 EntityLogic

实体逻辑基类（MonoBehaviour），开发者继承此类实现自定义实体逻辑。

**核心属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Entity` | `Entity` | 所属的 Entity 组件 |
| `Available` | `bool` | 实体是否可用 |
| `CachedTransform` | `Transform` | 缓存的 Transform |
| `Name` | `string` | 实体名称（get/set） |
| `Visible` | `bool` | 实体是否可见（set 会校验 Available） |

**生命周期方法（可重写）：**

```csharp
// 初始化
protected internal virtual void OnInit(object userData)

// 轮询
protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)

// 显示/隐藏
protected internal virtual void OnShow(object userData)
protected internal virtual void OnHide(bool isShutdown, object userData)

// 回收
protected internal virtual void OnRecycle()

// 附加/解除子实体（父实体视角）
protected internal virtual void OnAttached(EntityLogic childEntity, Transform parentTransform,
    object userData)
protected internal virtual void OnDetached(EntityLogic childEntity, object userData)

// 被附加/被解除（子实体视角）
protected internal virtual void OnAttachTo(EntityLogic parentEntity, Transform parentTransform,
    object userData)
protected internal virtual void OnDetachFrom(EntityLogic parentEntity, object userData)

// 设置可见性（内部实现）
protected virtual void InternalSetVisible(bool visible)
```

### 4.4 EntityGroup

实体组，管理组内的所有实体和实体实例对象池。

**属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 组名称 |
| `GroupGo` | `GameObject` | 组对应的 GameObject |
| `EntityCount` | `int` | 组内实体数量 |
| `PoolAutoDisposeCheckInterval` | `float` | 实例池自动销毁检查间隔 |
| `PoolCapacity` | `int` | 实例池容量 |
| `PoolExpireTimeAfterIdle` | `float` | 实例闲置超过该秒数即视为过期 |
| `PoolObjectPriority` | `int` | 实例优先级 |

**核心方法：**

```csharp
void Update(float deltaTime, float unscaledDeltaTime)

// 实体查询
bool HasEntity(int entityId)
bool HasEntity(string entityAssetName)
Entity GetEntity(int entityId)
Entity GetEntity(string entityAssetName)
Entity[] GetEntities(string entityAssetName)
void GetEntities(string entityAssetName, List<Entity> results)
Entity[] GetAllEntities()
void GetAllEntities(List<Entity> results)

// 实体管理
void AddEntity(Entity entity)
void RemoveEntity(Entity entity)

// 对象池
void RegisterEntityObject(EntityObject obj, bool spawned)
EntityObject SpawnEntityObject(string name)
void RecycleEntity(Entity entity)
void SetEntityObjectLocked(object entityGo, bool locked)
void SetEntityObjectPriority(object entityGo, int priority)
```

### 4.5 EEntityStatus

实体状态枚举。

```csharp
public enum EEntityStatus : byte
{
    Unknown,        // 未知
    WillInit,       // 将要初始化
    Inited,         // 初始化完毕
    WillShow,       // 将要显示
    Showed,         // 显示完毕
    WillHide,       // 将要隐藏
    Hidden,         // 隐藏完毕
    WillRecycle,    // 将要回收
    Recycled        // 回收完毕
}
```

### 4.6 EntityInfo

实体信息类（`IReference`），管理单个实体的状态、父子实体关系。关键属性和方法：

```csharp
Entity Entity { get; }
Entity ParentEntity { get; set; }
EEntityStatus Status { get; set; }
int ChildEntityCount { get; }

Entity GetChildEntity()
Entity[] GetChildEntities()
void GetChildEntities(List<Entity> results)
void AddChildEntity(Entity childEntity)
void RemoveChildEntity(Entity childEntity)
```

### 4.7 EntityObject

实体实例对象（继承 `ObjectBase`），包装实体资源和辅助器，用于对象池管理。

### 4.8 ShowEntityInfo / ShowEntityInfoEx

显示实体时的信息载体（`IReference`），用于在异步加载过程中传递实体参数。

- `ShowEntityInfo`：SerialId、EntityId、EntityGroup、UserData
- `ShowEntityInfoEx`：EntityLogicType、UserData

### 4.9 EntityHelper

默认实体辅助器（MonoBehaviour），负责实体的实例化、创建和释放。

```csharp
GameObject InstantiateEntity(object entityAssetHandle)
Entity CreateEntity(object entityGo, EntityGroup entityGroup)
void ReleaseEntity(object entityAssetHandle, object entityGo)
```

### 4.10 相关事件

| 事件类 | 关键属性 |
|------|------|
| `ShowEntitySuccessEventArgs` | Entity、Duration、UserData、EntityLogicType |
| `ShowEntityFailureEventArgs` | EntityId、EntityAssetName、EntityGroupName、ErrorMessage、UserData、EntityLogicType |
| `HideEntityCompleteEventArgs` | EntityId、EntityAssetName、EntityGroup、UserData |

## 5. 使用示例

### 5.1 定义实体逻辑

```csharp
using Hotfix.Framework.Entity;
using UnityEngine;

public class BulletLogic : EntityLogic
{
    private float m_Speed = 10f;
    private Vector3 m_Direction;

    protected internal override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected internal override void OnShow(object userData)
    {
        base.OnShow(userData);
        if (userData is Vector3 direction)
        {
            m_Direction = direction.normalized;
        }
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(deltaTime, unscaledDeltaTime);
        CachedTransform.position += m_Direction * m_Speed * deltaTime;

        if (Vector3.Distance(CachedTransform.position, Vector3.zero) > 50f)
        {
            EntityModule.Instance.HideEntity(Entity);
        }
    }

    protected internal override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
    }
}
```

### 5.2 显示和隐藏实体

```csharp
using Hotfix.Framework.Core;
using Hotfix.Framework.Entity;

public class BulletManager
{
    private EntityModule m_EntityModule;

    public void Init()
    {
        m_EntityModule = EntityModule.Instance;
    }

    public async UniTask<Entity> FireBullet(int entityId, Vector3 direction)
    {
        // 显示实体
        var entity = await m_EntityModule.ShowEntityAsync<BulletLogic>(
            entityId,
            "Assets/Prefabs/Bullet.prefab",
            "BulletGroup"
        );

        return entity;
    }

    public void DestroyBullet(int entityId)
    {
        m_EntityModule.HideEntity(entityId);
    }
}
```

### 5.3 实体依附

```csharp
// 将特效依附到角色上
var playerEntity = m_EntityModule.GetEntity(playerEntityId);
var effectEntity = await m_EntityModule.ShowEntityAsync<EffectLogic>(
    effectEntityId,
    "Assets/Prefabs/Effect.prefab",
    "EffectGroup"
);

// 按照 Transform 路径依附
m_EntityModule.AttachEntity(effectEntity, playerEntity, null, "Bip001/Head");

// 按照 Transform 引用依附
Transform boneTransform = playerEntity.Logic.CachedTransform.Find("Bip001/Head");
m_EntityModule.AttachEntity(effectEntity, playerEntity, null, boneTransform);

// 解除依附
m_EntityModule.DetachEntity(effectEntity);
```

### 5.4 实体组管理

```csharp
// 检查实体组
if (m_EntityModule.HasEntityGroup("BulletGroup"))
{
    var group = m_EntityModule.GetEntityGroup("BulletGroup");

    // 配置对象池参数
    group.PoolCapacity = 50;
    group.PoolExpireTimeAfterIdle = 60f;
    group.PoolObjectPriority = 0;

    // 查询组内实体
    var entities = group.GetAllEntities();
    Debug.Log($"实体组中有 {group.EntityCount} 个实体");
}
```

## 6. 目录结构

```text
Entity/
├── EntityModule.cs                       # 实体管理模块
├── Info/
│   ├── Entity.cs                         # 实体显示类 (MonoBehaviour)
│   ├── EntityGroup.cs                    # 实体组
│   ├── EntityInfo.cs                     # 实体信息
│   ├── EntityLogic.cs                    # 实体逻辑基类
│   ├── EntityObject.cs           # 实体实例对象 (对象池)
│   ├── EEntityStatus.cs                  # 实体状态枚举
│   ├── AttachEntityInfo.cs              # 附加实体信息
│   ├── ShowEntityInfo.cs                 # 显示实体信息
│   └── ShowEntityInfoEx.cs              # 显示实体额外信息
├── Event/
│   ├── ShowEntitySuccessEventArgs.cs     # 显示实体成功事件
│   ├── ShowEntityFailureEventArgs.cs     # 显示实体失败事件
│   └── HideEntityCompleteEventArgs.cs    # 隐藏实体完成事件
├── Helper/
│   └── EntityHelper.cs                   # 实体辅助器
└── README.md                             # 本文档
```

## 7. 依赖

- **Hotfix.Framework.Core**：提供 ModuleBase 基类、FuLogger
- **Hotfix.Framework.Event**：事件系统
- **Hotfix.Framework.ObjectPool**：对象池管理
- **Hotfix.Framework.ReferencePool**：引用池
- **Hotfix.Framework.Asset**：资源加载
- **Hotfix.Framework.Config**：配置表（EntityGroup 配置）
- **UniTask**：异步加载支持

## 8. 最佳实践

1. **显示逻辑分离**：`EntityLogic` 中只写逻辑代码，不要在 `Entity` 中写业务逻辑
2. **对象池预加载**：高频实体（如子弹、特效）应在启动时预创建实例
3. **实体组规划**：按实体类型和用途合理规划实体组，配置合适的对象池参数
4. **生命周期管理**：在 `OnHide` 中重置状态，在 `OnRecycle` 中清理资源，确保下次 `OnShow` 时是干净状态
5. **依附关系**：子实体依附父实体后，父实体隐藏时会自动递归隐藏所有子实体
6. **使用 CachedTransform**：在 `EntityLogic` 中优先使用 `CachedTransform` 而非 `transform`，减少 Unity 内部调用开销

## 9. 注意事项

1. `EntityLogic` 必须挂在实体 Prefab 的根节点上
2. 实体显示是异步操作（`ShowEntityAsync`），需要等待资源加载完成
3. 隐藏的实体不会立即销毁，会进入待回收队列，在下一帧通过 `OnRecycle` 回收到对象池
4. `Entity.Id` 在回收后会重置为 0
5. 不要在 `OnHide` / `OnRecycle` 中持有外部引用，可能导致内存泄漏
6. 同一 `entityId` 不能重复显示，必须等待前一个实体隐藏完成后才能重新使用该 Id
