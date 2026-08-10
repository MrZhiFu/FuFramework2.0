# Config 模块重构设计

> 日期：2026-08-11
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Scripts/Hotfix/Framework/Config/` + `Unity/Assets/Editor/FuFramework/Config/`

## 1. 背景与问题

Config 模块（配置表管理系统，含 `ConfigModule` / `IDataTable` / `BaseDataTable`）当前存在三类问题：

**① 结构问题**
- `ConfigModule.cs` 单文件混合 public API / 私有状态 / 生命周期，未拆「主文件 + API 分部」，与本分支 AssetModule、ReferencePool 已完成的「`XxxModule.cs` + `XxxModule.API.cs`」模式不一致。

**② 性能与冗余**
- `BaseDataTable` 用 `SortedDictionary<long,T>` / `SortedDictionary<string,T>` 双索引，查询 O(log n)；配置为加载后只读数据，普通 `Dictionary`（O(1)）更合适。
- `ConfigModule.m_CfgNameTypeDict`（`ConcurrentDictionary<Type,string>`）只是缓存 `typeof(T).Name`，属无效缓存。
- `GetConfig<T>()` 走「`HasConfig` → `GetTypeName` → `GetConfig`」三次字典操作，存在冗余查询。
- `ConfigModule.m_CfgDataDict` 用 `ConcurrentDictionary`，但实际配置只在启动期一次性加载、之后只读，读取路径可用普通 `Dictionary` 加速。

**③ 语义缺陷**
- `AddConfig` 静默忽略重复添加、返回 `void`，无任何反馈。
- `CfgNames` 返回 `m_CfgDataDict.Keys` 活视图，外部遍历期间若字典变更会产生弱一致性问题。
- `GetConfig(string)` 等 string 版本接口对空名无卫语句，静默返回。
- README 宣称「配置数据读取通过 ConcurrentDictionary 保证线程安全」，但 `BaseDataTable` 内部 `List/SortedDictionary` 并无锁，表述过度承诺。

## 2. 目标与约束

### 2.1 目标

1. **对齐模块统一模式**：`ConfigModule` 拆出 `ConfigModule.API.cs` 分部，对齐 ReferencePool / AssetModule。
2. **优化数据结构与性能**：`SortedDictionary` → `Dictionary`，删除无效类型名缓存，收敛冗余查询。
3. **修正接口语义缺陷**：`AddConfig` 返回 `bool` + 告警、`CfgNames` 返回快照、string 接口空名卫语句、README 只读契约。
4. **契约加固**：`All` / `ToArray` 复用实现、`FuGuard` / `FuException` 卫语句、README 补充「名称 = 类名」约束。
5. **新增只读调试面板**：仿 ReferencePoolModuleWindow，提供配置查询调试能力。

### 2.2 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| 零调用接口（`Find`/`Max`/`Min`/`Sum`/`ToArray`/`FirstOrDefault` 等） | **保留** | 有意保留作为框架通用能力，本次不删 API |
| Luban 生成代码契约 | **仅框架侧，不动生成器** | 生成代码只访问 `DataList`/`LongKeyDataDict`/`StrKeyDataDict` 三个 protected 字段，改内部容器类型不破坏契约；Luban 无需重建/重生成 |
| ConfigModule 存储模型 | **普通 Dictionary + 只读契约** | 配置仅启动期一次性加载、之后只读，无运行时重载路径；读取更快 |
| 调试窗口移除类功能 | **不包含** | 面板定位为只读查询工具 |
| 调试窗口功能 | 行数据浏览器 + 行搜索 + 加载信息统计 | 用户选定，不包含「复制为文本」 |
| 验证方式 | **用户手动编译后反馈** | 不主动跑 unity-cli 编译 |

## 3. 目标架构

```
Scripts/Hotfix/Framework/Config/
├── ConfigModule.cs              # [改] 私有字段 + 生命周期(OnInit/OnDispose)，不再含 public API
├── ConfigModule.API.cs          # [新增] 全部 public 成员（partial class ConfigModule）
├── BaseDataTable.cs             # [改] SortedDictionary → Dictionary，仅容器类型变化
├── IDataTable.cs                # [不变] 全部接口成员保留
└── README.md                    # [改] 同步接口清单与只读契约

