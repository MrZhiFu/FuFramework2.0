# ObjectPool 重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 优化 ObjectPool 模块：精简 API、消除反射、修复 autoReleaseInterval 常量、O(n²) 排序改 List.Sort、风格规范化、README 对齐。

**Architecture:** 保留 `ObjectPoolModule` + 嵌套 `ObjectPool<T>` 结构；删除全部无调用方的 Type/Predicate 重载（连带消除 `MakeGenericType`+`Activator` 反射）；`CreateObjectPool` 精简到 4 个泛型重载；`ObjectPoolModule.cs` 964 行按 region 拆 3 个 partial；`ObjectPool<T>` 的 O(n²) 冒泡改为 `List.Sort`；新增 `DefaultAutoReleaseInterval` 常量修复语义耦合。

**Tech Stack:** C# (Hotfix / HybridCLR)、Unity。

## Global Constraints

- **交流与注释**：全程中文；代码注释全中文，XML 文档注释 `<summary>` 换行。
- **代码风格**（`Docs/代码风格规范.md`）：方法一律 PascalCase（含私有）；私有字段 `m_` 前缀；Tab 缩进；K&R 括号；显式访问修饰符；能用 `readonly` 就加；`new()` 简化语法；局部变量用 `var`。
- **Git 提交**（`Docs/Git提交规范.md`）：Conventional Commits + 中文 + `[AI]` 前缀；**任何 git add/commit 前必须征得用户同意**；只 add 本任务相关文件，不波及工作区其他未提交改动（当前工作区可能有 Asset/Entity/Scene/Sound 等改动，勿带入）。
- **编译门禁**：每个任务结束由用户手动编译（不使用 unity-cli），确认零错误；**编译验证需用户配合，请在实现者返回后请用户编译**。
- **模块访问入口**：`GlobalModule.ObjectPoolModule` 已存在，禁止重复添加。
- **保留核心 API 签名**：`ObjectPool<T>.Spawn/Recycle/Register/ReleaseObject/Release/ReleaseAllUnused/CanSpawn/SetLocked/SetPriority/GetAllObjectInfos` 及属性签名不变（Entity/UI 调用方零改动）。
- **范围排除**：不改 `ObjectPoolBase`/`ObjectBase`/`ReleaseObjectFilterCallback` 的逻辑；不引入测试设施。

---

### Task 1: API 精简 + 拆分 partial + 风格规范化

删除全部 Type/Predicate 重载、消除反射、`CreateObjectPool` 精简到 4 个泛型重载、私有方法改 PascalCase、拆 3 个 partial、补 XML 注释。

**Files:**
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs`
- Create: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Query.cs`
- Create: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.PoolCreation.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs`（私有方法改名 + 补注释）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/Misc/ObjectInfo.cs`（删冗余注释）

**Interfaces:**
- Consumes: `ObjectPool<T>`（嵌套于模块，构造函数 6 参不变）、`ObjectPoolBase`、`TypeNamePair`、`GlobalModule`（`Hotfix.Framework.Core`）
- Produces: 精简后的公开 API 面（见 Task 1 Step 1），`CreateObjectPoolInternal<T>` 私有方法（Task 2 不依赖，Task 3 README 依赖最终 API 清单）

- [ ] **Step 1: 确定精简后的公开 API 面**

最终 `ObjectPoolModule` 公开 API（供后续任务与 README 使用，签名如下）：

```csharp
// 生命周期
protected internal override void OnInit();    // 订阅低内存
protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime);
protected internal override void OnDispose();  // 退订低内存 + 清理

// 创建
public ObjectPool<T> CreateObjectPool<T>(bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(string poolName, bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime = float.MaxValue, bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase;

// 销毁
public bool DestroyObjectPool<T>() where T : ObjectBase;
public bool DestroyObjectPool<T>(string poolName) where T : ObjectBase;
public bool DestroyObjectPool<T>(ObjectPool<T> objectPool) where T : ObjectBase;
public bool DestroyObjectPool(ObjectPoolBase objectPool);

// 查询（仅泛型）
public bool HasObjectPool<T>() where T : ObjectBase;
public bool HasObjectPool<T>(string poolName) where T : ObjectBase;
public ObjectPool<T> GetObjectPool<T>() where T : ObjectBase;
public ObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase;
public ObjectPoolBase[] GetAllObjectPools();
public void GetAllObjectPools(List<ObjectPoolBase> results);
public ObjectPoolBase[] GetAllObjectPools(bool sort);
public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results);

// 释放
public void Release();
public void ReleaseAllUnused();

// 属性
public int Count { get; }
```

