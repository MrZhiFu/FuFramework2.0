# ObjectPool 模块优化重构设计

> 日期：2026-08-05
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/`
> 前置：`2026-08-02-objectpool-refactor-design.md` 已完成（API 精简、partial 拆分、List.Sort、常量解耦），随后完成 `Spawn→Get`、`Release→Dispose/Recycle` 重命名

## 1. 背景与问题

对象池模块在 08-02 重构后已具备：精简后的公开 API、拆分的 partial、List.Sort 排序、`DefaultAutoDisposeInterval` 常量、统一的 Get/Recycle/Dispose 命名。真实调用方仅 `Entity/EntityGroup.cs` 与 `UI/UIModule.cs` 两处。

**本次目标：消除模块复杂度与冗余代码**。经用户确认的核心冗余点：

**① `ObjectPoolModule.Object<T>` 包装类是最大冗余**
- `ObjectBase` 已持有 `Name/Target/Locked/Priority/LastUseTime/CustomCanDisposeFlag` 且实现 `IReference`
- `Object<T>` 只是**纯代理转发**这些属性，额外仅多一个 `SpawnCount` 计数
- 更严重的是**双重回收**：池 `OnDispose()`/`DisposeObject()` 时，`ObjectBase` 和 `Object<T>` 各自都要 `Recycle` 进引用池一次（2 次 `lock` + 2 次 `Stack.Contains` O(n) + 2 次 `Clear` + 2 次 `Push`）
- 筛选回调 `DisposeObjectFilterCallback<T>` 本就操作底层 `T`（`GetCanDisposeObjects` 早已 `results.Add(obj.TargetObject)`），证明包装层纯属字典实现细节

**② 重载冗余**：`GetAllObjectPools` 4 个重载（array/List × 有无 bool 排序），仅模块内部使用 `GetAllObjectPools(bool, List)` 无分配版

**③ 文件超限**：`ObjectPoolModule.ObjectPool.cs` 573 行，超过《代码风格规范》§4.1 的 500 行上限

**④ 文档瑕疵**：
- `ObjectPoolModule.cs:13` 类注释"创建、获取、销毁和销毁"中"销毁"重复
- `ObjectBase.cs` 属性用单行 `///` 注释（非 XML 多行 `<summary>`）
- `Object<T>.GetObject(object target)` 的 `<param>`/`<returns>` 为空
- `GetCanDisposeObjects` 的 `<summary>` 写"数量"实际是填充列表
- `m_AutoDisposeInterval` 私有字段注释误用属性措辞"获取或设置..."

## 2. 目标与已确认决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| `Object<T>` 包装类 | **并入 ObjectBase** | SpawnCount 上移、池直接存 T、删整个文件、单次回收 |
| 双重回收 | 改**单次回收** | 严格更少操作（锁/Contains/Clear/Push/Acquire 全部减半），无性能回归 |
| 两个字典 | **都保留**，但 T 重载绕过 target 字典 | 分别是 name/target 两条热路径的 O(1) 索引，缺一则 O(n)；T 重载手上已有 T 无需再查字典 |
| API 精简范围 | **中等**：只删明显冗余 | 仅 `GetAllObjectPools` 4→2；其余零调用但常规 API 保留 |
| 公开 API | **签名零改动** | Entity/UI 调用方零修改 |
| 命名 | `SpawnCount`/`OnSpawn`/`OnRecycle`/`allowSpawnInUse` 保留 | 与上轮 Get/Recycle 确立的语义一致，本轮不重命名 |

**性能结论（经仔细评估）**：双重→单次回收在锁次数、`Stack.Contains` O(n) 扫描、`Clear`、`Push`、注册时 `Acquire`、每对象内存六个维度全部只减不增；两回收是**不同类型、不同池、互不共享**，删 wrapper 不影响 T 的池；热路径 `Get/Recycle` 只动计数不碰引用池，不受影响。唯一多花成本是 `ObjectBase.Clear()` 多重置一个 int。

## 3. 目标架构

### 3.1 `ObjectBase.cs` 新增成员（从 `Object<T>` 迁入）

```csharp
/// <summary>
/// 获取对象池中对象的获取计数（引用计数）。
/// </summary>
public int SpawnCount { get; private set; }

/// <summary>
/// 获取对象是否正在使用中。
/// </summary>
public bool IsInUse => SpawnCount > 0;

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

- `_Initialize(...)` 末尾防御性 `SpawnCount = 0;`
- `Clear()` 增加 `SpawnCount = 0;`
- 原 `ObjectBase.cs` 属性单行 `///` 注释（第 16-32 行）改为 XML `<summary>` 多行

### 3.2 删除 `ObjectPoolModule.Object.cs`（含 `.meta`）

### 3.3 `ObjectPool<T>` 内部改存 `T`

| 成员 | 现值 | 改后 |
|---|---|---|
| `m_ObjectMultiDict` | `FuMultiDictionary<string, Object<T>>` | `FuMultiDictionary<string, T>` |
| `m_TargetObjectDict` | `Dictionary<object, Object<T>>` | `Dictionary<object, T>` |
| `GetObject(object)` | 返回 `Object<T>` | 返回 `T` |
| `GetCanDisposeObjects(List<T>)` | `results.Add(obj.TargetObject)` | `results.Add(obj)` |

