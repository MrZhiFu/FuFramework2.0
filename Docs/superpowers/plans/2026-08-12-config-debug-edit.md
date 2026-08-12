# ConfigModuleWindow 实时编辑配置内存数据实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 ConfigModuleWindow 调试面板中实现配置行简单值字段的实时内存编辑（反射 `SetValue`、Id/Key 锁定、字段级撤销、编辑高亮、写失败提示）。

**Architecture:** 单文件扩展。`DrawRow` 按 `IsEditableProperty` 将可编辑字段分发到 `DrawEditableField`：用 UnityEditor 类型化控件渲染，值变化时 `SetValue` 写回活配置对象并记录原值；已编辑字段黄色高亮、写失败 2s 红色标注、提供「重置」按钮还原。

**Tech Stack:** UnityEditor（`EditorGUILayout`/`GUI`/`EditorApplication`）、C# 反射（`PropertyInfo.SetValue`）。

## Global Constraints

- **仅改** `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`，零 Hotfix 改动、零新增 API。
- **可编辑判定**：有 setter（`GetSetMethod(true) != null`，Luban 的 `private set` 也算）+ 类型 ∈ {`string`,`bool`,`byte`,`sbyte`,`short`,`ushort`,`int`,`uint`,`long`,`ulong`,`float`,`double`,`enum`} + 名称 ∉ {`Id`,`Key`}（身份字段锁定，防破坏 `LongKeyDataDict`/`StrKeyDataDict`）。
- **实时性**：编辑直接写活配置对象（`All` 返回的 `DataList` 引用），不持久化、不重载。
- **撤销**：首次编辑缓存原值到 `m_FieldOriginalValues`，已编辑字段提供「重置」按钮还原。
- **可视化**：已编辑字段 `Color.yellow` 高亮；写回失败 `Debug.LogError` + `Color.red` 标注 2 秒。
- **复杂/只读**：无 setter 或非简单类型属性保持只读文本；`GetValue` 抛异常渲染错误串且不进入编辑分支。
- **ResetReflection**：退出 Play 时清空新增的两个缓存（`m_FieldOriginalValues`/`m_WriteFailTimes`）。
- **代码风格**：全部中文注释、`///` XML 注释（`<summary>` 换行）、Tab 缩进、K&R 括号、私有字段 `m_` 前缀、显式访问修饰符、局部变量 `var`、字段/属性/参数显式类型。
- **验证方式**：用户手动编译（执行者不跑 unity-cli 编译）。
- **提交规范**：`Docs/Git提交规范.md`，`[AI]` 前缀 + Conventional Commits。

---

### Task 1: 编辑基础设施（状态字段 + 可编辑判定 + 控件渲染 + 缓存清理）

**Files:**
- Modify: `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`（新增两个字段、`IsEditableProperty`、`RenderFieldControl`；`ResetReflection` 补两行清理）

**Interfaces:**
- Consumes: 现有 `m_FieldOriginalValues`/`m_WriteFailTimes` 尚不存在（本任务新增）；`using System`/`System.Collections.Generic`/`System.Reflection`/`UnityEditor`/`UnityEngine` 已存在，无需新增 using。
- Produces: `private static bool IsEditableProperty(PropertyInfo prop)`、`private static object RenderFieldControl(PropertyInfo prop, object currentValue)`（供 Task 2 的 `DrawEditableField` 使用）。

- [ ] **Step 1: 新增编辑状态字段**

在 `#region 私有字段` 中、`m_LastRefreshTime` 字段之后追加：

```csharp
/// <summary>
/// 字段编辑撤销缓存：行对象引用 → 属性名 → 原始值。
/// 首次编辑某字段时记录原值，供「重置」回滚与「编辑高亮」判定。
/// 行是活配置对象（Luban bean 未重写 Equals，引用相等作 key 安全）。
/// </summary>
private readonly Dictionary<object, Dictionary<string, object>> m_FieldOriginalValues = new();

/// <summary>
/// 写回失败时间戳缓存：行对象引用 → 属性名 → 失败时刻（EditorApplication.timeSinceStartup）。
/// 失败后 2 秒内该字段红色标注；成功后移除。
/// </summary>
private readonly Dictionary<object, Dictionary<string, double>> m_WriteFailTimes = new();
```

- [ ] **Step 2: 新增 `IsEditableProperty` 判定方法**

在 `#region 行数据绘制` 内、`DrawRow` 之前插入：