私有方法（PascalCase）：`HasObjectPoolInternal(TypeNamePair)`、`GetObjectPoolInternal(TypeNamePair)`、`CreateObjectPoolInternal<T>(string, bool, float, int, float, int)`、`DestroyObjectPoolInternal(TypeNamePair)`、`ObjectPoolComparer(ObjectPoolBase, ObjectPoolBase)`、`OnLowMemory()`。

- [ ] **Step 2: 重写 `ObjectPoolModule.cs`（字段 + 生命周期 + 释放 + 私有方法）**

整个文件替换为以下内容（删除 Type/Predicate 重载、`_CreateObjectPool(Type,...)` 反射版；保留生命周期/释放/低内存/私有方法；新增 `DefaultAutoReleaseInterval` 常量）：

```csharp
using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池管理模块。
    /// 功能：
    ///     1. 提供对象池的创建、获取、释放和销毁接口。
    /// </summary>
    public sealed partial class ObjectPoolModule : ModuleBase
    {
        /// <summary>
        /// 对象池默认容量。
        /// </summary>
        private const int DefaultCapacity = int.MaxValue;

        /// <summary>
        /// 对象池默认自动释放间隔秒数(默认不自动释放)。
        /// </summary>
        private const float DefaultAutoReleaseInterval = float.MaxValue;

        /// <summary>
        /// 对象池默认过期时间。
        /// </summary>
        private const float DefaultExpireTime = float.MaxValue;

        /// <summary>
        /// 对象池默认优先级。
        /// </summary>
        private const int DefaultPriority = 0;

        /// <summary>
        /// 存储所有对象池的字典, Key为对象池的类型+名称，Value为对象池。
        /// </summary>
        private readonly Dictionary<TypeNamePair, ObjectPoolBase> m_ObjPoolDict = new();

        /// <summary>
        /// 缓存所有对象池的列表。释放所有对象池时使用。
        /// </summary>
        private readonly List<ObjectPoolBase> m_CachedObjPoolList = new();

        /// <summary>
        /// 获取对象池数量。
        /// </summary>
        public int Count => m_ObjPoolDict.Count;

        /// <summary>
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Application.lowMemory += OnLowMemory;
        }

        /// <summary>
        /// 帧更新。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                objPool.Update(unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            foreach (var (typeNamePair, objPool) in m_ObjPoolDict)
            {
                try
                {
                    objPool.OnDispose();
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 释放对象池 {typeNamePair} 时出现异常: {e.Message}");
                }
            }

            m_ObjPoolDict.Clear();
            m_CachedObjPoolList.Clear();

            Application.lowMemory -= OnLowMemory;
        }

        /// <summary>
        /// 低内存回调。
        /// </summary>
        private void OnLowMemory()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 低内存警告, 释放对象池中所有未使用的资源...");
            ReleaseAllUnused();
        }

        /// <summary>
        /// 释放所有对象池中的所有可释放对象。
        /// </summary>
        public void Release()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 释放所有对象池中可释放对象...");
            GetAllObjectPools(true, m_CachedObjPoolList);
            foreach (var objectPool in m_CachedObjPoolList)
            {
                objectPool.Release();
            }
        }

        /// <summary>
        /// 释放对象池中的所有未使用对象。
        /// </summary>
        public void ReleaseAllUnused()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 释放所有对象池中的所有未使用对象...");
            GetAllObjectPools(true, m_CachedObjPoolList);
            foreach (var objectPool in m_CachedObjPoolList)
            {
                objectPool.ReleaseAllUnused();
            }
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="typeNamePair">类型与名称的组合。</param>
        /// <returns>是否存在对象池。</returns>
        private bool HasObjectPoolInternal(TypeNamePair typeNamePair)
        {
            return m_ObjPoolDict.ContainsKey(typeNamePair);
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="typeNamePair">类型与名称的组合。</param>
        /// <returns>要获取的对象池。</returns>
        private ObjectPoolBase GetObjectPoolInternal(TypeNamePair typeNamePair)
        {
            return m_ObjPoolDict.GetValueOrDefault(typeNamePair);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        private ObjectPool<T> CreateObjectPoolInternal<T>(string poolName, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime,
                                                          int priority) where T : ObjectBase
        {
            var typeNamePair = new TypeNamePair(typeof(T), poolName);
            if (HasObjectPoolInternal(typeNamePair))
                throw new InvalidOperationException($"[ObjectPoolModule] 对象池 '{typeNamePair}' 已存在, 不可重复创建.");

            var objectPool = new ObjectPool<T>(poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjPoolDict.Add(typeNamePair, objectPool);
            return objectPool;
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="typeNamePair">类型与名称的组合。</param>
        /// <returns>是否销毁对象池成功。</returns>
        private bool DestroyObjectPoolInternal(TypeNamePair typeNamePair)
        {
            if (!m_ObjPoolDict.TryGetValue(typeNamePair, out var objectPool)) return false;
            objectPool.OnDispose();
            return m_ObjPoolDict.Remove(typeNamePair);
        }

        /// <summary>
        /// 对象池比较器。
        /// </summary>
        /// <param name="a">对象池a。</param>
        /// <param name="b">对象池b。</param>
        /// <returns>优先级比较结果。</returns>
        private static int ObjectPoolComparer(ObjectPoolBase a, ObjectPoolBase b)
        {
            return a.Priority.CompareTo(b.Priority);
        }
    }
}
```

