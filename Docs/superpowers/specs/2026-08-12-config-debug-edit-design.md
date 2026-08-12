# ConfigModuleWindow 实时编辑配置内存数据设计

> 日期：2026-08-12
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`（单文件）

## 1. 背景与问题

`ConfigModuleWindow` 调试面板当前为纯只读：可展开配置表查看行数据、行搜索、加载统计，但无法修改配置数据。调试时需要临时改动某个配置值（如道具价格、UI 时长、声音路径）并立即观察游戏行为变化，只能改配表重导或写死代码，成本高。

需求：在面板中**实时编辑配置表行内简单值字段的内存数据**，编辑即生效（游戏代码下次读取即见新值），不持久化、不重载。

## 2. 目标与约束

### 2.1 目标

1. 行展开后，可编辑行内**简单值字段**（`int`/`long`/`float`/`double`/`bool`/`string`/`enum`）的内存数据。
2. 编辑**实时生效**：直接写活配置对象（行数据来自 `IDataTable<T>.All`，为 `DataList` 中同一批对象引用）。
3. **字段级撤销**：首次编辑缓存原值，提供「重置」按钮一键还原。
4. **编辑可视化**：已编辑字段黄色高亮；写回失败红色提示。
5. **仅窗口内实现**：零 Hotfix 改动、零新增 API。

### 2.2 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| 可编辑字段范围 | **全部简单值字段**（int/long/float/double/bool/string/enum） | 用户选定 |
| 身份字段 | **Id/Key 锁定不可编辑** | 修改会破坏 `LongKeyDataDict`/`StrKeyDataDict` 索引 |
| 复杂/引用字段 | **只读展示**（跨表引用、List、数组、decimal、char 等） | 防御性，编辑复杂类型风险高 |
| 撤销机制 | **字段级撤销**（缓存原值 + 「重置」按钮） | 用户选定，自包含、不新增 API |
| 持久化 | **不持久化**（仅内存实时修改） | 需求明确为实时改内存 |
| 解析失败警示 | **适配为写回失败警示** | Unity 类型化数值控件（IntField 等）内建校验：无效输入不提交、控件内保留输入，故不存在非法提交；改为对 `SetValue` 抛异常时红色提示 |
| 验证方式 | **用户手动编译** | 既有约定 |

## 3. 架构

仅修改 `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`：

```
ConfigModuleWindow.cs（现有，+ 编辑能力）
├── 状态
│   ├── m_FieldOriginalValues  # 新增：撤销缓存（行引用 → 属性名 → 原值）
│   └── （现有 caches）
├── 绘制
│   ├── DrawRow            # 修改：逐字段分发到可编辑控件或只读文本
│   └── DrawEditableField  # 新增：渲染控件 + 写回 + 高亮 + 重置按钮
├── 判定
│   └── IsEditableProperty # 新增：setter + 简单类型 + 非 Id/Key
└── ResetReflection        # 修改：补清 m_FieldOriginalValues
```

数据流：`DrawRow` → 读 `prop.GetValue(row)` → 若可编辑渲染控件 → 控件返回新值 → 记录原值（首次）→ `prop.SetValue(row, newValue)` → 内存配置对象即时变更 → 下次 Repaint/游戏读取即见新值。

### 3.1 可编辑判定 `IsEditableProperty(PropertyInfo prop)`

- `prop.GetSetMethod(true) != null`（有 setter；Luban 生成的 `private set;` 反射 `SetValue` 可写，Editor Mono 下成立）。
- `prop.PropertyType` ∈ { `string`, `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, 任意 enum }。
- `prop.Name` ∉ { `Id`, `Key` }。

### 3.2 控件映射（UnityEditor 类型化控件，内建校验）

| 类型 | 控件 | 提交时机 |
|---|---|---|
| `string` | `EditorGUILayout.TextField` | 输入即提交 |
| `bool` | `EditorGUILayout.Toggle` | 点击即提交 |
| enum | `EditorGUILayout.EnumPopup` | 选择即提交 |
| `int` | `EditorGUILayout.IntField` | 有效输入提交 |
| `long` | `EditorGUILayout.LongField` | 有效输入提交 |
| `float` | `EditorGUILayout.FloatField` | 有效输入提交 |
| `double` | `EditorGUILayout.DoubleField` | 有效输入提交 |
| 其余整型族（byte/short/uint 等） | `IntField`/`LongField` + 类型转换 | 有效输入提交 |

