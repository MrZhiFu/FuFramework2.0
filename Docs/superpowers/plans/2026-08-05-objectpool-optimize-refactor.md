# ObjectPool 模块优化重构实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 ObjectPool 模块冗余——合并 `Object<T>` 包装类到 `ObjectBase`、池直接存 `T`、双重回收改单次、`GetAllObjectPools` 精简 4→2、`ObjectPool<T>` 拆 partial、README 同步。

**Architecture:** 保留 `ObjectPoolModule` + 嵌套 `ObjectPool<T>` 结构；`SpawnCount/IsInUse/Spawn()/Recycle()` 从私有包装类迁入 `ObjectBase`（池状态上移，先例同 `LastUseTime`）；`ObjectPool<T>` 两个字典改存 `T` 本身，T 重载直接操作 `T`、object 重载经 target 字典查 `T`；删除 `ObjectPoolModule.Object.cs`；`ObjectPool<T>` 改 partial 拆两文件满足 500 行规范。

**Tech Stack:** C# (Hotfix / HybridCLR)、Unity。

**Spec:** `Docs/superpowers/specs/2026-08-05-objectpool-optimize-refactor-design.md`

## Global Constraints

- **交流与注释**：全程中文；代码注释全中文，XML 文档注释 `<summary>` 换行（不允许单行 `/// <summary>text</summary>`）。
- **代码风格**（`Docs/代码风格规范.md`）：方法一律 PascalCase（含私有）；私有字段 `m_` 前缀；Tab 缩进；K&R 括号；显式访问修饰符；能用 `readonly` 就加；`new()` 简化语法；局部变量用 `var`；单独文件不超过 500 行。
- **Git 提交**（`Docs/Git提交规范.md`）：Conventional Commits + 中文 + `[AI]` 前缀；**任何 git add/commit 前必须征得用户同意**；只 add 本任务相关文件，不波及工作区其他未提交改动。
- **编译门禁**：每个任务结束由用户手动编译（不使用 unity-cli），确认零错误；编译验证需用户配合，请在实现者返回后请用户编译。
- **模块访问入口**：`GlobalModule.ObjectPoolModule` 已存在，禁止重复添加。
- **保留核心 API 签名**：`ObjectPool<T>.Register/Get/Recycle/DisposeObject/CanGet/SetLocked/SetPriority/Dispose/DisposeAllUnused/GetAllObjectInfos` 及属性签名不变（Entity/UI 调用方零改动）。
- **范围排除**：不改 `ObjectPoolBase`/`DisposeObjectFilterCallback` 的逻辑；不引入测试设施；不做 `SpawnCount/OnSpawn/OnRecycle/allowSpawnInUse` 重命名。
- **当前分支**：`refactor/framework-modules-to-hotfix`，工作区除本任务文件外无其他未提交改动。

---

### Task 1: 合并 Object<T> 到 ObjectBase + 池存 T + 拆 partial（Commit 1）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/Base/ObjectBase.cs`
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs`
- Create: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.Dispose.cs`
- Delete: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Object.cs`（+ `.meta`）

**Interfaces:**
- Consumes: `ObjectBase`（现含 `Name/Target/Locked/Priority/LastUseTime/CustomCanDisposeFlag` + `IReference.Clear()`）、`ObjectPool<T>`（现嵌套于模块）、`FuMultiDictionary<string, T>`、`TypeNamePair`、`DisposeObjectFilterCallback<T>`、`GlobalModule.ReferencePoolModule`
- Produces: 带 `SpawnCount/IsInUse/internal Spawn()/internal Recycle()` 的 `ObjectBase`；存 `T` 的 `ObjectPool<T>`（partial 两文件）；Task 2/3 依赖其最终形态

- [ ] **Step 1: `ObjectBase.cs` 新增 `SpawnCount`/`IsInUse` 属性**

在 `ObjectBase` 类的 `CustomCanDisposeFlag` 属性之后、`Initialize` 重载之前，插入：

```csharp
/// <summary>
/// 获取对象池中对象的获取计数（引用计数）。
/// </summary>
public int SpawnCount { get; private set; }

