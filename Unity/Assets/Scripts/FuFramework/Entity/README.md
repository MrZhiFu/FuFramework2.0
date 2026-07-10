# FuFramework Entity Module

## 1. 简介

**FuFramework Entity** 模块是一个功能强大的实体管理系统，旨在统一管理游戏中的所有动态对象（如角色、怪物、特效、道具等）。它基于"实体组（EntityGroup）"的概念，提供了实体的异步加载、实例化、生命周期管理、对象池复用以及层级管理等功能。

---

## 2. 特性

- **实体组管理**：支持按类别分组管理实体，每个组独立配置对象池参数
- **异步加载**：基于 UniTask 的异步实体加载，支持加载中取消
- **对象池复用**：内置对象池机制，自动回收和复用实体实例
- **生命周期管理**：完整的实体生命周期回调（Init、Show、Hide、Recycle、Update）
- **层级管理**：支持实体间的父子关系建立和解绑
- **状态管理**：实体状态机管理（WillInit、Inited、WillShow、Showed、WillHide、Hidden、WillRecycle、Recycled）
- **事件通知**：实体显示成功/失败、隐藏完成等事件通知

---

## 3. 核心概念

### 3.1 实体 (Entity)

实体是游戏中的基本动态对象。

- **Entity**：框架内部使用的实体包装类，继承自 `MonoBehaviour`，负责连接框架层与逻辑层
- **EntityLogic**：实体逻辑基类，开发者编写的具体业务逻辑（如 `PlayerLogic`、`MonsterLogic`）都应继承此类

### 3.2 实体组 (EntityGroup)

实体组用于对实体进行分类管理。

- 每个实体组对应一个 GameObject 根节点
- 可以为不同的实体组设置不同的对象池属性（自动释放间隔、容量、过期时间、优先级）
- 常见分组：`Player`、`Monster`、`Effect`、`UI`、`Item` 等

### 3.3 实体辅助器 (EntityHelper)

负责实体的底层实例化和销毁操作，默认实现基于 YooAsset 进行资源加载和实例化。

---

## 4. 核心类说明

### 4.1 EntityModule

实体管理模块，继承自 `ModuleBase`，是实体系统的核心管理类。

**主要功能：**
- 管理实体组的创建和销毁
- 管理实体的创建、销毁、显示、隐藏等流程
- 管理实体的生命周期
- 管理实体的资源加载和对象池

**实体组管理方法：**

```csharp
// 检查是否存在实体组
bool HasEntityGroup(string entityGroupName)

// 获取实体组
EntityGroup GetEntityGroup(string entityGroupName)

// 获取所有实体组
EntityGroup[] GetAllEntityGroups()
void GetAllEntityGroups(List<EntityGroup> results)

// 添加实体组
bool AddEntityGroup(EntityGroupInfo entityGroupSetting)
```

**实体查询方法：**

```csharp
// 检查是否存在实体
bool HasEntity(int entityId)
bool HasEntity(string entityAssetName)

// 获取实体
Entity GetEntity(int entityId)
Entity GetEntity(string entityAssetName)
Entity[] GetEntities(string entityAssetName)
void GetEntities(string entityAssetName, List<Entity> results)
Entity[] GetAllLoadedEntities()
void GetAllLoadedEntities(List<Entity> results)

// 获取正在加载的实体
int[] GetAllLoadingEntityIds()
void GetAllLoadingEntityIds(List<int> results)
bool IsLoadingEntity(int entityId)
bool IsValidEntity(Entity entity)
```

**实体显示/隐藏方法：**

```csharp
// 异步显示实体（泛型版本）
UniTask<Entity> ShowEntityAsync<T>(int entityId, string entityAssetName, string entityGroupName) where T : EntityLogic

// 异步显示实体（Type 版本）
UniTask<Entity> ShowEntityAsync(int entityId, Type entityLogicType, string entityAssetName, string entityGroupName, object userData = null)

// 隐藏实体
void HideEntity(int entityId)
void HideEntity(int entityId, object userData)
void HideEntity(Entity entity)
void HideEntity(Entity entity, object userData)

// 隐藏所有实体
void HideAllLoadedEntities()
```

**实体附加/解绑方法：**

```csharp
// 附加子实体
void AttachEntity(int childEntityId, int parentEntityId, string parentTransformPath = null, object userData = null)
void AttachEntity(Entity childEntity, int parentEntityId, string parentTransformPath = null, object userData = null)
void AttachEntity(int childEntityId, Entity parentEntity, string parentTransformPath = null, object userData = null)
void AttachEntity(Entity childEntity, Entity parentEntity, string parentTransformPath = null, object userData = null)

// 解绑子实体
void DetachEntity(int childEntityId, object userData = null)
void DetachEntity(Entity childEntity, object userData = null)
```

