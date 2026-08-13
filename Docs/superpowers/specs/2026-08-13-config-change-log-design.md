# ConfigModuleWindow 变更记录导出设计

> 日期：2026-08-13
> 分支：`refactor/framework-modules-to-hotfix`
> 范围：`Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`（单文件）

## 1. 背景与问题

ConfigModuleWindow 已支持实时编辑配置行简单值字段（内存实时生效、字段级撤销、编辑高亮）。但编辑只在内存中，不持久化。为了把调试时的配置调整**正式应用到源配置**，需要把面板中的变更记录下来，供手动填回 Luban 源 Excel。

直接写回源 Excel 复杂且高风险（无 xlsx 写库、元数据/数据列错位、枚举名 vs int、本地化 Key 语义）。故改为：**「记录变更」按钮导出变更文档**，列出配置表名、行 Id、变更字段（Excel 列名）、旧值→新值，用户据此手动填回 Excel。

## 2. 目标与约束

### 2.1 目标

1. 窗口工具栏新增「记录变更」按钮。
2. 点击后遍历所有已编辑配置，生成**文本/Markdown 变更文档**：表名 + 行 Id + 变更字段（**Excel 列名**，snake_case）+ 旧值 → 新值。
3. 经保存对话框导出文档，供手动填回源 Excel。

### 2.2 已确认的关键决策

| 决策点 | 结论 | 理由 |
|---|---|---|
| 文档格式 | **文本/Markdown** | 用户选定，易读、便于手动对照 Excel |
| 字段名显示 | **Excel 列名**（C# 属性名 → snake_case） | 用户选定，直接对应源表 `##var` 列 |
| 数据来源 | 复用撤销缓存 `m_FieldOriginalValues`（row → 属性 → 原值） | 已精确记录哪些行/字段被编辑 |
| 新值读取 | `prop.GetValue(row)` 实时读 | 编辑已写内存，读活对象即新值 |
| 枚举显示 | 枚举名（`White`/`Blue`） | 源 Excel 枚举列存枚举名字符串，便于对照 |
| 行 Id 显示 | 现有 `GetRowLabel` 逻辑（优先 Id/Key 属性） | 身份字段锁定不可编辑，行身份稳定 |
| 保存 | `EditorUtility.SaveFilePanel` + 写文件 | 用户选择导出位置 |
| 幂等 | 记录不清理撤销缓存 | 可重复点击重新生成当前全部变更快照 |
| 空状态 | 无变更时 `DisplayDialog` 提示 | 避免导出空文档 |
| 验证方式 | 用户手动编译 | 既有约定 |

## 3. 架构

仅修改 `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`：

```
ConfigModuleWindow.cs（现有，+ 记录变更）
├── 工具栏
│   └── DrawToolbar      # 修改：在「刷新」左侧新增「记录变更」按钮
├── 记录与导出
│   └── ExportChangeLog  # 新增：收集变更 + 生成文档 + 保存
├── 辅助
│   └── ToExcelColumnName # 新增：C# 属性名 → snake_case
└── （复用现有 m_FieldOriginalValues / GetRows / GetRowLabel）
```

数据流：点「记录变更」→ `ExportChangeLog()` → 遍历 `CfgNames` → 每表 `GetRows` → 对每行检查 `m_FieldOriginalValues.ContainsKey(row)` → 命中则逐字段收集 `{旧值, 新值, Excel列名}` → 拼接 Markdown → `SaveFilePanel` 选路径 → `File.WriteAllText` 写文件 → 提示路径。

## 4. 详细设计

### 4.1 工具栏按钮（`DrawToolbar`）

在「刷新」按钮之前插入「记录变更」：

```csharp
if (GUILayout.Button("记录变更", EditorStyles.toolbarButton, GUILayout.Width(80)))
{
    ExportChangeLog();
}
```

### 4.2 收集变更 + 生成文档 + 保存（`ExportChangeLog`）

```csharp
/// <summary>
/// 记录变更：遍历所有已编辑配置，生成 Markdown 变更文档并保存，供手动填回源 Excel。
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
            sb.AppendLine($"### 行 {GetRowIdentity(row)}");
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

### 4.3 行 Id 显示（`GetRowIdentity`）

复用现有 `GetRowLabel` 的 Id/Key 取值逻辑，但不带序号前缀（文档里只显示身份值）：

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

### 4.4 字段名转换（`ToExcelColumnName`）

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

### 4.5 值格式化（`FormatValue`）

```csharp
/// <summary>
/// 格式化字段值用于文档展示：枚举显示枚举名，null 显示空串。
/// </summary>
/// <param name="value">字段值</param>
/// <returns>展示字符串</returns>
private static string FormatValue(object value)
{
    if (value == null) return "";
    return value.ToString();
}
```

> 说明：枚举 `value.ToString()` 天然输出枚举名（如 `White`），`int`/`float`/`string` 输出字面值，无需额外分支。

### 4.6 边界与错误处理

- **无变更**：`m_FieldOriginalValues.Count == 0` → `DisplayDialog` 提示后返回。
- **读取新值失败**：`prop.GetValue` 抛异常 → 显示 `<读取异常>`，不中断导出。
- **用户取消保存**：`SaveFilePanel` 返回空 → 直接返回，不写文件。
- **写文件失败**：`File.WriteAllText` 抛异常 → 由调用链冒泡（窗口按钮事件），或包 try/catch 记 `Debug.LogError`。设计采用包 try/catch 记日志，避免中断 OnGUI。
- **幂等**：不清理 `m_FieldOriginalValues`，可重复导出当前全部变更快照。

### 4.7 新增 using

`System.IO`（`File`）、`System.Text`（`StringBuilder`）、`System`（`DateTime`/`Exception`）。`UnityEditor`/`UnityEngine` 已有。

## 5. 验证方式

**用户手动编译**。

1. **编译**：用户手动触发 Unity 编译，无错误。
2. **Play 冒烟 + 导出验证**：
   - 打开调试面板，编辑若干字段（不同表、不同行）。
   - 点「记录变更」→ 选保存路径 → 确认提示。
   - 打开生成的 `.md` 文档：核对表名、行 Id、字段（snake_case）、旧值 → 新值与面板编辑一致；枚举显示枚举名。
   - 未编辑任何字段时点按钮 → 弹「当前没有需要记录的变更」。
   - 用户取消保存对话框 → 无文件写入。

## 6. 提交拆分（遵循 `Docs/Git提交规范.md`）

- **Commit 1**：`[AI]feat: ConfigModuleWindow 新增「记录变更」导出（遍历撤销缓存生成 Markdown 变更文档，表名/行Id/Excel列名/旧新值）`
- **Commit 2**：`[AI]docs: 新增 ConfigModuleWindow 变更记录导出设计文档`