/// <summary>
/// 获取对象是否正在使用中。
/// </summary>
public bool IsInUse => SpawnCount > 0;
```

- [ ] **Step 2: `ObjectBase.cs` 新增 `internal void Spawn()` / `internal void Recycle()`**

在 `IsInUse` 之后插入（逻辑原样迁自 `Object<T>.Spawn/Recycle`，去掉 `TargetObject.` 间接）：

```csharp
/// <summary>
/// 生成对象。
/// </summary>
internal void Spawn()
{
    SpawnCount++;
    try
    {
        LastUseTime = DateTime.UtcNow;
        OnSpawn();
    }
    catch
    {
        SpawnCount--;
        throw;
    }
}

/// <summary>
/// 回收对象。
/// </summary>
internal void Recycle()
{
    if (SpawnCount <= 0)
        throw new InvalidOperationException($"[ObjectBase] 对象 '{Name}' 生成次数已经为 0, 回收失败.");

    OnRecycle();
    LastUseTime = DateTime.UtcNow;
    SpawnCount--;
}
```

- [ ] **Step 3: `ObjectBase.cs` 的 `_Initialize` 与 `Clear` 重置 `SpawnCount`**

`_Initialize` 末尾（`LastUseTime = DateTime.UtcNow;` 之后）加：

```csharp
SpawnCount = 0;
```

`Clear()` 中（`LastUseTime = default;` 之后）加：

```csharp
SpawnCount = 0;
```

- [ ] **Step 4: `ObjectPoolModule.ObjectPool.cs` — 字典与字段类型改 `T`**

将字段声明与构造函数中泛型改为 `T`：

```csharp
private readonly FuMultiDictionary<string, T> m_ObjectMultiDict;
private readonly Dictionary<object, T> m_TargetObjectDict;
```

构造函数（`Object<T>` → `T`）：

```csharp
public ObjectPool(string name, bool allowSpawnInUse, float autoDisposeInterval, int capacity, float expireTime, int priority) : base(name)
{
    m_ObjectMultiDict                    = new FuMultiDictionary<string, T>();
    m_TargetObjectDict                   = new Dictionary<object, T>();
    m_DefaultDisposeObjectFilterCallback = DefaultDisposeObjectFilterCallback;
    m_CachedCanDisposeObjectList         = new List<T>();
    m_CachedToDisposeObjectList          = new List<T>();

    AllowSpawnInUse     = allowSpawnInUse;
    AutoDisposeInterval = autoDisposeInterval;
    Capacity            = capacity;
    ExpireTime          = expireTime;
    Priority            = priority;
    m_AutoDisposeTimer  = 0f;
}
```

- [ ] **Step 5: `ObjectPoolModule.ObjectPool.cs` — `Register` 直接存 `T`**

```csharp
public void Register(T obj, bool spawned)
{
    if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 要创建并注册对象不能为空.");

    m_ObjectMultiDict.Add(obj.Name, obj);
    m_TargetObjectDict.Add(obj.Target, obj);

    // 对象是否提前生成，若是则直接走一次生成流程（计数+1、刷新最后使用时间、触发 OnSpawn）
    if (spawned) obj.Spawn();

    if (Count > m_Capacity)
        Dispose();
}
```

> 行为差异（预期且更正确）：原 `Object<T>.Create` 对 spawned 只置计数+调 OnSpawn，不刷新 LastUseTime；现在走 `Spawn()` 会顺带刷新 LastUseTime。

- [ ] **Step 6: `ObjectPoolModule.ObjectPool.cs` — `Get`/`CanGet` 遍历 `T`**

```csharp
public T Get(string name)
{
    if (name == null) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

    if (!m_ObjectMultiDict.TryGetValue(name, out var objectRange)) return null;

    foreach (var obj in objectRange)
    {
        // 如果允许获取正在使用的对象，则直接获取。
        if (AllowSpawnInUse)
            return obj.Spawn();

        // 如果对象没有正在使用，则直接获取。
        if (!obj.IsInUse)
            return obj.Spawn();
    }

    return null;
}
```

`CanGet(string name)` 同逻辑，把两个 `return obj.Spawn();` 改为 `return true;`、末尾 `return false;`。

- [ ] **Step 7: `ObjectPoolModule.ObjectPool.cs` — `Recycle` T 重载绕过字典，object 重载查字典**

```csharp
public void Recycle(T obj)
{
    if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");
    obj.Recycle();
    if (Count > m_Capacity && obj.SpawnCount <= 0)
    {
        Dispose();
    }
}