---

### 4.2 Entity

实体显示类，继承自 `MonoBehaviour`，是框架层与逻辑层的桥梁。

**核心属性：**

```csharp
int Id { get; }                          // 实体编号
string EntityAssetName { get; }          // 实体资源名称
EntityGroup EntityGroup { get; }         // 实体所属的实体组
EntityLogic Logic { get; }               // 实体逻辑组件
object Handle => gameObject;             // 实体实例
```

**生命周期方法：**

```csharp
// 初始化
void OnInit(int entityId, string entityAssetName, EntityGroup entityGroup, bool isNewInstance, ShowEntityInfoEx showEntityInfoEx)

// 轮询更新
void OnUpdate(float deltaTime, float unscaledDeltaTime)

// 显示
void OnShow(ShowEntityInfoEx entityInfoEx)

// 隐藏
void OnHide(bool isShutdown, object userData)

// 回收
void OnRecycle()

// 附加子实体
void OnAttached(Entity childEntity, object userData)

// 解绑子实体
void OnDetached(Entity childEntity, object userData)

// 被附加到父实体
void OnAttachTo(Entity parentEntity, object userData)

// 被从父实体解绑
void OnDetachFrom(Entity parentEntity, object userData)
```

---

### 4.3 EntityLogic

实体逻辑基类，开发者编写的具体业务逻辑应继承此类。

**核心属性：**

```csharp
Entity Entity { get; }                   // 实体
bool Available { get; }                  // 实体是否可用
Transform CachedTransform { get; }       // 缓存的 Transform
string Name { get; set; }               // 实体名称
bool Visible { get; set; }              // 实体是否可见
```

**生命周期回调（可重写）：**

```csharp
// 初始化（仅一次）
protected internal virtual void OnInit(object userData)

// 轮询更新
protected internal virtual void OnUpdate(float deltaTime, float unscaledDeltaTime)

// 显示
protected internal virtual void OnShow(object userData)

// 隐藏
protected internal virtual void OnHide(bool isShutdown, object userData)

// 回收
protected internal virtual void OnRecycle()

// 子实体附加
protected internal virtual void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)

// 子实体解绑
protected internal virtual void OnDetached(EntityLogic childEntity, object userData)

// 被附加到父实体
protected internal virtual void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)

// 被从父实体解绑
protected internal virtual void OnDetachFrom(EntityLogic parentEntity, object userData)

// 设置可见性
protected virtual void InternalSetVisible(bool visible)
```

---

### 4.4 EntityGroup

实体组，用于对实体进行分类管理。

**核心属性：**

```csharp
string Name { get; }                     // 实体组名称
GameObject GroupGo { get; }              // 实体组对应的 GameObject
int EntityCount { get; }                 // 实体组中实体数量
float InstanceAutoReleaseInterval { get; set; }  // 对象池自动释放间隔
int InstanceCapacity { get; set; }       // 对象池容量
float InstanceExpireTime { get; set; }   // 对象池对象过期时间
int InstancePriority { get; set; }       // 对象池优先级
```

**主要方法：**

```csharp
// 轮询更新
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

// 对象池操作
void RegisterEntityInstanceObject(EntityInstanceObject obj, bool spawned)
EntityInstanceObject SpawnEntityInstanceObject(string name)
void RecycleEntity(Entity entity)
void SetEntityInstanceLocked(object entityInstance, bool locked)
void SetEntityInstancePriority(object entityInstance, int priority)
```

---

### 4.5 实体状态 (EEntityStatus)

```csharp
public enum EEntityStatus : byte
{
    Unknown = 0,        // 未知
    WillInit,           // 将要初始化
    Inited,             // 初始化完毕
    WillShow,           // 将要显示
    Showed,             // 显示完毕
    WillHide,           // 将要隐藏
    Hidden,             // 隐藏完毕
    WillRecycle,        // 将要回收
    Recycled            // 回收完毕
}
```

---

### 4.6 实体信息类

**EntityInfo**：用于管理实体的状态及其子实体

```csharp
Entity Entity { get; }                   // 实体
Entity ParentEntity { get; set; }       // 父实体
EEntityStatus Status { get; set; }      // 实体状态
int ChildEntityCount { get; }            // 子实体数量

Entity GetChildEntity()                   // 获取第一个子实体
Entity[] GetChildEntities()               // 获取所有子实体
void AddChildEntity(Entity childEntity)   // 添加子实体
void RemoveChildEntity(Entity childEntity)// 移除子实体
```

