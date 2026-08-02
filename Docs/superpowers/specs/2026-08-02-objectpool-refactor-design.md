# ObjectPool 模块重构设计

> 日期：2026-08-02
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/ObjectPool/`

## 1. 背景与问题

FuFramework 对象池模块管理 Unity 端 GameObject 对象的创建、销毁与复用，减少 Instantiate/Destroy 开销。模块由 `ObjectPoolModule`（964 行）+ 嵌套 `ObjectPool<T>`（533 行）+ `Object<T>` 包装 + `ObjectPoolBase`/`ObjectBase` 基类 + `ObjectInfo`/`ReleaseObjectFilterCallback` 结构体组成。

**真实调用方仅 2 处**：`Entity/EntityGroup.cs`（`ObjectPool<EntityInstanceObject>`）与 `UI/UIModule.cs`（`CreateObjectPool<WinObject>("UIWinObjectPool")`）。ObjectBase 子类仅 `EntityInstanceObject`、`WinObject`。其余约 20 个 `CreateObjectPool` 重载、Type/Predicate 查询 API 均无调用方。

存在问题：

**① 逻辑正确性**
- **autoReleaseInterval 默认值语义耦合**：约 20 个重载调用 `_CreateObjectPool` 时，autoReleaseInterval 参数位填 `DefaultExpireTime`（float.MaxValue）。若将来修改 `DefaultExpireTime` 常量，自动释放间隔将跟着变化——两个语义不同的参数耦合同一常量，隐患。

**② 性能**
- **O(n²) 冒泡排序**：`_DefaultReleaseObjectFilterCallback` 第二阶段用内嵌双重循环选"优先级最低、最久未用"对象，每次自动释放 O(n²)。
- **Type 版创建走反射**：`typeof(ObjectPool<>).MakeGenericType` + `Activator.CreateInstance`，每次运行时创建对象池有反射开销。

**③ 代码风格**（违反 `Docs/代码风格规范.md`）
- 8 个私有方法用下划线+camelCase（`_HasObjectPool` 等），规范要求 PascalCase。
- `ObjectPoolModule.cs` 964 行超 500 行规范。
- 多处单行 `///` 注释非 XML `<summary>`。
- `ObjectInfo.cs` 的 `// Preserve ... for Unity's serialization` 行注释冗余（struct 无需序列化保护）。

**④ 文档**：README 与代码 API 不一致（含已删除/待精简的重载）。

## 2. 目标

经用户确认，本次优化目标（全部选定）：

1. **代码风格规范化**：对齐《代码风格规范》。
2. **性能优化**：O(n²) 排序改 List.Sort、消除反射创建。
3. **逻辑正确性**：修复 autoReleaseInterval 默认值语义耦合。
4. **文档/注释梳理**：重写 README、清理冗余注释。

### 2.1 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| API 兼容性 | 自由调整 | 真实调用方仅 Entity/UI 两处 |
| Type 版重载 | 删除 | 零调用方 + 反射 |
| CreateObjectPool 重载 | 精简到 4 个 | 其余零调用方 |
| 结构 | 保留嵌套 `ObjectPool<T>` 于模块内 | 与 ReferencePool 重构风格一致 |
| 核心 API | `Spawn`/`Recycle`/`Register` 等签名不变 | Entity/UI 调用方零改动 |

## 3. 目标架构

### 3.1 文件布局（拆分超 500 行文件）

```
ObjectPool/
├── ObjectPoolModule.cs                  # 模块：字段 + 生命周期 + 低内存回调 + 释放
├── ObjectPoolModule.Query.cs            # 查询对象池（原 #region 获取对象池）
├── ObjectPoolModule.PoolCreation.cs     # 创建/销毁对象池（原 #region 创建/销毁 + 私有方法）
├── ObjectPoolModule.ObjectPool.cs       # ObjectPool<T> 具体池（保留，改排序）
├── ObjectPoolModule.Object.cs           # Object<T> 内部包装（保留）
├── Base/ObjectPoolBase.cs               # 不变
├── Base/ObjectBase.cs                   # 不变
├── Misc/ObjectInfo.cs                   # 清理冗余注释
├── Misc/ReleaseObjectFilterCallback.cs  # 不变
└── README.md                            # 重写
```

### 3.2 CreateObjectPool 精简为 4 个泛型重载

