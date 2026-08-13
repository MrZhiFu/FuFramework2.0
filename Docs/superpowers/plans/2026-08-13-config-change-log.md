# ConfigModuleWindow 变更记录导出实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 ConfigModuleWindow 工具栏新增「记录变更」按钮，点击后遍历撤销缓存生成 Markdown 变更文档（表名/行Id/Excel列名/旧值→新值）并经保存对话框导出，供手动填回源 Excel。

**Architecture:** 单文件扩展。`ExportChangeLog()` 遍历 `CfgNames` → 每表 `GetRows` → 对每行检查 `m_FieldOriginalValues` 是否有变更 → 收集 {表名, 行Id, Excel列名, 旧值, 新值} → 拼接 Markdown → `SaveFilePanel` 选路径 → `File.WriteAllText` 写文件。新增 `GetRowIdentity`/`ToExcelColumnName`/`FormatValue` 三个辅助方法。

**Tech Stack:** UnityEditor（`EditorGUILayout`/`EditorUtility`/`GUI`）、C#（`System.IO`/`System.Text`/`System`）、反射。

## Global Constraints

- **仅改** `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`，零 Hotfix 改动、零写 Excel/JSON 文件。
- **数据来源**：复用 `m_FieldOriginalValues`（撤销缓存：行引用 → 属性名 → 原值）；新值实时读 `prop.GetValue(row)`。
- **文档格式**：文本/Markdown；字段名显示 **Excel 列名**（C# 属性名 → snake_case，如 `SubType` → `sub_type`）；枚举显示枚举名；行 Id 显示 Id/Key 属性值。
- **幂等**：记录不清理 `m_FieldOriginalValues`，可重复导出。
- **空状态**：`m_FieldOriginalValues.Count == 0` → `EditorUtility.DisplayDialog` 提示后返回。
- **用户取消**：`SaveFilePanel` 返回空串 → 直接返回不写文件。
- **写文件异常**：`File.WriteAllText` 包 try/catch，失败 `Debug.LogError` + `DisplayDialog` 提示。
- **新增 using**：`System.IO`（`File`）、`System.Text`（`StringBuilder`）；`System` 已有（`DateTime`/`Exception`）。
- **代码风格**：全部中文注释、`///` XML 注释（`<summary>` 换行）、**Tab 缩进**（非空格）、K&R 括号、私有字段 `m_` 前缀、显式访问修饰符、局部变量 `var`、字段/属性/参数显式类型。
- **验证方式**：用户手动编译（执行者不跑 unity-cli 编译）。
- **提交规范**：`Docs/Git提交规范.md`，`[AI]` 前缀 + Conventional Commits。

---

### Task 1: 变更记录导出实现

**Files:**
- Modify: `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`（新增 using ×2、`ExportChangeLog`、`GetRowIdentity`、`ToExcelColumnName`、`FormatValue`；改 `DrawToolbar` 加按钮）

**Interfaces:**
- Consumes: 现有 `m_FieldOriginalValues`、`m_CfgNamesProperty`、`m_GetConfigMethod`、`GetRows(object, string)`（均已存在于窗口内）。
- Produces: `private void ExportChangeLog()`、`private static string GetRowIdentity(object row)`、`private static string ToExcelColumnName(string propName)`、`private static string FormatValue(object value)`（全部为窗口内私有，无跨任务依赖）。

- [ ] **Step 1: 新增 using**

在文件顶部现有 using 区（`using System;` 之后）追加：

```csharp
using System.IO;
using System.Text;
```

> 最终 using 区应为：`using System;`、`using System.Collections;`、`using System.Collections.Generic;`、`using System.IO;`、`using System.Reflection;`、`using System.Text;`、`using UnityEditor;`、`using UnityEngine;`。

- [ ] **Step 2: 新增 `ExportChangeLog` 方法**

在 `#region 反射` 之前（`DrawColumnSeparator` 方法之后）插入：