public void Recycle(object target)
{
    if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 要回收的目标对象不能为空.");

    var obj = GetObject(target);
    if (obj == null)
        throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中找不到目标对象 '{target.GetType().FullName}'.");

    obj.Recycle();
    if (Count > m_Capacity && obj.SpawnCount <= 0)
    {
        Dispose();
    }
}
```

- [ ] **Step 8: `ObjectPoolModule.ObjectPool.cs` — `DisposeObject` T 重载做核心逻辑 + 单次回收，object 重载查字典委托**

```csharp
public bool DisposeObject(T obj)
{
    if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");

    if (obj.IsInUse) return false;
    if (obj.Locked) return false;
    if (!obj.CustomCanDisposeFlag) return false;

    FuLogger.LogInfo($"[ObjectPoolModule] 真正销毁对象池中的可销毁对象 '{obj.Name}'");

    m_ObjectMultiDict.Remove(obj.Name, obj);
    m_TargetObjectDict.Remove(obj.Target);

    try
    {
        obj.OnDispose();
    }
    finally
    {
        // 即使 OnDispose 异常也回收对象到引用池，避免跳过清理
        GlobalModule.ReferencePoolModule.Recycle(obj);
    }

    return true;
}

public bool DisposeObject(object target)
{
    if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");

    var obj = GetObject(target);
    if (obj == null) return false;

    return DisposeObject(obj);
}
```

- [ ] **Step 9: `ObjectPoolModule.ObjectPool.cs` — `OnDispose` 单次回收（try/catch/finally）**

```csharp
internal override void OnDispose()
{
    foreach (var (_, obj) in m_TargetObjectDict)
    {
        try
        {
            obj.OnDispose();
        }
        catch (Exception e)
        {
            FuLogger.LogWarning($"[ObjectPoolModule] 销毁对象池 {Name} 中的对象时出现异常: {e.Message}");
        }
        finally
        {
            // 单次回收：即使 OnDispose 异常也回收对象到引用池
            GlobalModule.ReferencePoolModule.Recycle(obj);
        }
    }

    m_ObjectMultiDict.Clear();
    m_TargetObjectDict.Clear();
    m_CachedCanDisposeObjectList.Clear();
    m_CachedToDisposeObjectList.Clear();
}
```

- [ ] **Step 10: `ObjectPoolModule.ObjectPool.cs` — `SetLocked`/`SetPriority` T 重载绕过字典，object 重载查字典**

```csharp
public void SetLocked(T obj, bool locked)
{
    if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");
    obj.Locked = locked;
}

public void SetLocked(object target, bool locked)
{
    if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");

    var obj = GetObject(target);
    if (obj == null)
        throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”.");
    obj.Locked = locked;
}

public void SetPriority(T obj, int priority)
{
    if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");
    obj.Priority = priority;
}