```csharp
public ObjectPool<T> CreateObjectPool<T>(bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(string poolName, bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime = float.MaxValue, bool allowSpawnInUse = false) where T : ObjectBase;
public ObjectPool<T> CreateObjectPool<T>(string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase;
```

内部统一收敛到私有 `CreateObjectPoolInternal<T>`（泛型，无反射）。

### 3.3 删除项

| 删除 | 原因 |
|---|---|
| 全部 `CreateObjectPool(Type, ...)` 重载（~16 个） | 零调用方 + 反射创建 |
| 私有 `_CreateObjectPool(Type, ...)`（`MakeGenericType` + `Activator.CreateInstance`） | 连带消除全部反射 |
| `HasObjectPool(Type)` / `GetObjectPool(Type)` / `GetObjectPool(Predicate)` / `GetObjectPools(Predicate...)` | 零调用方 |
| `ObjectInfo.cs` 的 `// Preserve ...` 行注释 | 结构体无需 Unity 序列化保护 |

### 3.4 保留项

- `ObjectPool<T>` 泛型 API：`Spawn` / `Recycle` / `Register` / `ReleaseObject` / `Release` / `ReleaseAllUnused` / `CanSpawn` / `SetLocked` / `SetPriority` / `GetAllObjectInfos` + 属性（Entity/UI 调用方零改动）
- `DestroyObjectPool<T>` / `DestroyObjectPool<T>(string)` / `DestroyObjectPool(ObjectPoolBase)` / `DestroyObjectPool<T>(ObjectPool<T>)`
- `HasObjectPool<T>` / `GetObjectPool<T>`（泛型）/ `GetAllObjectPools` 系列
- `Release()` / `ReleaseAllUnused()` / `Count` / 低内存回调

## 4. 性能优化

**O(n²) 冒泡 → List.Sort**（`ObjectPool<T>.DefaultReleaseObjectFilterCallback`）

```csharp
// 第一阶段：剔除过期对象（保留现有逻辑）
if (expireTimeThreshold.HasValue)
{
    for (var i = candidateObjects.Count - 1; i >= 0; i--)
        if (candidateObjects[i].LastUseTime <= expireTimeThreshold.Value)
        {
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
    m_CachedToReleaseObjectList.Add(candidateObjects[i]);
```

语义与原冒泡完全一致（优先级最低优先释放；同优先级取最久未用）。`candidateObjects` 是池字段 `m_CachedCanReleaseObjectList`，下次 Release 会重新填充，排序改动无副作用。

## 5. 逻辑修复：autoReleaseInterval 默认值

新增独立常量 `DefaultAutoReleaseInterval = float.MaxValue`（默认不自动释放），替换所有重载中 autoReleaseInterval 参数位误用的 `DefaultExpireTime`，消除语义耦合。

## 6. 风格规范化

| 项 | 处理 |
|---|---|
| 8 个下划线私有方法 | 改 PascalCase：`HasObjectPoolInternal` / `GetObjectPoolInternal` / `CreateObjectPoolInternal` / `DestroyObjectPoolInternal` / `GetObject` / `GetCanReleaseObjects` / `DefaultReleaseObjectFilterCallback` / `ObjectPoolComparer` |
| 缺 `<summary>` 成员 | 补全 XML 注释（`Count`、`m_CachedObjPoolList`、模块单行 `///` 成员等） |
| `ObjectPoolModule.cs` 964 行 | 按 region 拆 3 个 partial |

## 7. 文档重写

- **ObjectPool/README.md**：更新 API 清单（删 Type 版、CreateObjectPool 精简到 4 个）、修正 autoReleaseInterval / capacity / expireTime 参数说明与默认值、更新目录结构与依赖表。
- 关联模块 README 中 `ObjectPool` 相关引用同步核对（Entity/UI 文档）。

## 8. 验证方式

1. 编译零错误（用户手动编译）。
2. Play 冒烟：UI/Entity 走对象池路径，正常启动无异常。

## 9. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`refactor:` API 精简（删 Type 版）+ 拆分 partial + 风格规范化
- **Commit 2**：`perf:` 排序优化 + autoReleaseInterval 常量修复
- **Commit 3**：`docs:` README 重写

每个 commit 前征得用户同意；提交时只 add 本任务相关文件，不波及工作区其他未提交改动。