> 说明：数值控件返回恒为最后一个有效值，无效输入不产生提交，故无需手动 parse 与解析错误态。

## 4. 详细设计

### 4.1 新增状态字段

```csharp
/// <summary>
/// 字段编辑撤销缓存：行对象引用 → 属性名 → 原始值。
/// 首次编辑某字段时记录原值，供「重置」回滚与「编辑高亮」判定。
/// 行是活配置对象（Luban bean 未重写 Equals，引用相等作 key 安全）。
/// </summary>
private readonly Dictionary<object, Dictionary<string, object>> m_FieldOriginalValues = new();
```

### 4.2 `DrawRow` 修改

遍历属性时：读取 `value`（`GetValue` 抛异常则按只读渲染错误串并跳过编辑分支）→ 若 `IsEditableProperty(prop)` 走 `DrawEditableField(row, prop, value)`；否则保持现有只读文本展示。

### 4.3 `DrawEditableField` 流程

1. 判断当前是否已编辑：`m_FieldOriginalValues.TryGetValue(row, out var orig) && orig.ContainsKey(prop.Name)`。
2. 已编辑 → `GUI.color = Color.yellow` 包裹控件渲染（编辑高亮）。
3. 按 `prop.PropertyType` 渲染对应控件，得到控件返回新值。
4. 新值 ≠ 当前值（`!Equals`）：
   - 未记录原值 → 记录**编辑前的值**：`(orig ??= (m_FieldOriginalValues[row] = new Dictionary<string, object>()))[prop.Name] = value;`
   - `try { prop.SetValue(row, newValue); } catch (Exception e) { Debug.LogError($"[ConfigDebug] 写入 {prop.Name} 失败：{e.Message}"); 该字段红色标注；撤销缓存不变 }`
5. 已编辑字段的控件行尾渲染「重置」小按钮：
   - `prop.SetValue(row, orig[prop.Name]);` → `orig.Remove(prop.Name);`（orig 空则移除行条目）→ 值还原、高亮消失。

### 4.4 边界与错误处理

- **Id/Key 锁定**：`IsEditableProperty` 排除，只读文本展示。
- **复杂/只读属性**：无 setter 或类型不在可编辑集合 → 只读文本（含跨表引用 `TbXxx`、`List<T>`、数组、`decimal`、`char` 等）。
- **读取失败**：`GetValue` 抛异常 → 渲染错误串，不进入编辑分支。
- **写回失败**：`SetValue` 抛异常 → `Debug.LogError` + 字段红色标注 + 保持旧值；撤销缓存不变。
- **ResetReflection**：退出 Play 时新增 `m_FieldOriginalValues.Clear()`（与现有缓存清理一致）。
- **已知限制**：本地化字段（如 `Item.Name`/`Desc`）在游戏内 `TranslateText`（切语言）时会被覆盖回翻译值；「重置」恢复编辑前原值而非翻译值。
- **主线程**：窗口在 Editor 主线程渲染/写入，无并发问题。
- **搜索**：`RowMatches` 读活值，行搜索能命中编辑后的值。

## 5. 验证方式

**用户手动编译**。

1. **编译**：用户手动触发 Unity 编译，无错误。
2. **Play 冒烟**：框架正常启动、配置加载成功。
3. **面板验证**：
   - 展开某表某行，编辑一个简单值字段（如 `TbItem`→`Name`/`Icon`、`TbUIConfig`→`TweenDuration`、`TbSound`→`Path`）→ 面板即时显示新值 + 黄色高亮。
   - **运行时生效**：修改某个运行时读取的配置（如改 UI 配置时长后观察对应 UI 行为变化），确认改动真实作用于内存数据。
   - **Id/Key 锁定**：`Id`/`Key` 只读不可编辑。
   - **复杂/引用字段**：跨表引用、列表、数组等只读展示。
   - **字段级撤销**：点「重置」→ 恢复原值、高亮消失。
   - **行搜索**：用编辑后的新值做行搜索能命中该行。
   - **写失败**：人为构造（对只读属性强改）→ 红色提示、值不变。

## 6. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`[AI]feat: ConfigModuleWindow 支持实时编辑配置行简单值字段（反射 SetValue、Id/Key 锁定、字段级撤销、编辑高亮、写失败提示）`
- **Commit 2**：`[AI]docs: 新增 ConfigModuleWindow 实时编辑设计文档`