public void SetPriority(object target, int priority)
{
    if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");

    var obj = GetObject(target);
    if (obj == null)
        throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”..");
    obj.Priority = priority;
}
```

- [ ] **Step 11: `ObjectPoolModule.ObjectPool.cs` — `GetObject`/`GetCanDisposeObjects`/`GetAllObjectInfos` 返回/存储 `T`**

```csharp
private T GetObject(object target)
{
    if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");
    return m_TargetObjectDict.GetValueOrDefault(target);
}
```

```csharp
private void GetCanDisposeObjects(List<T> results)
{
    if (results == null) throw new InvalidOperationException("[ObjectPoolModule] 结果列表不能为空.");

    results.Clear();
    foreach (var (_, obj) in m_TargetObjectDict)
    {
        // 如果对象正在使用中，或者被加锁，或者自定义标记为不能被销毁，则跳过。
        if (obj.IsInUse || obj.Locked || !obj.CustomCanDisposeFlag)
        {
            continue;
        }

        results.Add(obj);
    }
}
```

```csharp
public override ObjectInfo[] GetAllObjectInfos()
{
    var results = new List<ObjectInfo>();
    foreach (var (_, objectRang) in m_ObjectMultiDict)
    {
        foreach (var obj in objectRang)
        {
            results.Add(new ObjectInfo(obj.Name, obj.Locked, obj.CustomCanDisposeFlag,
                                       obj.Priority, obj.LastUseTime, obj.SpawnCount));
        }
    }

    return results.ToArray();
}
```

- [ ] **Step 12: 拆分 `ObjectPool<T>` 为 partial —— 新建 `ObjectPoolModule.ObjectPool.Dispose.cs`**

将 `ObjectPool<T>` 声明改 `public sealed partial class ObjectPool<T> : ObjectPoolBase`。

**`ObjectPoolModule.ObjectPool.cs` 保留**：类声明、字段、构造函数、属性（`AutoDisposeInterval/Priority/AllowSpawnInUse/ObjectType/Count/CanDisposeCount/Capacity/ExpireTime`）、`Register/Get/Recycle/CanGet/SetLocked/SetPriority/GetAllObjectInfos/GetObject`。

**新建 `ObjectPoolModule.ObjectPool.Dispose.cs`**，移入：`Update`、`OnDispose`、`Dispose()`×3、`DisposeAllUnused`、`DisposeObject`×2、`GetCanDisposeObjects`、`DefaultDisposeObjectFilterCallback`。文件骨架：

```csharp
using System;
using System.Collections.Generic;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        public sealed partial class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
        {
            /// <summary>
            /// 对象池轮询。
            /// </summary>
            /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
            internal override void Update(float unscaledDeltaTime)
            {
                // 默认不自动销毁时短路，避免无谓的每帧累加
                if (AutoDisposeInterval >= float.MaxValue) return;

                m_AutoDisposeTimer += unscaledDeltaTime;

                // 每隔 AutoDisposeInterval 秒触发一次自动销毁检查
                if (m_AutoDisposeTimer >= AutoDisposeInterval)
                {
                    Dispose();
                }
            }

            // （其余方法原样搬移：OnDispose / Dispose×3 / DisposeAllUnused / DisposeObject×2 /
            //  GetCanDisposeObjects / DefaultDisposeObjectFilterCallback，代码见 Step 8-9、11 及原文件）
        }
    }
}
```

> `DefaultDisposeObjectFilterCallback` 逻辑不变（已操作 `List<T>`），仅随文件搬移；保留两阶段筛选（过期剔除 + `List.Sort` 按优先级/最后使用时间取前 N）。

- [ ] **Step 13: 删除 `ObjectPoolModule.Object.cs`（+ `.meta`）**

删除文件后，用下面的 grep 确认全模块无残留。

- [ ] **Step 14: grep 验证无残留引用**

```bash
grep -rn "Object<T>\|TargetObject" Unity/Assets/Scripts/Hotfix/Framework/ObjectPool --include='*.cs'
```
预期：**零命中**。

调用方核查（签名不变，应仍全部命中）：

```bash
grep -rn "\.Register(\|\.Get(\|\.Recycle(\|\.DisposeObject(\|\.SetLocked(\|\.SetPriority(" \
  Unity/Assets/Scripts/Hotfix/Framework/Entity/Info/EntityGroup.cs \
  Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.cs \
  Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Open.cs \
  Unity/Assets/Scripts/Hotfix/Framework/UI/UIModule.Close.cs
