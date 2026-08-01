# ReferencePools 模块重构设计

> 日期：2026-08-02
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/ReferencePools/` + 全框架 130+ 调用方

## 1. 背景与问题

FuFramework 的引用池模块管理纯 C# 类对象的内存分配与回收，是全框架高频基础设施（事件参数、实体信息、声音参数、Variable 变量类、Fsm、Download 等 130+ 文件都在用）。当前实现为静态类 `ReferencePool` + 嵌套 `ReferenceCollection`，存在四类问题：

**① 代码风格违规**（违反 `Docs/代码风格规范.md`）
- 私有方法 `_CheckReferenceType` / `_GetReferenceCollection` 使用下划线 + camelCase，规范要求方法一律 PascalCase。
- `ReferenceCollection` 的计数器字段用单行 `///` 注释而非 `<summary>` XML 文档。
- `ReferencePool.cs`、`ReferenceCollection.cs` 中 `using Hotfix.Framework.Core;` 是无效引用。
- `m_FreeQueue?.Count`、`ReferenceCollectionDict?.Count` 对 readonly 非空字段做无效判空。

**② 性能隐患**
- `Release` 内 `m_FreeQueue.Contains()` 为 O(n) 线性扫描且包在锁内。
- 非泛型重载 `Acquire(Type)` / `Add(Type, int)` 等走 `Activator.CreateInstance` 反射路径，HybridCLR 热更类下偏慢。

**③ 架构问题**
- 池逻辑在静态类 `ReferencePool`，与框架其他模块（`ObjectPoolModule` 等实例化模块）风格不一致。
- `ReferencePoolModule.EnableStrictCheck` 是实例模块类上的 `static` 属性，语义别扭且带日志副作用。
- `RemoveAll` 后空集合仍留在字典中，`GetAllReferencePoolInfos` 一直显示空池（保持现状，不在本次范围）。
- `Release` 先 `Clear()` 再查重，重复释放时对象状态已丢失，报错信息不保留现场。

**④ 文档失真**
- README 依赖表列出了不存在的 `FuException`。
- README API 清单含已删除计划的非泛型重载。
- 目录结构图示格式错乱（多了一个 `├──` 空行）。

## 2. 目标

经与用户逐项确认，本次重构目标（全部选定）：

1. **代码风格规范化**：对齐《代码风格规范》。
2. **性能优化**：高频路径去除反射、优化数据结构。
3. **API/架构调整**：实例化模块，与框架其他模块风格一致。
4. **文档/注释梳理**：全量重写 README，修正关联文档。

### 2.1 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| API 兼容性 | 自由调整（可破坏） | 用户明确接受全量改调用方 |
| 池归属 | 纯实例（删除静态 `ReferencePool`） | 与 `ObjectPoolModule` 风格统一 |
| 闲置存储 | `Stack<IReference>`（LIFO） | 刚释放对象仍在 CPU 缓存，复用友好 |
| 重复释放检测 | **无条件保留** | 与现行为一致（改动最小）；反射检查已删，`Contains` 成本微秒级 |
| 非泛型反射重载 | 删除 | 全框架零调用方 |
| 严格检查配置 | 整体删除（属性/枚举/字段） | 类型校验已由泛型约束覆盖，配置失去意义 |

## 3. 目标架构

```
ReferencePools/
├── ReferencePoolModule.cs                     # 模块：状态 + 全部公开 API + 生命周期
├── ReferencePoolModule.ReferenceCollection.cs # 嵌套引用集合（partial 拆分）
├── IReference.cs                              # 不变
├── ReferencePoolInfo.cs                       # 不变（纯数据 struct）
├── EReferenceStrictCheckType.cs               # 删除
└── README.md                                  # 全量重写
```

删除 `ReferencePool.cs`、`ReferencePool.ReferenceCollection.cs`、`EReferenceStrictCheckType.cs`。

### 3.1 ReferencePoolModule 公开 API（实例化）

```csharp
public sealed class ReferencePoolModule : ModuleBase
{
    private readonly Dictionary<Type, ReferenceCollection> m_ReferenceCollectionDict = new();

    /// 池数量（当前注册的引用集合数量）
    public int Count { get; }

    protected internal override void OnDispose() { ClearAll(); }

    public T Acquire<T>() where T : class, IReference, new();
    public void Release(IReference reference);
    public void Add<T>(int count) where T : class, IReference, new();
    public void Remove<T>(int count) where T : class, IReference;
    public void RemoveAll<T>() where T : class, IReference;
    public void ClearAll();
    public ReferencePoolInfo[] GetAllReferencePoolInfos();
}
```

`ReferencePoolModule` 位于 `Hotfix.Framework.ReferencePools` 命名空间，仅依赖 Core（`ModuleBase`）与 `AOT.Framework.Core.Log`（`FuLogger`），无循环依赖。

### 3.2 删除项