- [ ] **Step 3: 创建 `ObjectPoolModule.Query.cs`（查询对象池）**

新建文件，含原 #region 获取对象池的泛型查询 API + 完整参数重载的创建 API 之外的查询部分：

```csharp
using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        #region 获取对象池

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>() where T : ObjectBase
        {
            return HasObjectPoolInternal(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>(string poolName) where T : ObjectBase
        {
            return HasObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>() where T : ObjectBase
        {
            return (ObjectPool<T>)GetObjectPoolInternal(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase
        {
            return (ObjectPool<T>)GetObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <returns>所有对象池。</returns>
        public ObjectPoolBase[] GetAllObjectPools() => GetAllObjectPools(false);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="results">所有对象池。</param>
        public void GetAllObjectPools(List<ObjectPoolBase> results) => GetAllObjectPools(false, results);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <returns>所有对象池。</returns>
        public ObjectPoolBase[] GetAllObjectPools(bool sort)
        {
            if (sort)
            {
                var results = new List<ObjectPoolBase>();
                foreach (var (_, objPool) in m_ObjPoolDict)
                {
                    results.Add(objPool);
                }

                results.Sort(ObjectPoolComparer);
                return results.ToArray();
            }
            else
            {
                var index   = 0;
                var results = new ObjectPoolBase[m_ObjPoolDict.Count];
                foreach (var (_, objPool) in m_ObjPoolDict)
                {
                    results[index++] = objPool;
                }

                return results;
            }
        }

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <param name="results">所有对象池。</param>
        public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results)
        {
            if (results == null) throw new InvalidOperationException("[ObjectPoolModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                results.Add(objPool);
            }

            if (sort)
                results.Sort(ObjectPoolComparer);
        }

        #endregion
    }
}
```

- [ ] **Step 4: 创建 `ObjectPoolModule.PoolCreation.cs`（创建/销毁对象池）**

新建文件，含精简后的 4 个 CreateObjectPool 重载 + 销毁对象池 API（全部委托私有 `CreateObjectPoolInternal` / `DestroyObjectPoolInternal`）：

```csharp
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        #region 创建对象池

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(string.Empty, allowSpawnInUse, DefaultAutoReleaseInterval, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, DefaultAutoReleaseInterval, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime = float.MaxValue, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(string.Empty, allowSpawnInUse, DefaultAutoReleaseInterval, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池(命名池 + 容量 + 过期时间 + 优先级，自动释放间隔取默认)。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, DefaultAutoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority,
                                                 bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
        }

        #endregion

        #region 销毁对象池

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>() where T : ObjectBase
        {
            return DestroyObjectPoolInternal(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(string poolName) where T : ObjectBase
        {
            return DestroyObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(ObjectPool<T> objectPool) where T : ObjectBase
        {
            if (objectPool == null) throw new InvalidOperationException("[ObjectPoolModule] 对象池为不能为空.");
            return DestroyObjectPoolInternal(new TypeNamePair(typeof(T), objectPool.Name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(ObjectPoolBase objectPool)
        {
            if (objectPool == null) throw new InvalidOperationException("[ObjectPoolModule] 对象池为不能为空.");
            return DestroyObjectPoolInternal(new TypeNamePair(objectPool.ObjectType, objectPool.Name));
        }

        #endregion
    }
}
```

- [ ] **Step 5: 修改 `ObjectPoolModule.ObjectPool.cs`（私有方法改名 + 补注释）**