```csharp
/// <summary>
/// 判断属性是否可编辑：有 setter（含私有）、类型为简单值类型、且非身份字段。
/// </summary>
/// <param name="prop">属性</param>
/// <returns>是否可编辑</returns>
private static bool IsEditableProperty(PropertyInfo prop)
{
    // 身份字段锁定（修改会破坏 LongKeyDataDict/StrKeyDataDict 索引）
    if (prop.Name == "Id" || prop.Name == "Key") return false;
    if (prop.GetSetMethod(true) == null) return false;

    var type = prop.PropertyType;
    if (type.IsEnum) return true;
    if (type == typeof(string) || type == typeof(bool)) return true;
    if (type == typeof(byte) || type == typeof(sbyte)) return true;
    if (type == typeof(short) || type == typeof(ushort)) return true;
    if (type == typeof(int) || type == typeof(uint)) return true;
    if (type == typeof(long) || type == typeof(ulong)) return true;
    if (type == typeof(float) || type == typeof(double)) return true;
    return false;
}
```

- [ ] **Step 3: 新增 `RenderFieldControl` 控件渲染方法**

在 `IsEditableProperty` 之后插入：

```csharp
/// <summary>
/// 渲染属性对应类型化控件并返回新值。
/// 数值控件内建校验：无效输入不提交，返回值恒为最后一个有效值。
/// </summary>
/// <param name="prop">属性</param>
/// <param name="currentValue">当前值</param>
/// <returns>控件返回的新值</returns>
private static object RenderFieldControl(PropertyInfo prop, object currentValue)
{
    var type = prop.PropertyType;

    if (type == typeof(string))
        return EditorGUILayout.TextField((string)currentValue, GUILayout.MinWidth(120));

    if (type == typeof(bool))
        return EditorGUILayout.Toggle((bool)currentValue, GUILayout.MinWidth(120));

    if (type.IsEnum)
        return EditorGUILayout.EnumPopup((Enum)currentValue, GUILayout.MinWidth(120));

    if (type == typeof(int))
        return EditorGUILayout.IntField((int)currentValue, GUILayout.MinWidth(120));

    if (type == typeof(long))
        return EditorGUILayout.LongField((long)currentValue, GUILayout.MinWidth(120));

    if (type == typeof(float))
        return EditorGUILayout.FloatField((float)currentValue, GUILayout.MinWidth(120));

    if (type == typeof(double))
        return EditorGUILayout.DoubleField((double)currentValue, GUILayout.MinWidth(120));

    // 其余整型族：经 IntField/LongField 转换回目标类型
    if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short)
        || type == typeof(ushort) || type == typeof(uint))
        return Convert.ChangeType(EditorGUILayout.IntField(Convert.ToInt32(currentValue), GUILayout.MinWidth(120)), type);

    if (type == typeof(ulong))
        return unchecked((ulong)EditorGUILayout.LongField((long)currentValue, GUILayout.MinWidth(120)));

    return currentValue; // 不可编辑类型不应到达此分支
}
```

- [ ] **Step 4: `ResetReflection` 补清新增缓存**

在 `ResetReflection()` 方法内、`m_RowFoldoutStates.Clear();` 之后追加：

```csharp
m_FieldOriginalValues.Clear();
m_WriteFailTimes.Clear();
```

- [ ] **Step 5: 校验编译逻辑与代码风格**

Run（确认无语法残留、无多余 using 需要）：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Editor/FuFramework/Config"
grep -n "m_FieldOriginalValues\|m_WriteFailTimes\|IsEditableProperty\|RenderFieldControl" ConfigModuleWindow.cs
```

Expected: 四处符号均出现；`using System.Runtime.CompilerServices` 未被引入（本设计用嵌套字典作 key，无需 RuntimeHelpers）。

- [ ] **Step 6: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs
git commit -m "[AI]feat: ConfigModuleWindow 新增实时编辑基础设施（撤销缓存、写失败缓存、IsEditableProperty 判定、RenderFieldControl 控件渲染）"
```

---

### Task 2: 编辑接入（DrawEditableField + DrawRow 分发 + 类注释）

**Files:**
- Modify: `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`（新增 `DrawEditableField`；改 `DrawRow` 分发；更新类文档注释）

**Interfaces:**
- Consumes: Task 1 的 `IsEditableProperty(PropertyInfo)`、`RenderFieldControl(PropertyInfo, object)`、`m_FieldOriginalValues`、`m_WriteFailTimes`。
- Produces: 面板行内可编辑字段的实时编辑能力。

- [ ] **Step 1: 新增 `DrawEditableField` 方法**

在 `RenderFieldControl` 之后插入：