| 删除 | 原因 |
|---|---|
| `ReferencePool` 静态类 | 实例化重构 |
| `Acquire(Type)` / `Add(Type,int)` / `Remove(Type,int)` / `RemoveAll(Type)` | 零调用方 + 反射慢 |
| `_CheckReferenceType` | 泛型约束 `class, IReference, new()` 编译期已保证，成死代码 |
| `ReferencePoolModule.EnableStrictCheck` 静态转发属性 | 无外部调用方 |
| `EnableStrictCheck` 属性 / `EReferenceStrictCheckType` 枚举 / `m_EnableStrictCheck` 字段 | 唯一作用（反射校验）已删，配置失去意义 |
| `[SerializeField]` 属性 | 模块经 `Activator.CreateInstance` 创建，不生效且误导 |

### 3.3 ReferenceCollection 内部实现

```csharp
private sealed class ReferenceCollection
{
    public Type RefType { get; }
    private readonly Stack<IReference> m_FreeStack = new();

    public int UsingReferenceCount { get; private set; }
    public int AcquireReferenceCount { get; private set; }
    public int ReleaseReferenceCount { get; private set; }
    public int AddReferenceCount { get; private set; }
    public int RemoveReferenceCount { get; private set; }
    public int UnusedReferenceCount => m_FreeStack.Count;

    public T Acquire<T>() where T : class, IReference, new();  // new T() 快速路径
    public void Release(IReference reference);                 // 无条件查重
    public void Add<T>(int count) where T : class, IReference, new();
    public void Remove(int count);
    public void RemoveAll();
}
```

**Release 关键顺序**（修复"先 Clear 后查重丢现场"问题）：

```csharp
lock (m_FreeStack)
{
    if (m_FreeStack.Contains(reference))   // 无条件保留，与现行为一致
        throw new InvalidOperationException($"[ReferencePoolModule] 引用实例{reference.GetType().Name}重复释放.");
    reference.Clear();                      // 查重后才 Clear，出错时保留现场
    m_FreeStack.Push(reference);
}
```

- `Clear()` 移入锁内：保证"正在清理的对象不会被并发 Acquire 取走"，修复原实现（Clear 在锁外）的潜在竞态。
- `UnusedReferenceCount` 去除无效 `?.` 判空。

### 3.4 性能结论

| 场景 | 相对现行为 |
|---|---|
| 编辑器/开发（原严格检查默认开） | **变快**：删除反射类型校验（成本大头） |
| 发布版（原严格检查默认关） | 新增一次 `Stack.Contains` O(n) 指针扫描；本框架池规模小（≤数十对象），微秒级，可忽略 |

删除配置不引入性能问题，换来所有构建统一 fail-fast。

## 4. 调用方迁移

**替换规则**（纯机械替换，覆盖全部 .cs 与关联 README）：

| 原 | 新 |
|---|---|
| `ReferencePool.Acquire<X>()` | `GlobalModule.ReferencePoolModule.Acquire<X>()` |
| `ReferencePool.Release(x)` | `GlobalModule.ReferencePoolModule.Release(x)` |
| `ReferencePool.Add/Remove/RemoveAll/ClearAll(...)` | 同规则（真实代码仅 README 用到） |

**using 处理**：
- 只调用 `Acquire/Release` 的文件：`using Hotfix.Framework.ReferencePools;` → `using Hotfix.Framework.Core;`
- 实现 `IReference` 的类：同时保留 `using Hotfix.Framework.ReferencePools;` 与新增 `using Hotfix.Framework.Core;`
- 收尾清理无效 using、核对无遗漏调用点

**执行方式**：脚本化批量替换（正则扫全部 .cs）+ 逐文件核对 using + unity-cli 编译兜底。

## 5. 生命周期时序安全（已核实）

- `ReferencePoolModule` 在 `HotfixLauncher.RegisterBaseModules()` 中注册顺序第 2（仅晚于 ConfigModule），早于 Fsm/Event/ObjectPool/Asset/UI 等所有用池模块。
- `ModuleManager.Dispose` 逆序释放 → 引用池模块倒数第二释放（仅晚于 ConfigModule），其他模块 `OnDispose` 期间池仍存活。
- 实现时需确认两个边界：ConfigModule.OnInit 是否用池；是否存在静态字段初始化时 Acquire（当前排查均无）。
- 文档补充约束：**引用池必须在 `ReferencePoolModule` 注册后方可使用**，`HotfixLauncher` 注册顺序已保证。

## 6. 文档重写

- **ReferencePools/README.md 全量重写**（~745 行）：
  - 所有示例改 `GlobalModule.ReferencePoolModule.Xxx`。
  - 删除非泛型重载、严格检查配置（`EReferenceStrictCheckType`）相关章节。
  - 架构图 `Queue` → `Stack`；工作流图移除"检查类型 (EnableStrictCheck)"步骤。
  - 修正依赖表（移除不存在的 `FuException`，Core 提供 `ModuleBase`）。
  - 修复目录结构图示格式错乱。
  - 补充"必须先注册模块"约束说明。
- **顺带修正** Event/FSM/ObjectPool 等 README 中残留的 `ReferencePool.` 示例引用。

## 7. 验证方式

1. 脚本化迁移 → 2. unity-cli 触发 Unity 编译 → 3. 修复残留编译错误 → 4. 编辑器 Play 冒烟（框架能正常启动）。

## 8. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`refactor:` 模块重构 + 调用方迁移（两者必须同落，保证编译通过）。
- **Commit 2**：`docs:` ReferencePools README 重写与关联文档修正。