```
预期：`EntityGroup` 的 `Register/Get/Recycle×2/SetLocked/SetPriority`、`UIModule` 的 `Register/Get/Recycle/SetLocked/SetPriority` 全部仍命中。

- [ ] **Step 15: 请用户手动编译**

返回控制器，控制器请用户编译。预期零错误（若报缺失 `Object<T>`/`TargetObject` 相关错误，回到 Step 5-13 排查漏改）。

- [ ] **Step 16: 提交（Commit 1）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/Base/ObjectBase.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.Dispose.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.Dispose.cs.meta \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Object.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Object.cs.meta
git commit -m "[AI]refactor: 合并 Object<T> 到 ObjectBase，池直接存 T，双重回收改单次，拆分 ObjectPool<T> partial"
```

**提交前征得用户同意。** 只 add 上述文件，不 add 工作区其他未提交改动。

---

### Task 2: GetAllObjectPools 精简 4→2 + 文档修复（Commit 2）

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Query.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/Base/ObjectBase.cs`
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs`

**Interfaces:**
- Consumes: Task 1 的 `ObjectPool<T>`、`GetAllObjectPools(bool, List)` 内部调用（`ObjectPoolModule.cs` 的 `Release`/`ReleaseAllUnused`）
- Produces: 精简后 API 面 + 规范注释（Task 3 README 依赖最终 API 清单）

- [ ] **Step 1: `Query.cs` — `GetAllObjectPools` 4 重载删 2**

删除 `GetAllObjectPools()` 与 `GetAllObjectPools(List<ObjectPoolBase>)` 两个无 bool 版；`GetAllObjectPools(bool sort)` 加默认参数 `= false`。保留：

```csharp
/// <summary>
/// 获取所有对象池。
/// </summary>
/// <param name="sort">是否根据对象池的优先级排序。</param>
/// <returns>所有对象池。</returns>
public ObjectPoolBase[] GetAllObjectPools(bool sort = false)
{
    // 原方法体不变
}

/// <summary>
/// 获取所有对象池。
/// </summary>
/// <param name="sort">是否根据对象池的优先级排序。</param>
/// <param name="results">所有对象池。</param>
public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results)
{
    // 原方法体不变
}
```

> 原无 bool 版是纯语法糖（`=> GetAllObjectPools(false)` 一行），删除后 `GetAllObjectPools()` 调用仍走默认参数。

- [ ] **Step 2: `ObjectPoolModule.cs` — 类注释修复**

第 13 行 `提供对象池的创建、获取、销毁和销毁接口。` → `提供对象池的创建、获取、销毁接口。`

- [ ] **Step 3: `Base/ObjectBase.cs` — 属性单行 `///` 注释转 XML**

第 16-32 行的 6 个属性，单行 `///` 改多行 XML（含修正 `。。` 笔误）：

```csharp
/// <summary>
/// 对象名称。
/// </summary>
public string Name { get; private set; }

/// <summary>
/// 对象的目标真实对象。如GameObject。
/// </summary>
public object Target { get; private set; }

/// <summary>
/// 对象是否被加锁。
/// </summary>
public bool Locked { get; set; }

/// <summary>
/// 对象的优先级。
/// </summary>
public int Priority { get; set; }

/// <summary>
/// 对象上次使用时间。
/// </summary>
public DateTime LastUseTime { get; internal set; }

/// <summary>
/// 自定义是否可销毁标记。默认为true。
/// </summary>
public virtual bool CustomCanDisposeFlag => true;
```

- [ ] **Step 4: `ObjectPoolModule.ObjectPool.cs` — 补注释**

① `GetObject(object target)` 补全 `<param>`/`<returns>`：

```csharp
/// <summary>
/// 获取对象。
/// </summary>
/// <param name="target">目标对象。</param>
/// <returns>目标对象对应的池内对象，不存在时返回null。</returns>
private T GetObject(object target)
```

② `GetCanDisposeObjects` 的 `<summary>` "获取对象池中能被销毁的对象的数量" → "获取对象池中能被销毁的对象（填充到结果列表）。"