在 `ObjectPool<T>` 内：
- `_GetObject` → `GetObject`（3 处调用：行 230/260/396/422）
- `_GetCanReleaseObjects` → `GetCanReleaseObjects`（3 处调用：行 72/320/340）
- `_DefaultReleaseObjectFilterCallback` → `DefaultReleaseObjectFilterCallback`（2 处：字段初始化行 118、`m_DefaultReleaseObjectFilterCallback` 引用行 284）

用编辑器对 `ObjectPoolModule.ObjectPool.cs` 做上述替换（replace_all）。不改方法体逻辑（排序优化在 Task 2）。同时给缺 `<summary>` 的成员补 XML 注释（`m_Capacity`、`m_ExpireTime`、`m_AutoReleaseTimer`、`m_ObjectMultiDict` 等字段若缺注释则补，格式与 `ObjectPool<T>` 现有 `<summary>` 一致）。

- [ ] **Step 6: 修改 `ObjectInfo.cs`（删冗余注释）**

删除 `ObjectInfo.cs` 中 6 处 `// Preserve the X property for Unity's serialization` 与 1 处 `// Preserve the constructor for Unity's serialization` 行注释（结构体不需要 Unity 序列化保护）。

- [ ] **Step 7: grep 验证无残留引用**

```bash
grep -rn "CreateObjectPool(Type\|GetObjectPool(Type\|HasObjectPool(Type\|GetObjectPools(\|_CreateObjectPool\|_HasObjectPool\|_GetObjectPool\|_DestroyObjectPool\|_ObjectPoolComparer\|_GetCanReleaseObjects\|_DefaultReleaseObjectFilterCallback\|_GetObject(" --include='*.cs' Unity/Assets/Scripts/Hotfix
```

预期：**零命中**。同时确认 Entity/UI 调用方仍可用：
```bash
grep -rn "CreateObjectPool\|ObjectPoolModule\|\.Spawn(\|\.Recycle(" --include='*.cs' Unity/Assets/Scripts/Hotfix/Framework/Entity/EntityGroup.cs Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs
```
预期：`CreateObjectPool<WinObject>("UIWinObjectPool")`、`ObjectPoolModule.ObjectPool<EntityInstanceObject>` 等现有调用不变（它们的签名在精简后的 API 中仍存在）。

- [ ] **Step 8: 请用户手动编译**

返回控制器，控制器请用户编译。预期零错误（若报缺失 API，说明有 Type 版调用方遗漏，回到 Step 5/7 排查）。