**ShowEntityInfo / ShowEntityInfoEx**：显示实体时的信息传递

```csharp
// ShowEntityInfo
int SerialId { get; }                    // 实体自增编号
int EntityId { get; }                    // 实体编号
EntityGroup EntityGroup { get; }         // 实体所属组
object UserData { get; }                 // 用户数据

// ShowEntityInfoEx
Type EntityLogicType { get; }            // 实体逻辑类型
object UserData { get; }                 // 用户数据
```

**AttachEntityInfo**：附加实体时的信息传递

```csharp
Transform ParentTransform { get; }       // 父级对象
object UserData { get; }                 // 用户自定义数据
```

---

## 5. 使用示例

### 5.1 定义实体逻辑类

```csharp
using FuFramework.Entity.Runtime;
using UnityEngine;

public class PlayerLogic : EntityLogic
{
    private float m_MoveSpeed = 5f;
    private Vector3 m_TargetPosition;

    protected internal override void OnInit(object userData)
    {
        base.OnInit(userData);
        Debug.Log("Player Init");
        
        // 初始化玩家数据
        if (userData is PlayerData data)
        {
            m_MoveSpeed = data.MoveSpeed;
        }
    }

    protected internal override void OnShow(object userData)
    {
        base.OnShow(userData);
        Debug.Log("Player Show");
        
        // 设置初始位置
        transform.position = Vector3.zero;
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        base.OnUpdate(deltaTime, unscaledDeltaTime);
        
        // 移动逻辑
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                m_TargetPosition = hit.point;
            }
        }
        
        transform.position = Vector3.MoveTowards(
            transform.position, 
            m_TargetPosition, 
            m_MoveSpeed * deltaTime
        );
    }

    protected internal override void OnHide(bool isShutdown, object userData)
    {
        base.OnHide(isShutdown, userData);
        Debug.Log("Player Hide");
    }

    protected internal override void OnRecycle()
    {
        base.OnRecycle();
        Debug.Log("Player Recycle");
    }
}

[Serializable]
public class PlayerData
{
    public float MoveSpeed = 5f;
    public int Health = 100;
}
```

### 5.2 显示实体

```csharp
using FuFramework.Core.Runtime;
using FuFramework.Entity.Runtime;

public class GameManager : MonoBehaviour
{
    private EntityModule m_EntityModule;
    
    async void Start()
    {
        // 注册模块
        ModuleManager.RegisterModule<EntityModule>();
        m_EntityModule = ModuleManager.GetModule<EntityModule>();
        
        // 异步显示实体
        try
        {
            var playerData = new PlayerData { MoveSpeed = 8f, Health = 150 };
            var entity = await m_EntityModule.ShowEntityAsync<PlayerLogic>(
                entityId: 1001,                                    // 实体ID（需唯一）
                entityAssetName: "Assets/Game/Prefabs/Player.prefab", // 资源路径
                entityGroupName: "PlayerGroup",                    // 实体组名称
                userData: playerData                               // 自定义数据
            );
            
            Debug.Log($"Player entity created: {entity.Id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to show entity: {ex.Message}");
        }
    }
}
```

### 5.3 隐藏实体

```csharp
// 通过实体ID隐藏
m_EntityModule.HideEntity(1001);

// 通过实体对象隐藏
var entity = m_EntityModule.GetEntity(1001);
m_EntityModule.HideEntity(entity);

// 隐藏时传递自定义数据
m_EntityModule.HideEntity(1001, "SavePlayerState");

// 在 EntityLogic 内部调用隐藏
public class PlayerLogic : EntityLogic
{
    public void Die()
    {
        // 隐藏自己
        Entity.EntityGroup.EntityModule.HideEntity(Entity.Id);
    }
}
```

### 5.4 附加子实体

```csharp
// 示例：将武器附加到角色的右手
public async void EquipWeapon(int playerId)
{
    // 1. 先显示武器实体
    var weaponEntity = await m_EntityModule.ShowEntityAsync<WeaponLogic>(
        entityId: 2001,
        entityAssetName: "Assets/Game/Prefabs/Weapon.prefab",
        entityGroupName: "WeaponGroup"
    );
    
    // 2. 将武器附加到角色的右手节点
    m_EntityModule.AttachEntity(
        childEntityId: 2001,           // 子实体ID（武器）
        parentEntityId: playerId,      // 父实体ID（角色）
        parentTransformPath: "Bip001/Bip001 Spine/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand", // 父节点路径
        userData: null
    );
}

// 解绑武器
public void UnequipWeapon(int weaponId)
{
    m_EntityModule.DetachEntity(weaponId);
}
```