Editor/FuFramework/Config/
├── ConfigImporter.cs            # [不变] 已有导表工具
└── ConfigModuleWindow.cs        # [新增] 配置调试面板
```

### 3.1 分部归属约定（对齐 ReferencePool）

- 主文件 `ConfigModule.cs`：`m_CfgDataDict` 字段 + `OnInit` / `OnDispose` 生命周期。
- API 分部 `ConfigModule.API.cs`：`Instance` 属性、`Count`、`CfgNames`、`GetConfig<T>` / `GetConfig(string)`、`HasConfig<T>` / `HasConfig(string)`、`AddConfig`、`RemoveConfig<T>` / `RemoveConfig(string)`、`RemoveAllConfigs`。

## 4. ConfigModule 语义修正

### 4.1 ConfigModule.cs（主文件）

```csharp
public sealed partial class ConfigModule : ModuleBase
{
    /// <summary>
    /// 配置表字典。key为配置表名称，value为配置表数据。
    /// 配置在启动期一次性加载，加载后只读，故使用普通 Dictionary。
    /// </summary>
    private readonly Dictionary<string, IDataTable> m_CfgDataDict = new(StringComparer.Ordinal);

    /// <summary>
    /// 初始化。
    /// </summary>
    protected internal override void OnInit()
    {
        Instance = this;
        m_CfgDataDict.Clear();
    }

    /// <summary>
    /// 释放。
    /// </summary>
    protected internal override void OnDispose()
    {
        RemoveAllConfigs();
        Instance = null;
    }
}
```

### 4.2 ConfigModule.API.cs（新增）

| 成员 | 语义修正 |
|---|---|
| `Instance` | 移至 API 分部（public 属性归 API） |
| `Count` | 不变，`m_CfgDataDict.Count` |
| `CfgNames` | `IEnumerable<string>` → `string[]` 快照（`Keys.ToArray()`），外部遍历不弱一致 |
| `GetConfig<T>()` | **单次查询**：`var cfg = GetConfig(typeof(T).Name); return cfg == null ? default : (T)cfg;` 消除 HasConfig 预检 + 双 GetTypeName 冗余 |
| `GetConfig(string)` | 首行 `FuGuard.NotNullOrEmpty(cfgName, nameof(cfgName))`，空名抛 `FuException` |
| `HasConfig<T>` / `HasConfig(string)` | 加空名卫语句 |
| `AddConfig` | `void` → **`bool`**：`FuGuard.NotNull(cfgValue)`；重复时 `FuLogger.LogWarning` 并返回 `false`；成功 `true` |
| `RemoveConfig<T>` / `RemoveConfig(string)` | 加空名卫语句，其余不变 |
| `RemoveAllConfigs` | 不变 |

删除项：`m_CfgNameTypeDict` 字段、`GetTypeName<T>()` 私有辅助。

### 4.3 破坏性检查

| 变更 | 调用方影响 |
|---|---|
| `AddConfig` `void → bool` | 唯一调用方 `TableManager` 忽略返回值，兼容 |
| `CfgNames` `IEnumerable<string> → string[]` | `string[]` 实现 `IEnumerable<string>`，源码兼容；零外部调用方 |
| `GetConfig(string)` / `RemoveConfig(string)` 空名抛异常 | 外部调用全走泛型重载，无真实调用方受影响 |
| `GetConfig<T>()` 查询路径优化 | 语义保持（缺失返回 `default`） |

## 5. BaseDataTable 数据结构优化

- `LongKeyDataDict`：`SortedDictionary<long,T>` → **`Dictionary<long,T>`**
- `StrKeyDataDict`：`SortedDictionary<string,T>` → **`Dictionary<string,T>`**
- `DataList`：`List<T>` 不变
- 查询路径 O(log n) → **O(1)**；无任何外部/生成代码依赖排序迭代（protected 字段仅生成代码访问，且只做 `Clear` / `Add`，契约兼容）
- `All` / `ToArray()` 复用：`public T[] All => ToArray();`（`ToArray` 为唯一拷贝实现）
- 其余成员（`Get` / `Find` / `FindList` / `ForEach` / `Max` / `Min` / `Sum` / `FirstOrDefault` / `LastOrDefault` / 索引器 / `ToList`）**全部保留不动**
- `IDataTable.cs` **零改动**

## 6. ConfigModuleWindow 调试面板

- **文件**：`Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`，命名空间 `FuFramework.Config.Editor`，`#if UNITY_EDITOR` 包裹。
- **入口**：`[MenuItem("FuFramework/调试/配置调试面板")]`，仅 Play 模式可用。
- **访问方式**：反射 `Type.GetType("Hotfix.Framework.Config.ConfigModule, Hotfix")`，经静态 `Instance` 属性取热更实例（ConfigModule 有单例，无需 `ModuleManager.GetModule`）。
- **纯只读**：无任何移除/修改按钮。