```csharp
/// <summary>
/// 绘制可编辑字段：渲染类型化控件、写回内存、编辑高亮与字段级撤销。
/// </summary>
/// <param name="row">行对象</param>
/// <param name="prop">属性</param>
/// <param name="currentValue">当前值</param>
private void DrawEditableField(object row, PropertyInfo prop, object currentValue)
{
    var isEdited = m_FieldOriginalValues.TryGetValue(row, out var origDict)
                   && origDict.ContainsKey(prop.Name);
    var isWriteFail = m_WriteFailTimes.TryGetValue(row, out var failDict)
                      && failDict.TryGetValue(prop.Name, out var failTime)
                      && EditorApplication.timeSinceStartup - failTime < 2.0;

    // 已编辑字段黄色高亮；写失败红色（优先显示）
    var fieldOldColor = GUI.color;
    if (isWriteFail) GUI.color = Color.red;
    else if (isEdited) GUI.color = Color.yellow;

    object newValue;
    try
    {
        newValue = RenderFieldControl(prop, currentValue);
    }
    catch (Exception e)
    {
        GUI.color = fieldOldColor;
        Debug.LogError($"[ConfigDebug] 渲染 {prop.Name} 失败：{e.Message}");
        return;
    }

    GUI.color = fieldOldColor;

    // 值变化 → 记录原值并写回活配置对象
    if (!Equals(newValue, currentValue))
    {
        if (origDict == null)
        {
            origDict = new Dictionary<string, object>();
            m_FieldOriginalValues[row] = origDict;
        }

        if (!origDict.ContainsKey(prop.Name)) origDict[prop.Name] = currentValue;

        try
        {
            prop.SetValue(row, newValue);
            if (failDict != null) failDict.Remove(prop.Name);
        }
        catch (Exception e)
        {
            if (failDict == null)
            {
                failDict = new Dictionary<string, double>();
                m_WriteFailTimes[row] = failDict;
            }

            failDict[prop.Name] = EditorApplication.timeSinceStartup;
            Debug.LogError($"[ConfigDebug] 写入 {prop.Name} 失败：{e.Message}");
        }
    }

    // 已编辑字段提供「重置」按钮
    if (isEdited && origDict != null && origDict.TryGetValue(prop.Name, out var originalValue))
    {
        if (GUILayout.Button("重置", GUILayout.Width(40)))
        {
            try
            {
                prop.SetValue(row, originalValue);
                origDict.Remove(prop.Name);
                if (origDict.Count == 0) m_FieldOriginalValues.Remove(row);
                if (failDict != null) failDict.Remove(prop.Name);
            }
            catch (Exception e)
            {
                if (failDict == null)
                {
                    failDict = new Dictionary<string, double>();
                    m_WriteFailTimes[row] = failDict;
                }

                failDict[prop.Name] = EditorApplication.timeSinceStartup;
                Debug.LogError($"[ConfigDebug] 重置 {prop.Name} 失败：{e.Message}");
            }
        }
    }
}
```

- [ ] **Step 2: 修改 `DrawRow` 分发可编辑字段**

将 `DrawRow` 内的字段遍历循环（当前为「读值 → 渲染 label + 只读文本」）：

```csharp
		foreach (var prop in row.GetType().GetProperties())
		{
			object value;
			try
			{
				value = prop.GetValue(row);
			}
			catch (Exception e)
			{
				value = $"<读取异常: {e.Message}>";
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(prop.Name, GUILayout.MinWidth(120));
			DrawColumnSeparator();
			GUILayout.Label(value?.ToString() ?? "null");
			EditorGUILayout.EndHorizontal();
		}
```

替换为：

```csharp
		foreach (var prop in row.GetType().GetProperties())
		{
			object value;
			var readOk = true;
			try
			{
				value = prop.GetValue(row);
			}
			catch (Exception e)
			{
				value = $"<读取异常: {e.Message}>";
				readOk = false;
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(prop.Name, GUILayout.MinWidth(120));
			DrawColumnSeparator();

			// 可编辑字段渲染编辑控件；读取失败或不可编辑则只读展示
			if (readOk && IsEditableProperty(prop))
			{
				DrawEditableField(row, prop, value);
			}
			else
			{
				GUILayout.Label(value?.ToString() ?? "null");
			}

			EditorGUILayout.EndHorizontal();
		}
```

> 说明：`readOk` 标志区分「读取失败的错误串」与「合法 string 值」，读取失败时不进入编辑分支。保持现有缩进为 Tab（文件全篇 Tab）。

- [ ] **Step 3: 更新类文档注释**

将 `ConfigModuleWindow` 类顶部的文档注释（当前含 3 条功能）：

```csharp
	/// 功能：
	///     1. 展示所有配置表及其加载信息（类型、数据行数、long/string key 数量）。
	///     2. 展开配置表查看行数据，再展开行查看字段键值对。
	///     3. 支持表名搜索过滤、表内字段值模糊搜索、自动刷新。
```