### 5.5 实体组配置

实体组的配置位于 `ModuleSetting` 的 `EntitySetting` 中，需在编辑器中预先配置：

```csharp
// EntitySetting 配置示例
[Serializable]
public class EntityGroupInfo
{
    public string Name;                      // 实体组名称
    public int InstanceCapacity = 100;       // 对象池容量
    public float InstanceExpireTime = 60f;   // 对象过期时间（秒）
    public float InstanceAutoReleaseInterval = 60f; // 自动释放间隔（秒）
    public int InstancePriority = 0;         // 对象池优先级
}
```

---

## 6. 事件系统

实体模块提供了以下事件通知：

### 6.1 显示实体成功事件

```csharp
public class ShowEntitySuccessEventArgs : GameEventArgs
{
    public int EntityId { get; }
    public string EntityAssetName { get; }
    public EntityGroup EntityGroup { get; }
    public Entity Entity { get; }
    public float Duration { get; }
    public object UserData { get; }
}

// 订阅事件
EventManager.Subscribe<ShowEntitySuccessEventArgs>(OnShowEntitySuccess);

void OnShowEntitySuccess(object sender, ShowEntitySuccessEventArgs e)
{
    Debug.Log($"Entity {e.EntityAssetName} shown successfully in {e.Duration}s");
}
```

### 6.2 显示实体失败事件

```csharp
public class ShowEntityFailureEventArgs : GameEventArgs
{
    public int EntityId { get; }
    public string EntityAssetName { get; }
    public EntityGroup EntityGroup { get; }
    public string ErrorMessage { get; }
    public object UserData { get; }
}
```

### 6.3 隐藏实体完成事件

```csharp
public class HideEntityCompleteEventArgs : GameEventArgs
{
    public int EntityId { get; }
    public string EntityAssetName { get; }
    public EntityGroup EntityGroup { get; }
    public object UserData { get; }
}
```

---

## 7. 编辑器功能

### 7.1 EntityModuleInspector

`EntityModule` 的 Inspector 扩展，提供运行时实体信息查看功能。

**功能：**
- 显示实体组数量
- 显示实体总数量
- 列出每个实体组及其中的实体数量

**使用方法：**
1. 在编辑器中运行游戏
2. 在 Hierarchy 中找到 `[FrameworkModule]` 下的 `EntityModule`
3. 选中后在 Inspector 面板查看实体统计信息

---

## 8. 目录结构说明

```text
Entity/
├── Editor/                          # 编辑器扩展代码
│   ├── Inspector/
│   │   └── EntityModuleInspector.cs # EntityModule Inspector 扩展
│   └── FuFramework.Entity.Editor.asmdef
├── Runtime/                         # 运行时核心代码
│   ├── EntityModule.cs              # 实体管理模块
│   ├── Helper/
│   │   └── EntityHelper.cs          # 实体辅助器
│   ├── Info/
│   │   ├── Entity.cs                # 实体类
│   │   ├── EntityLogic.cs           # 实体逻辑基类
│   │   ├── EntityGroup.cs           # 实体组
│   │   ├── EntityInfo.cs            # 实体信息
│   │   ├── EntityInstanceObject.cs  # 实体实例对象（对象池）
│   │   ├── EEntityStatus.cs         # 实体状态枚举
│   │   ├── ShowEntityInfo.cs        # 显示实体信息
│   │   ├── ShowEntityInfoEx.cs      # 显示实体扩展信息
│   │   └── AttachEntityInfo.cs      # 附加实体信息
│   ├── Event/
│   │   ├── ShowEntitySuccessEventArgs.cs   # 显示成功事件
│   │   ├── ShowEntityFailureEventArgs.cs   # 显示失败事件
│   │   └── HideEntityCompleteEventArgs.cs  # 隐藏完成事件
│   └── FuFramework.Entity.Runtime.asmdef
└── README.md                        # 本文档
```

---

## 9. 依赖

- **Unity**: 2021.3 LTS 或更高版本
- **FuFramework.Core**: 框架核心模块
- **FuFramework.Asset**: 资源管理模块（用于实体资源加载）
- **FuFramework.Event**: 事件管理模块
- **FuFramework.ObjectPool**: 对象池模块
- **FuFramework.ReferencePool**: 引用池模块
- **FuFramework.ModuleSetting**: 模块配置
- **YooAsset**: 资源管理库
- **UniTask**: 异步编程库