### 6.1 界面结构（三层展开）

```
┌─ 工具栏：表名搜索框 | 自动刷新(0.5s节流) | 刷新按钮
├─ 模块概览：配置表总数 N
└─ 配置表列表（Foldout，按表名搜索过滤）
   ├─ 表名（青色高亮）+ 数据行数 + 加载状态
   ├─ [展开] 加载信息统计行：类型名 | long key 数量 | string key 数量（反射 protected
   │          字段 LongKeyDataDict/StrKeyDataDict → IDictionary.Count；仅 string key → 判为本地化表）
   ├─ [展开] 表内搜索框（字段值模糊过滤）
   └─ [展开] 行数据列表（最多显示前 200 行，超出提示"…"）
      └─ 每一行：Foldout（显示 Id 值或序号）
         └─ [展开] 反射该行全部公共属性/字段 → 键值对展示
```

### 6.2 关键技术点

- **行数据获取**：反射调用 `IDataTable<T>.All` 属性（`T[]`），仅对**展开**的表执行；结果按表名缓存（`Dictionary<string, object[]>`）。配置加载后只读，缓存安全，避免自动刷新时重复枚举。
- **行搜索**：在展开表的行数组内，逐行反射各字段值 `.ToString().Contains(query, OrdinalIgnoreCase)` 过滤；空查询显示全部。
- **反射缓存**：复用 ReferencePool 的 `EnsureReflection` / `ResetReflection` 双方法模式；非 Play 模式重置缓存并提示，避免持有失效热更实例。
- **行 Id 显示**：优先反射取 `Id` / `Key` 属性，缺失则退化为行序号，保证通用性。

## 7. README 更新

同步 `Unity/Assets/Scripts/Hotfix/Framework/Config/README.md`：

1. **接口清单同步**：`AddConfig` 返回 `bool`；`CfgNames` 返回 `string[]`；新增目录结构 `ConfigModule.API.cs`。
2. **线程安全表述修正**：「配置数据读取通过 ConcurrentDictionary 保证线程安全」→ **「加载期单线程注册，加载后只读（load-once read-only）」**。
3. **新增约束说明**：「配置表名称 = 类名」（`typeof(T).Name` / `nameof(TbXxx)` 必须一致）。
4. **重复添加行为更新**：`AddConfig` 重复同名 → 返回 `false` 并 `FuLogger.LogWarning` 告警。
5. **新增调试面板章节**：`FuFramework/调试/配置调试面板`，说明为只读调试工具。

## 8. 验证方式

**用户手动编译后反馈**（本设计验证不主动跑 unity-cli 编译）。

1. **编译**：用户手动触发 Unity 编译，无错误。
2. **Play 冒烟**：框架正常启动、`LoadConfigAsync` 配置加载成功。
3. **面板验证**：Play 模式下打开「配置调试面板」，验证表列表、行数、加载信息、行展开（字段键值对）、行搜索过滤正常。
4. **残留复核**：`SortedDictionary` / `ConcurrentDictionary` / `GetTypeName` 在 Config 目录无残留。
5. **调用方复核**：`AddConfig` 返回值变化仅 TableManager（忽略返回值）；`CfgNames` 类型变化零调用方。

## 9. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`[AI]refactor: ConfigModule 拆分 API 分部，SortedDictionary→Dictionary，语义修正（AddConfig 返回 bool/告警、CfgNames 快照、空名卫语句），新增 ConfigModuleWindow 调试面板`（框架侧重构 + Editor 窗口，同落保证编译通过）。
- **Commit 2**：`[AI]docs: 同步 ConfigModule README 接口清单与只读契约`。
