# FuFramework Entity Module

## 简介
FuFramework Entity 模块是一个功能强大的实体管理系统，旨在统一管理游戏中的所有动态对象（如角色、怪物、特效等）。它基于“实体组（EntityGroup）”的概念，提供了实体的加载、实例化、生命周期管理、对象池复用以及层级管理等功能。

## 核心概念

### 1. 实体 (Entity)
实体是游戏中的基本动态对象。
- **Entity**: 框架内部使用的实体包装类，负责连接框架层与逻辑层。
- **EntityLogic**: 实体逻辑基类（`MonoBehaviour`），开发者编写的具体业务逻辑（如 `PlayerLogic`, `MonsterLogic`）都应继承此类。

### 2. 实体组 (EntityGroup)
实体组用于对实体进行分类管理。
- 每个实体组对应一个 GameObject 根节点。
- 可以为不同的实体组设置不同的属性（如自动释放间隔、对象池容量等）。
- 常见分组：`Player`, `Monster`, `Effect`, `UI` 等。

### 3. 实体辅助器 (EntityHelper)
负责实体的底层实例化和销毁操作，默认实现基于 `YooAsset` 进行资源加载和实例化。

## 核心类说明

### EntityManager
实体管理器，继承自 `FuModule`。
- **ShowEntity**: 显示（加载）一个实体。
- **HideEntity**: 隐藏（回收）一个实体。
- **AttachEntity**: 将一个实体附加到另一个实体上（如武器附加到角色手上）。
- **GetEntityGroup**: 获取指定的实体组。

### EntityLogic
实体逻辑基类，提供了丰富的生命周期回调：
- `OnInit(object userData)`: 实体初始化时调用（仅一次）。
- `OnShow(object userData)`: 实体显示时调用。
- `OnHide(bool isShutdown, object userData)`: 实体隐藏时调用。
- `OnRecycle()`: 实体被回收时调用。
- `OnUpdate(float elapseSeconds, float realElapseSeconds)`: 轮询更新。
- `OnAttached` / `OnDetached`: 子实体附加/解除时的回调。

## 使用示例

### 1. 定义实体逻辑类
```csharp
using FuFramework.Entity.Runtime;
using UnityEngine;

public class MyHeroLogic : EntityLogic
{
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        Debug.Log("Hero Init");
    }

    protected override void OnShow(object userData)
    {
        base.OnShow(userData);
        Debug.Log($"Hero Show, Data: {userData}");
        
        // 设置位置等初始化操作
        transform.position = Vector3.zero;
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        // 移动逻辑...
    }
}
```

### 2. 显示实体
```csharp
// 获取实体管理器
var entityMgr = ModuleManager.GetModule<EntityManager>();

// 显示实体
// 参数: 实体ID(需唯一), 资源路径, 实体组名称, 优先级, 自定义数据
entityMgr.ShowEntity(1001, "Assets/Game/Heroes/HeroA.prefab", "PlayerGroup", 0, "MyUserData");
```

### 3. 隐藏实体
```csharp
// 通过实体ID隐藏
entityMgr.HideEntity(1001);

// 或者在 EntityLogic 内部调用
// entityMgr.HideEntity(this.Entity.Id);
```

### 4. 附加子实体
例如将武器附加到角色的右手节点：
```csharp
// 假设 1001 是角色，2001 是武器
// 参数: 子实体ID, 父实体ID, 父节点路径(相对于父实体), 自定义数据
entityMgr.AttachEntity(2001, 1001, "Bone/RightHand", null);
```

## 配置说明
实体组的配置位于 `EntitySetting` (ScriptableObject) 中，需在编辑器中预先配置好所有用到的实体组（如 `PlayerGroup`, `EffectGroup` 等）。

## 编辑器扩展
选中场景中的 `[ModuleManager]` 节点，在 Inspector 面板的 `EntityManager` 组件中可以查看：
- **统计信息**：当前实体总数、实体组数量。
- **分组详情**：每个实体组内当前的实体数量。