- [ ] **Step 9: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Query.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Query.cs.meta \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.PoolCreation.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.PoolCreation.cs.meta \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/Misc/ObjectInfo.cs
git commit -m "[AI]refactor: 精简 ObjectPool API（删 Type/Predicate 重载），拆分 partial，风格规范化"
```

**提交前征得用户同意。** 只 add 上述文件，不 add 工作区其他未提交改动。

---

### Task 2: 排序优化 + autoReleaseInterval 常量修复

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs`（`DefaultReleaseObjectFilterCallback` 排序改 List.Sort）
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs`（已含 `DefaultAutoReleaseInterval` 常量，本任务验证其被所有重载正确使用）

**Interfaces:**
- Consumes: Task 1 的 `ObjectPool<T>`（含改名的 `DefaultReleaseObjectFilterCallback` 方法）
- Produces: O(n log n) 排序 + 语义解耦的常量

- [ ] **Step 1: 替换 `DefaultReleaseObjectFilterCallback` 的第二阶段排序**

将 `ObjectPool<T>.DefaultReleaseObjectFilterCallback` 中第二阶段（原行 509-526 的双重循环冒泡）替换为 `List.Sort`：

```csharp
// 第二阶段：按（优先级升序，最后使用时间升序）排序，取前 toReleaseCount 个
candidateObjects.Sort((a, b) =>
{
    var priorityCmp = a.Priority.CompareTo(b.Priority);
    return priorityCmp != 0 ? priorityCmp : a.LastUseTime.CompareTo(b.LastUseTime);
});
for (var i = 0; i < toReleaseCount && i < candidateObjects.Count; i++)
{
    m_CachedToReleaseObjectList.Add(candidateObjects[i]);
}
```

第一阶段（剔除过期对象）保持不变。方法最终形态：

```csharp
private List<T> DefaultReleaseObjectFilterCallback(List<T> candidateObjects, int toReleaseCount, DateTime? expireTimeThreshold)
{
    m_CachedToReleaseObjectList.Clear();

    // 第一阶段：根据最后使用时间筛选过期对象。
    if (expireTimeThreshold.HasValue)
    {
        for (var i = candidateObjects.Count - 1; i >= 0; i--)
        {
            // 如果对象最后使用时间 > 过期时间点，说明了对象还没过期，则继续筛选。
            if (candidateObjects[i].LastUseTime > expireTimeThreshold.Value) continue;
            m_CachedToReleaseObjectList.Add(candidateObjects[i]);
            candidateObjects.RemoveAt(i);
        }

        toReleaseCount -= m_CachedToReleaseObjectList.Count;
    }

    // 第二阶段：按（优先级升序，最后使用时间升序）排序，取前 toReleaseCount 个
    candidateObjects.Sort((a, b) =>
    {
        var priorityCmp = a.Priority.CompareTo(b.Priority);
        return priorityCmp != 0 ? priorityCmp : a.LastUseTime.CompareTo(b.LastUseTime);
    });
    for (var i = 0; i < toReleaseCount && i < candidateObjects.Count; i++)
    {
        m_CachedToReleaseObjectList.Add(candidateObjects[i]);
    }

    return m_CachedToReleaseObjectList;
}
```

- [ ] **Step 2: 核对 `DefaultAutoReleaseInterval` 使用**

grep 确认 `ObjectPoolModule.cs` 中所有 `CreateObjectPoolInternal<T>` 调用（4 个重载）的 autoReleaseInterval 参数位都用 `DefaultAutoReleaseInterval`（而非 `DefaultExpireTime`）：

```bash
grep -n "CreateObjectPoolInternal" Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.PoolCreation.cs
```
预期：4 处，均为 `CreateObjectPoolInternal<T>(... DefaultAutoReleaseInterval ... DefaultExpireTime ...)`。

- [ ] **Step 3: 请用户手动编译**

返回控制器，控制器请用户编译。预期零错误。

- [ ] **Step 4: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs
git commit -m "[AI]perf: 释放筛选改 List.Sort 消除 O(n²)，修复 autoReleaseInterval 默认值语义耦合"
```

**提交前征得用户同意。**

---

### Task 3: README 重写

**Files:**
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md`

**Interfaces:**
- Consumes: Task 1 精简后的最终 API 面（4 个 CreateObjectPool 重载、泛型查询、删除 Type/Predicate 版）
- Produces: 与代码一致的文档

- [ ] **Step 1: 重写 `ObjectPool/README.md`**

按以下变更清单重写（未列出的章节保持原语义，仅同步 API 写法）：

1. **API 清单**：删除所有 `CreateObjectPool(Type, ...)`、`HasObjectPool(Type)`、`GetObjectPool(Type)`、`GetObjectPools(Predicate)` 示例；`CreateObjectPool<T>` 列出 4 个重载。
2. **参数说明**：`autoReleaseInterval` 默认值注明 `float.MaxValue`（默认不自动释放），与 `expireTime` 分离说明；修正依赖表中引用。
3. **目录结构**：更新为拆分后的 6 个文件（含 `ObjectPoolModule.Query.cs`、`ObjectPoolModule.PoolCreation.cs`）。
4. **示例代码**：确保所有 `ReferencePool.` 静态调用已替换为 `GlobalModule.ReferencePoolModule.Xxx`（上一轮已改，本轮核对）；使用 `GlobalModule.ObjectPoolModule.CreateObjectPool` 的示例保持一致。
5. **默认值**：`capacity` 默认 `int.MaxValue`、`expireTime` 默认 `float.MaxValue`、`priority` 默认 `0`，与代码一致。

- [ ] **Step 2: 自检一致性**

```bash
grep -n "CreateObjectPool(Type\|HasObjectPool(Type\|GetObjectPool(Type\|GetObjectPools(\|_CreateObjectPool" Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md
```
预期：零命中。

- [ ] **Step 3: 提交**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md
git commit -m "[AI]docs: 重写 ObjectPool README，同步精简后的 API 与默认值说明"
```

**提交前征得用户同意。**

---

## Self-Review 备注

- spec 的"删除 Type 版"、"精简到 4 个"、"拆 3 个 partial"、"私有方法 PascalCase"在 Task 1 落地；"O(n²)→List.Sort"与"autoReleaseInterval 常量"在 Task 2；"README 重写"在 Task 3；"验证方式"由每任务的用户手动编译 + Task 4（可选）Play 冒烟承载。
- 提交拆分与 spec §9 一致（refactor / perf / docs 三提交）。