③ 私有字段 `m_AutoDisposeInterval` 注释 "获取或设置对象池每次轮询中自动销毁可销毁对象的间隔秒数。" → "对象池自动销毁可销毁对象的间隔秒数。"

- [ ] **Step 5: grep 验证**

```bash
grep -rn "GetAllObjectPools" Unity/Assets/Scripts/Hotfix --include='*.cs'
```
预期：仅 `ObjectPoolModule.cs` 内部 `GetAllObjectPools(true, m_CachedObjPoolList)`、`Query.cs` 两个重载；Entity/UI 不调用（零命中）。

- [ ] **Step 6: 请用户手动编译**

预期零错误。

- [ ] **Step 7: 提交（Commit 2）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.Query.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/Base/ObjectBase.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.cs \
        Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/ObjectPoolModule.ObjectPool.Dispose.cs
git commit -m "[AI]refactor: GetAllObjectPools 精简为 2 个重载，修复 ObjectPool 注释规范"
```

**提交前征得用户同意。**

---

### Task 3: README 同步（Commit 3）

**Files:**
- Rewrite: `Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md`

**Interfaces:**
- Consumes: Task 1/2 的最终文件结构与 API 面（`GetAllObjectPools` 2 重载、`ObjectBase` 含 `SpawnCount/IsInUse`、删 `Object.cs`、新增 `ObjectPool.Dispose.cs`）
- Produces: 与代码一致的文档

- [ ] **Step 1: 更新目录结构**

README 第 6 节目录树：删除 `ObjectPoolModule.Object.cs  # 内部对象包装类` 一行；新增 `ObjectPoolModule.ObjectPool.Dispose.cs  # 对象池销毁与筛选`；`ObjectBase.cs` 说明不变。

- [ ] **Step 2: 更新 ObjectBase 说明**

第 4.4 节 `ObjectBase` 核心属性代码块补 `SpawnCount` 与 `IsInUse`：

```csharp
public int SpawnCount { get; private set; }       // 获取计数（引用计数）
public bool IsInUse => SpawnCount > 0;            // 是否正在使用
```

- [ ] **Step 3: 更新 GetAllObjectPools 重载数**

第 4.1 节 `ObjectPoolModule` 核心功能代码块，`GetAllObjectPools` 从 4 个重载改为 2 个：

```csharp
public ObjectPoolBase[] GetAllObjectPools(bool sort = false)           // 获取所有对象池（按优先级排序可选）
public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results) // 获取所有对象池（填充到列表）
```

- [ ] **Step 4: 自检一致性**

```bash
grep -n "Object<T>\|ObjectPoolModule.Object.cs\|GetAllObjectPools()" Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md
```
预期：零命中（无 `Object<T>`、无旧 `Object.cs` 引用、无参数版 `GetAllObjectPools()` 列示）。

- [ ] **Step 5: 提交（Commit 3）**

```bash
git add Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/README.md
git commit -m "[AI]docs: 同步 ObjectPool README 目录结构、ObjectBase 成员与重载数"
```

**提交前征得用户同意。**

---

## Self-Review 备注

- spec §3（合并设计）→ Task 1 Step 1-11；spec §5（文件拆分）→ Task 1 Step 12-13；spec §4（API 精简）→ Task 2 Step 1；spec §6（文档修复）→ Task 2 Step 2-4；spec §7（验证）→ 各任务 grep + 用户编译；spec §8（提交拆分）→ Task 1/2/3 各一 commit，与 spec 表一一对应。
- `DisposeObjectFilterCallback<T>`/`ObjectPoolBase`/模块级 `ObjectPoolModule.cs` 主逻辑本轮零改动（除类注释）。
- 类型一致性：Task 1 产出 `ObjectBase.SpawnCount`（public get）、`internal Spawn()/Recycle()`；Task 2 Step 3 的 XML 转换不改成员名，Task 3 README 用 `SpawnCount/IsInUse` 与 Task 1 一致。