```csharp
		/// <summary>
		/// 记录变更：遍历所有已编辑配置，生成 Markdown 变更文档并保存，供手动填回源 Excel。
		/// 数据来源为编辑功能撤销缓存（行引用 → 属性名 → 原值），新值实时读取行对象。
		/// </summary>
		private void ExportChangeLog()
		{
			if (m_FieldOriginalValues.Count == 0)
			{
				EditorUtility.DisplayDialog("记录变更", "当前没有需要记录的变更。", "确定");
				return;
			}

			var cfgNames = m_CfgNamesProperty?.GetValue(m_ModuleInstance) as string[];
			if (cfgNames == null || cfgNames.Length == 0) return;

			var sb = new StringBuilder();
			sb.AppendLine("# 配置变更记录");
			sb.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm}");
			sb.AppendLine();

			foreach (var cfgName in cfgNames)
			{
				var table = m_GetConfigMethod?.Invoke(m_ModuleInstance, new object[] { cfgName });
				if (table == null) continue;

				var rows = GetRows(table, cfgName);
				var changedRows = new List<object>();
				foreach (var row in rows)
				{
					if (m_FieldOriginalValues.ContainsKey(row)) changedRows.Add(row);
				}

				if (changedRows.Count == 0) continue;

				sb.AppendLine($"## {cfgName}");
				foreach (var row in changedRows)
				{
					sb.AppendLine($"### id {GetRowIdentity(row)}");
					var orig = m_FieldOriginalValues[row];
					foreach (var kv in orig)
					{
						object newValue;
						try
						{
							newValue = row.GetType().GetProperty(kv.Key)?.GetValue(row);
						}
						catch (Exception)
						{
							newValue = "<读取异常>";
						}

						sb.AppendLine($"- {ToExcelColumnName(kv.Key)}：{FormatValue(kv.Value)} → {FormatValue(newValue)}");
					}

					sb.AppendLine();
				}
			}

			var path = EditorUtility.SaveFilePanel("保存配置变更记录", "", "配置变更记录", "md");
			if (string.IsNullOrEmpty(path)) return; // 用户取消

			try
			{
				File.WriteAllText(path, sb.ToString());
				EditorUtility.DisplayDialog("记录变更", $"变更记录已保存：{path}", "确定");
			}
			catch (Exception e)
			{
				Debug.LogError($"[ConfigDebug] 保存变更记录失败：{e.Message}");
				EditorUtility.DisplayDialog("记录变更", $"保存失败：{e.Message}", "确定");
			}
		}
```

- [ ] **Step 3: 新增 `GetRowIdentity` 方法**

在 `ExportChangeLog` 之后插入：

```csharp
		/// <summary>
		/// 获取行身份值（优先 Id/Key 属性，缺失返回空串）。
		/// </summary>
		/// <param name="row">行对象</param>
		/// <returns>行身份值字符串</returns>
		private static string GetRowIdentity(object row)
		{
			foreach (var name in new[] { "Id", "Key" })
			{
				var prop = row.GetType().GetProperty(name);
				if (prop != null)
				{
					object value;
					try
					{
						value = prop.GetValue(row);
					}
					catch (Exception)
					{
						continue;
					}

					if (value != null) return value.ToString();
				}
			}

			return "";
		}
```

- [ ] **Step 4: 新增 `ToExcelColumnName` 方法**

在 `GetRowIdentity` 之后插入：

```csharp
		/// <summary>
		/// C# 属性名转 Excel 列名（CamelCase → snake_case）：SubType → sub_type、Id → id。
		/// 仅用于变更文档展示；若出现缩写列名（如 HP）转换不完美，用户可对照源表自行匹配。
		/// </summary>
		/// <param name="propName">C# 属性名</param>
		/// <returns>snake_case 列名</returns>
		private static string ToExcelColumnName(string propName)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < propName.Length; i++)
			{
				var c = propName[i];
				if (char.IsUpper(c))
				{
					if (i > 0) sb.Append('_');
					sb.Append(char.ToLowerInvariant(c));
				}
				else
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}
```

- [ ] **Step 5: 新增 `FormatValue` 方法**

在 `ToExcelColumnName` 之后插入：

```csharp
		/// <summary>
		/// 格式化字段值用于文档展示：枚举 `ToString()` 天然输出枚举名（如 White），null 显示空串。
		/// </summary>
		/// <param name="value">字段值</param>
		/// <returns>展示字符串</returns>
		private static string FormatValue(object value)
		{
			if (value == null) return "";
			return value.ToString();
		}
```

- [ ] **Step 6: 修改 `DrawToolbar` 新增「记录变更」按钮**