---

## 10. 最佳实践

### 10.1 实体ID管理

建议使用枚举或常量管理实体ID，避免硬编码：

```csharp
public static class EntityIds
{
    public const int Player = 1000;
    public const int MainCamera = 1001;
    
    // 怪物ID范围：2000-2999
    public const int MonsterStart = 2000;
    
    // 特效ID范围：3000-3999
    public const int EffectStart = 3000;
}

// 生成唯一ID
private static int s_MonsterIdCounter = 0;
public static int GetNextMonsterId() => EntityIds.MonsterStart + s_MonsterIdCounter++;
```

### 10.2 实体组划分

合理划分实体组，便于管理和优化：

```csharp
// 按功能划分
- PlayerGroup: 玩家角色
- MonsterGroup: 怪物（可设置较大的对象池容量）
- EffectGroup: 特效（可设置较短的对象过期时间）
- UIGroup: UI 元素
- ItemGroup: 掉落物品
- SceneGroup: 场景物体
```

### 10.3 对象池配置

根据实体类型配置合适的对象池参数：

```csharp
// 频繁创建销毁的实体（如特效）
{
    Name = "EffectGroup",
    InstanceCapacity = 50,
    InstanceExpireTime = 30f,        // 30秒过期
    InstanceAutoReleaseInterval = 60f
}

// 长期存在的实体（如玩家）
{
    Name = "PlayerGroup",
    InstanceCapacity = 10,
    InstanceExpireTime = 300f,       // 5分钟过期
    InstanceAutoReleaseInterval = 300f
}

// 大量出现的实体（如怪物）
{
    Name = "MonsterGroup",
    InstanceCapacity = 200,
    InstanceExpireTime = 60f,
    InstanceAutoReleaseInterval = 120f
}
```

### 10.4 生命周期管理

在适当的生命周期方法中执行相应的逻辑：

```csharp
public class MonsterLogic : EntityLogic
{
    private MonsterData m_Data;
    private AIController m_AI;

    protected internal override void OnInit(object userData)
    {
        // 初始化数据（只执行一次）
        m_Data = userData as MonsterData;
        m_AI = new AIController(this);
    }

    protected internal override void OnShow(object userData)
    {
        // 每次显示时重置状态
        m_AI.Reset();
        gameObject.SetActive(true);
    }

    protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
    {
        // 每帧更新AI
        m_AI.Update(deltaTime);
    }

    protected internal override void OnHide(bool isShutdown, object userData)
    {
        // 隐藏时清理
        m_AI.Stop();
        gameObject.SetActive(false);
    }

    protected internal override void OnRecycle()
    {
        // 回收时释放资源
        m_AI.Dispose();
        m_AI = null;
    }
}
```

### 10.5 异步加载处理

正确处理实体异步加载，避免重复加载：

```csharp
public class EntitySpawner : MonoBehaviour
{
    private HashSet<int> m_LoadingEntities = new HashSet<int>();

    public async void SpawnMonster(int monsterId, string assetPath)
    {
        // 检查是否已在加载中
        if (m_LoadingEntities.Contains(monsterId))
        {
            Debug.LogWarning($"Monster {monsterId} is already loading");
            return;
        }

        m_LoadingEntities.Add(monsterId);

        try
        {
            var entity = await m_EntityModule.ShowEntityAsync<MonsterLogic>(
                monsterId, assetPath, "MonsterGroup"
            );
            
            // 设置初始位置
            entity.Logic.CachedTransform.position = GetSpawnPosition();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to spawn monster: {ex.Message}");
        }
        finally
        {
            m_LoadingEntities.Remove(monsterId);
        }
    }
}
```

---

## 11. 注意事项

1. **实体ID唯一性**：每个实体ID必须在整个游戏生命周期内唯一，重复ID会导致异常
2. **异步加载**：`ShowEntityAsync` 是异步方法，需要使用 `await` 或正确处理返回的 `UniTask`
3. **资源路径**：实体资源路径需要与 YooAsset 的资源寻址一致
4. **实体组配置**：实体组需要在 `ModuleSetting` 中预先配置，否则无法创建实体
5. **生命周期顺序**：实体生命周期回调顺序为 OnInit -> OnShow -> OnUpdate -> OnHide -> OnRecycle
6. **对象池复用**：实体被隐藏后会进入对象池，再次显示时会复用实例，注意在 OnShow 中重置状态
7. **父子关系**：附加子实体时，子实体的生命周期仍由 EntityModule 管理，只是变换层级发生变化