**行为等价的关键改写**（语义不变）：

- `Register(T obj, bool spawned)`：入两字典后 `if (spawned) obj.Spawn();`（替代原 `Object<T>.Create` 的置计数+OnSpawn；附带 LastUseTime 刷新，更正确）
- `Get(name)` / `CanGet(name)`：遍历 range 直接判 `obj.IsInUse`
- `Recycle(object target)`：`GetObject(target).Recycle();` + 容量检查
- `DisposeObject(object target)`：校验 → 移出两字典 → `try { obj.OnDispose(); } finally { ReferencePoolModule.Recycle(obj); }`（**单次回收 + 异常兜底**）
- `ObjectPool.OnDispose()`：foreach t → `try { t.OnDispose(); } catch (Exception e) { LogWarning; } finally { ReferencePoolModule.Recycle(t); }` → 清空字典（**单次回收**）
- `GetAllObjectInfos`：直接 `new ObjectInfo(t.Name, t.Locked, t.CustomCanDisposeFlag, t.Priority, t.LastUseTime, t.SpawnCount)`

### 3.4 T 重载绕过 target 字典

`Recycle(T obj)` 不再委托 `Recycle(object)` 走字典查找，直接操作 T：

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
```

`SetLocked(T)` / `SetPriority(T)` / `DisposeObject(T)` 同理。target 字典只服务真正只有 target 的 object 重载。

### 3.5 无影响部分

- `ObjectPoolModule.cs` / `Query.cs` / `PoolCreation.cs`：不涉及（操作 `ObjectPoolBase` 层面）
- `DisposeObjectFilterCallback<T>` 签名不变
- Entity/UI 调用方零改动

## 4. API 精简（中等）

**唯一改动：`GetAllObjectPools` 4 重载 → 2**：

```csharp
public ObjectPoolBase[] GetAllObjectPools(bool sort = false);
public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results);
```

- 内部 `GetAllObjectPools(true, m_CachedObjPoolList)`（无分配版）保留
- `GetAllObjectPools()` 调用仍编译通过（默认参数）
- **保留**：`Has/GetObjectPool`、`DisposeObjectPool` ×4、`GetAllObjectInfos`、`CanGet` ×2、`CreateObjectPool` ×5、`Recycle/SetLocked/SetPriority/DisposeObject` 的 T+object 对

## 5. 文件拆分（满足 500 行规范）

`ObjectPool<T>` 改 `partial`（嵌套 partial 合法），拆两文件：

| 文件 | 内容 | 预估行数 |
|---|---|---|
| `ObjectPoolModule.ObjectPool.cs` | 类声明、字段、构造函数、属性、`Register/Get/Recycle/CanGet/SetLocked/SetPriority/GetAllObjectInfos`、`GetObject` | ~360 |
| `ObjectPoolModule.ObjectPool.Dispose.cs`（新） | `Update`、`OnDispose`、`Dispose`×3、`DisposeAllUnused`、`DisposeObject`×2、`GetCanDisposeObjects`、`DefaultDisposeObjectFilterCallback` | ~230 |

新增文件补 `.meta`。

## 6. 文档修复

1. `ObjectPoolModule.cs:13` —— "创建、获取、销毁和销毁" → "创建、获取、销毁"
2. `ObjectBase.cs:16-32` —— 单行 `///` 注释改 XML `<summary>` 多行
3. `ObjectPool<T>.GetObject(object)` —— 空 `<param>`/`<returns>` 补全
4. `GetCanDisposeObjects` —— `<summary>` 改为"获取对象池中能被销毁的对象（填充到结果列表）"
5. `m_AutoDisposeInterval` 私有字段注释 —— 改字段措辞
6. **README 同步**：目录结构删 `ObjectPoolModule.Object.cs`、加 `ObjectPoolModule.ObjectPool.Dispose.cs`；ObjectBase 补 `SpawnCount/IsInUse`；`GetAllObjectPools` 重载数改 2

## 7. 验证方式

1. **编译门禁**：每个 commit 前由用户手动编译，确认零错误（不使用 unity-cli）。
2. **残留引用核查**：`grep -rn "Object<T>\|TargetObject" Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/` 预期零命中。
3. **调用方核查**：`grep -rn "GetAllObjectPools" Unity/Assets/Scripts/Hotfix --include='*.cs'` 预期仅模块内部使用；Entity/UI 调用不受影响。
4. **Play 冒烟**：UI/Entity 走对象池路径正常启动无异常。

## 8. 提交拆分（遵循 `Docs/Git提交规范.md`，每个 commit 前征得用户同意）

| Commit | 类型 | 内容 |
|---|---|---|
| 1 | `refactor:` | 合并 `Object<T>` → ObjectBase（SpawnCount/IsInUse/Spawn/Recycle 迁入），池改存 T，T 重载绕过 target 字典，删 `Object.cs`，`ObjectPool<T>` 拆 partial 两文件 |
| 2 | `refactor:` | `GetAllObjectPools` 4→2 重载 + 文档修复（模块类注释、ObjectBase XML 注释、`GetObject` 空 param、`GetCanDisposeObjects` summary、字段注释） |
| 3 | `docs:` | README 同步（目录结构、ObjectBase 新增成员、重载数） |

只 add 本任务相关文件，不波及工作区其他未提交改动。