在 `DrawToolbar` 的 `GUILayout.FlexibleSpace();` 之后、`if (GUILayout.Button("刷新"` 之前插入：

```csharp
		if (GUILayout.Button("记录变更", EditorStyles.toolbarButton, GUILayout.Width(80)))
		{
			ExportChangeLog();
		}
```

> 即工具栏顺序：搜索框 → 自动刷新 → 弹性空间 → **记录变更** → 刷新。

- [ ] **Step 7: 校验符号与代码风格**

Run（确认新增符号齐备、无多余 using）：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Editor/FuFramework/Config"
grep -n "using System.IO\|using System.Text\|ExportChangeLog\|GetRowIdentity\|ToExcelColumnName\|FormatValue\|记录变更" ConfigModuleWindow.cs
```

Expected: `using System.IO`/`using System.Text` 各 1 处；`ExportChangeLog` 定义 1 处 + 按钮调用 1 处；`GetRowIdentity`/`ToExcelColumnName`/`FormatValue` 定义 + 调用各成对；「记录变更」按钮 1 处。

- [ ] **Step 8: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs
git commit -m "[AI]feat: ConfigModuleWindow 新增「记录变更」导出（遍历撤销缓存生成 Markdown 变更文档，表名/行Id/Excel列名/旧新值）"
```

---

### Task 2: 验证与反馈收集

**Files:** 无新增/修改

**Interfaces:**
- Consumes: Task 1 全部产出

- [ ] **Step 1: 用户手动编译**

请用户手动触发 Unity 编译。执行者不主动跑 unity-cli 编译。预期：无编译错误（重点确认 `System.IO`/`System.Text` using、`File`/`StringBuilder`/`DateTime` 解析、`SaveFilePanel` API 正确）。

- [ ] **Step 2: Play 冒烟 + 导出验证**

请用户：进入 Play 模式，打开 `FuFramework/调试/配置调试面板`，验证：
- 编辑若干字段（不同表、不同行，含 enum、数值、string）。
- 点「记录变更」→ 选保存路径 → 确认提示。
- 打开生成的 `.md` 文档：核对表名、行 Id、字段（snake_case，如 `sub_type`/`tween_duration`）、旧值 → 新值与面板编辑一致；枚举显示枚举名。
- 未编辑任何字段时点按钮 → 弹「当前没有需要记录的变更」。
- 用户取消保存对话框 → 无文件写入。

- [ ] **Step 3: 反馈处理**

收集用户编译/运行反馈。若有编译错误或运行问题，回到 Task 1 修复；全部通过则本计划完成。

---

## Self-Review

### Spec coverage

| 规格要求 | 对应任务 |
|---|---|
| 工具栏「记录变更」按钮（§4.1） | Task 1 Step 6 |
| `ExportChangeLog` 收集 + 生成 + 保存（§4.2） | Task 1 Step 2 |
| `GetRowIdentity` 行 Id（§4.3） | Task 1 Step 3 |
| `ToExcelColumnName` snake_case（§4.4） | Task 1 Step 4 |
| `FormatValue` 枚举名/null（§4.5） | Task 1 Step 5 |
| 无变更提示 / 取消 / 写异常 try/catch（§4.6） | Task 1 Step 2 |
| 新增 using（§4.7） | Task 1 Step 1 |
| 验证方式（§5） | Task 2 |

### Placeholder scan

所有代码步骤均含完整可编译代码；无 "TBD"/"TODO"/"待定" 占位。

### Type consistency

- `ExportChangeLog()`：Task 1 Step 2 定义（private void），Step 6 按钮调用——一致。
- `GetRowIdentity(object row)` 返回 string：Step 3 定义，Step 2 `$"### 行 {GetRowIdentity(row)}"` 调用——一致。
- `ToExcelColumnName(string)` 返回 string：Step 4 定义，Step 2 `ToExcelColumnName(kv.Key)` 调用——一致。
- `FormatValue(object)` 返回 string：Step 5 定义，Step 2 `FormatValue(kv.Value)`/`FormatValue(newValue)` 调用——一致。
- 使用现有 `m_FieldOriginalValues`/`m_CfgNamesProperty`/`m_GetConfigMethod`/`GetRows`——均存在于当前窗口（Task 1-2 编辑功能已实现并评审通过）。