替换为：

```csharp
	/// 功能：
	///     1. 展示所有配置表及其加载信息（类型、数据行数、long/string key 数量）。
	///     2. 展开配置表查看行数据，再展开行查看字段键值对。
	///     3. 支持表名搜索过滤、表内字段值模糊搜索、自动刷新。
	///     4. 支持实时编辑配置行简单值字段（Id/Key 锁定、字段级撤销、编辑高亮）。
```

- [ ] **Step 4: 校验编辑分支一致性**

Run（确认无遗留重复的只读渲染、新方法均已接入）：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Editor/FuFramework/Config"
grep -n "DrawEditableField\|IsEditableProperty\|RenderFieldControl\|m_FieldOriginalValues\|m_WriteFailTimes" ConfigModuleWindow.cs
```

Expected: `DrawEditableField` 定义 1 处 + `DrawRow` 调用 1 处；`IsEditableProperty` 定义 1 处 + 调用 1 处；`RenderFieldControl` 定义 1 处 + 调用 1 处（在 `DrawEditableField` 内）；两个缓存字段定义各 1 处、`ResetReflection` 清理各 1 处、`DrawEditableField` 使用若干处。

- [ ] **Step 5: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs
git commit -m "[AI]feat: ConfigModuleWindow 接入实时编辑（DrawRow 分发可编辑字段、DrawEditableField 写回/撤销/高亮、类注释更新）"
```

---

### Task 3: 验证与反馈收集

**Files:** 无新增/修改

**Interfaces:**
- Consumes: Task 1-2 全部产出

- [ ] **Step 1: 用户手动编译**

请用户手动触发 Unity 编译。执行者不主动跑 unity-cli 编译。预期：无编译错误（重点确认 `Enum`/`Convert`/`Equals` 等 System 成员可用、`GUILayout.MinWidth` 与 `EditorGUILayout.*Field` API 正确、无新增 using 缺失）。

- [ ] **Step 2: Play 冒烟 + 面板编辑验证**

请用户：进入 Play 模式，打开 `FuFramework/调试/配置调试面板`，验证：
- 展开某表某行（如 `TbItem`→`Name`、`TbUIConfig`→`TweenDuration`、`TbSound`→`Path`）编辑一个简单值字段 → 面板即时显示新值 + 黄色高亮。
- **运行时生效**：修改某个运行时读取的配置后，确认游戏逻辑读到新值。
- `Id`/`Key` 字段只读不可编辑；复杂/引用字段（跨表引用、List、数组）只读。
- 点「重置」→ 值还原、高亮消失。
- 用编辑后的新值做行搜索能命中该行。

- [ ] **Step 3: 反馈处理**

收集用户编译/运行反馈。若有编译错误或运行问题，回到对应 Task 修复；全部通过则本计划完成。

---

## Self-Review

### Spec coverage

| 规格要求 | 对应任务 |
|---|---|
| 可编辑判定（setter + 简单类型 + 非 Id/Key，§3.1） | Task 1 Step 2 |
| 控件映射（§3.2） | Task 1 Step 3 |
| 编辑状态字段 `m_FieldOriginalValues`（§4.1） | Task 1 Step 1 |
| `DrawRow` 分发 + `readOk` 守卫（§4.2） | Task 2 Step 2 |
| `DrawEditableField`：写回/原值缓存/黄色高亮/重置/写失败红 2s（§4.3） | Task 2 Step 1 |
| 复杂/只读保持只读（§4.4） | Task 2 Step 2（else 分支） |
| ResetReflection 清理（§4.4） | Task 1 Step 4 |
| 类注释更新（功能 4） | Task 2 Step 3 |
| 验证方式（§5） | Task 3 |

### Placeholder scan

所有代码步骤均含完整可编译代码；无 "TBD"/"TODO"/"待定" 类占位。

### Type consistency

- `IsEditableProperty(PropertyInfo)` 返回 `bool`：Task 1 定义，Task 2 `DrawRow` 调用（`readOk && IsEditableProperty(prop)`）——一致。
- `RenderFieldControl(PropertyInfo, object)` 返回 `object`：Task 1 定义，Task 2 `DrawEditableField` 调用（`newValue = RenderFieldControl(prop, currentValue)`）——一致。
- `m_FieldOriginalValues`/`m_WriteFailTimes` 类型为 `Dictionary<object, Dictionary<string, ...>>`：Task 1 定义，Task 2 `DrawEditableField` 读写、Task 1 `ResetReflection` 清理——一致。
- `DrawEditableField(object row, PropertyInfo prop, object currentValue)`：Task 2 定义与调用签名一致。
