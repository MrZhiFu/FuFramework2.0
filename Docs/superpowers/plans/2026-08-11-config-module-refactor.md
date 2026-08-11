# Config 模块重构实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Config 模块重构为「主文件 + API 分部」结构，优化数据结构（SortedDictionary→Dictionary、删除冗余缓存、收敛查询），修正接口语义（AddConfig 返回 bool、CfgNames 快照、空名卫语句），并新增只读调试面板。

**Architecture:** 参照本分支 AssetModule/ReferencePool 已完成的「`XxxModule.cs` + `XxxModule.API.cs`」分部模式重构 `ConfigModule`；`BaseDataTable` 仅替换内部容器类型（字段名与契约不变，Luban 生成代码零影响）；新增 `ConfigModuleWindow` 编辑器调试面板（反射访问 Hotfix 实例）。

**Tech Stack:** Unity C#（.NET Standard 2.1）、UnityEditor 反射、Hotfix 框架模块体系、Luban 生成代码（仅消费，不改）。

## Global Constraints

- **保留全部接口**：`IDataTable<T>` 所有成员（含 `Find`/`Max`/`Min`/`Sum`/`ToArray`/`FirstOrDefault`/`LastOrDefault` 等零调用成员）一律保留，不得删除。
- **仅框架侧，不动生成器**：`DataList`/`LongKeyDataDict`/`StrKeyDataDict` 三个 protected 字段名与成员契约保持不变（Luban 生成代码只做 `Clear`/`Add`，`Dictionary` 兼容），无需重构建 Luban、无需重新生成表格。
- **普通 Dictionary + 只读契约**：配置在启动期 `LoadConfigAsync` 一次性加载，之后只读；ConfigModule 与 BaseDataTable 内部均用普通 `Dictionary`。
- **对齐分部模式**：`ConfigModule` 拆出 `ConfigModule.API.cs`（全部 public 成员），主文件只保留私有字段 + 生命周期。
- **调试面板只读**：无任何移除/修改功能。
- **代码风格**：`Docs/代码风格规范.md`——全部中文注释、`///` XML 注释（`<summary>` 换行）、Tab 缩进、K&R 括号、私有字段 `m_` 前缀、显式访问修饰符、局部变量 `var`、字段/属性/参数显式类型。
- **实际 API 名称**（代码风格文档有出入，以实际代码库为准）：守卫用 `FuGuardEx` 扩展方法（`Hotfix.Framework.Core`），空名/空值抛 `ArgumentNullException`；日志用 `FuLogger`（`AOT.Framework.Core.Log`）。
- **验证方式**：用户手动编译后反馈，执行者不主动跑 unity-cli 编译。
- **提交规范**：遵循 `Docs/Git提交规范.md`，`[AI]` 前缀 + Conventional Commits。

---

### Task 1: ConfigModule 拆分 API 分部 + 语义修正

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Config/ConfigModule.cs`（整体重写为：私有字段 + 生命周期）
- Create: `Unity/Assets/Scripts/Hotfix/Framework/Config/ConfigModule.API.cs`（全部 public 成员 + 语义修正）
- （新增文件的 `.meta` 由 Unity 编译后自动生成，随提交带上）

**Interfaces:**
- Consumes: `Hotfix.Framework.Core.ModuleBase`、`Hotfix.Framework.Core.FuGuardEx`（扩展方法）、`AOT.Framework.Core.Log.FuLogger`
- Produces: `ConfigModule`（partial）：`Instance`（静态属性）、`int Count`、`string[] CfgNames`、`T GetConfig<T>() where T : IDataTable`、`IDataTable GetConfig(string)`、`bool HasConfig<T>()`、`bool HasConfig(string)`、`bool AddConfig(string, IDataTable)`、`bool RemoveConfig<T>()`、`bool RemoveConfig(string)`、`void RemoveAllConfigs()`

- [ ] **Step 1: 重写主文件 `ConfigModule.cs`**

将整个文件替换为以下内容（删除 `m_CfgNameTypeDict` 字段、`GetTypeName<T>()` 方法及全部 public 成员）：

```csharp
using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Config
{
    /// <summary>
    /// 配置管理模块。
    /// 功能：
    ///     1. 存储所有配置表。
    ///     2. 配置表在启动期一次性加载，加载后只读。
    /// </summary>
    public sealed partial class ConfigModule : ModuleBase
    {
        /// <summary>
        /// 配置表字典。key为配置表名称，value为配置表数据。
        /// 配置在启动期一次性加载、加载后只读，故使用普通 Dictionary 保证读取路径最快。
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
}
```

> 说明：原 `ConcurrentDictionary` 改普通 `Dictionary`；`using System.Collections.Concurrent` 移除。

- [ ] **Step 2: 创建 `ConfigModule.API.cs`**

写入以下内容（全部 public 成员，语义修正已内嵌）：

```csharp
using System.Collections.Generic;
using System.Linq;
using AOT.Framework.Core.Log;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Config
{
    /// <summary>
    /// 配置管理模块的公共 API。
    /// 功能：
    ///     1. 获取配置表。
    ///     2. 检查配置表是否存在。
    ///     3. 增加/移除配置表。
    /// </summary>
    public sealed partial class ConfigModule : ModuleBase
    {
        /// <summary>
        /// 模块单例
        /// </summary>
        public static ConfigModule Instance { get; private set; }

        /// <summary>
        /// 获取配置表数量。
        /// </summary>
        public int Count => m_CfgDataDict.Count;

        /// <summary>
        /// 获取所有配置表名称。
        /// </summary>
        public string[] CfgNames => m_CfgDataDict.Keys.ToArray();

        /// <summary>
        /// 获取指定配置表。
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>配置表，不存在时返回 default</returns>
        public T GetConfig<T>() where T : IDataTable
        {
            var cfg = GetConfig(typeof(T).Name);
            return cfg == null ? default : (T)cfg;
        }

        /// <summary>
        /// 获取指定配置表。
        /// </summary>
        /// <param name="cfgName">配置表名称</param>
        /// <returns>配置表，不存在时返回 null</returns>
        public IDataTable GetConfig(string cfgName)
        {
            cfgName.NotNullOrEmpty(nameof(cfgName));
            return m_CfgDataDict.GetValueOrDefault(cfgName);
        }

        /// <summary>
        /// 检查是否存在指定配置表。
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>是否存在</returns>
        public bool HasConfig<T>() where T : IDataTable
        {
            return HasConfig(typeof(T).Name);
        }

        /// <summary>
        /// 检查是否存在指定配置表。
        /// </summary>
        /// <param name="cfgName">配置表名称</param>
        /// <returns>是否存在</returns>
        public bool HasConfig(string cfgName)
        {
            cfgName.NotNullOrEmpty(nameof(cfgName));
            return m_CfgDataDict.ContainsKey(cfgName);
        }

        /// <summary>
        /// 增加指定配置表。
        /// </summary>
        /// <param name="cfgName">配置表名称</param>
        /// <param name="cfgValue">配置表数据</param>
        /// <returns>是否增加成功</returns>
        public bool AddConfig(string cfgName, IDataTable cfgValue)
        {
            cfgName.NotNullOrEmpty(nameof(cfgName));
            cfgValue.NotNull(nameof(cfgValue));
            if (m_CfgDataDict.ContainsKey(cfgName))
            {
                FuLogger.LogWarning($"[ConfigModule] 配置表 '{cfgName}' 已存在，忽略重复添加。");
                return false;
            }

            m_CfgDataDict.Add(cfgName, cfgValue);
            return true;
        }

        /// <summary>
        /// 移除指定配置表。
        /// </summary>
        /// <typeparam name="T">配置表类型</typeparam>
        /// <returns>是否移除成功</returns>
        public bool RemoveConfig<T>() where T : IDataTable
        {
            return RemoveConfig(typeof(T).Name);
        }

        /// <summary>
        /// 移除指定配置表。
        /// </summary>
        /// <param name="cfgName">配置表名称</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveConfig(string cfgName)
        {
            cfgName.NotNullOrEmpty(nameof(cfgName));
            return m_CfgDataDict.Remove(cfgName);
        }

        /// <summary>
        /// 清空所有配置表。
        /// </summary>
        public void RemoveAllConfigs()
        {
            m_CfgDataDict.Clear();
        }
    }
}
```

> 说明：`GetConfig<T>()` 由三次字典操作收敛为「一次 `GetConfig(string)` + null 判断 + 类型转换」；`CfgNames` 返回 `string[]` 快照；`AddConfig` 返回 `bool` 并对重复添加 `FuLogger.LogWarning` 告警；所有 string 版本接口加 `NotNullOrEmpty` 卫语句（抛 `ArgumentNullException`）。

- [ ] **Step 3: 校验无残留引用**

Run（仅检查本任务改动的两个文件；`SortedDictionary` 属 Task 2 的 BaseDataTable 改动范围，此处不查）:

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Framework/Config"
grep -n "m_CfgNameTypeDict\|GetTypeName\|ConcurrentDictionary" ConfigModule.cs ConfigModule.API.cs
```

Expected: 无输出（ConfigModule 两个文件内无这些符号残留）。

- [ ] **Step 4: 校验分部一致性**

Run:

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Framework/Config"
grep -n "partial class ConfigModule\|sealed partial class\|: ModuleBase" ConfigModule.cs ConfigModule.API.cs
```

Expected: 两个文件均声明 `public sealed partial class ConfigModule : ModuleBase`（分部一致）。确认 `ConfigModule.API.cs` 未重复声明 `OnInit`/`OnDispose`/`m_CfgDataDict`。

- [ ] **Step 5: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Config/ConfigModule.cs Unity/Assets/Scripts/Hotfix/Framework/Config/ConfigModule.API.cs
# 若 Unity 已生成 ConfigModule.API.cs.meta，一并 add
git commit -m "[AI]refactor: ConfigModule 拆分 API 分部，ConcurrentDictionary→Dictionary，语义修正（AddConfig 返回 bool/告警、CfgNames 快照、空名卫语句）"
```

---

### Task 2: BaseDataTable 数据结构优化

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs:19`、`:24`（两处字段类型）、`:94-102`（All 属性）

**Interfaces:**
- Consumes: Task 1 的 `IDataTable<T>` 契约（未变）
- Produces: `BaseDataTable<T>` 内部容器换为 `Dictionary`（`Get`/`Find`/`ForEach`/`Max`/`Min`/`Sum`/`All`/`ToArray` 等全部签名不变）；`All` 与 `ToArray()` 复用实现

- [ ] **Step 1: 两个索引字典换为 Dictionary**

将第 19、24 行：

```csharp
protected readonly SortedDictionary<long, T> LongKeyDataDict = new();
protected readonly SortedDictionary<string, T> StrKeyDataDict = new();
```

替换为：

```csharp
protected readonly Dictionary<long, T> LongKeyDataDict = new();
protected readonly Dictionary<string, T> StrKeyDataDict = new();
```

> 说明：`Dictionary` 兼容生成代码的 `Clear()`/`Add()` 调用；查询 O(log n)→O(1)；无外部/生成代码依赖 `SortedDictionary` 的排序迭代（protected 字段仅 Luban 生成代码访问）。

- [ ] **Step 2: `All` 属性复用 `ToArray()` 实现**

将 `All` 属性（当前含 `get` 块体、独立拷贝逻辑）替换为表达式体：

```csharp
/// <summary>
/// 获取数据表的所有数据。
/// </summary>
public T[] All => ToArray();
```

> 说明：`ToArray()`（下方原方法）保留为唯一拷贝实现，与 `All` 复用，消除重复代码。

- [ ] **Step 3: 校验生成代码契约未破坏**

Run:

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Game/AutoGen/Tables"
grep -rn "DataList\.\|LongKeyDataDict\.\|StrKeyDataDict\." Generate | grep -o "\(Clear\|Add\)" | sort | uniq -c
```

Expected: 只出现 `Clear`/`Add` 两种调用（`Dictionary` 均支持，契约未破坏）。

- [ ] **Step 4: 校验 BaseDataTable 无 SortedDictionary 残留**

Run:

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts/Hotfix/Framework/Config"
grep -rn "SortedDictionary" .
```

Expected: 无输出。

- [ ] **Step 5: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Config/BaseDataTable.cs
git commit -m "[AI]refactor: BaseDataTable 改用 Dictionary 优化查询（SortedDictionary→Dictionary），All/ToArray 复用实现"
```

---

### Task 3: ConfigModuleWindow 调试面板

**Files:**
- Create: `Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs`
- （新增文件的 `.meta` 由 Unity 编译后自动生成，随提交带上）

**Interfaces:**
- Consumes: Task 1 产出的 `ConfigModule` public API（`Instance` 静态属性、`Count`、`CfgNames`、`GetConfig(string)`）；`IDataTable<T>.All` 属性；protected 字段 `LongKeyDataDict`/`StrKeyDataDict`
- Produces: 编辑器菜单 `FuFramework/调试/配置调试面板`，Play 模式下只读展示配置表

- [ ] **Step 1: 创建 `ConfigModuleWindow.cs`**

写入以下完整实现（对齐 `ReferencePoolModuleWindow` 模式，纯只读无移除功能）：

```csharp
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
    /// <summary>
    /// 配置调试面板。
    /// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 ConfigModule。
    /// 功能：
    ///     1. 展示所有配置表及其加载信息（类型、数据行数、long/string key 数量）。
    ///     2. 展开配置表查看行数据，再展开行查看字段键值对。
    ///     3. 支持表名搜索过滤、表内字段值模糊搜索、自动刷新。
    /// </summary>
    public class ConfigModuleWindow : EditorWindow
    {
        /// <summary>
        /// 打开调试面板
        /// </summary>
        [MenuItem("FuFramework/调试/配置调试面板")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigModuleWindow>("配置调试");
            window.minSize = new Vector2(800, 600);

            const float width  = 1000f;
            const float height = 600f;
            var x = (Screen.currentResolution.width  - width)  / 2f;
            var y = (Screen.currentResolution.height - height) / 2f;
            window.position = new Rect(x, y, width, height);
        }

        #region 私有字段

        /// <summary>
        /// 滚动位置
        /// </summary>
        private Vector2 m_ScrollPos;

        /// <summary>
        /// 表名搜索过滤字符串
        /// </summary>
        private string m_SearchFilter = "";

        /// <summary>
        /// 是否自动刷新
        /// </summary>
        private bool m_AutoRefresh = true;

        /// <summary>
        /// 配置表折叠状态缓存（按表名）
        /// </summary>
        private readonly Dictionary<string, bool> m_TableFoldoutStates = new();

        /// <summary>
        /// 展开表的行数据缓存（按表名，配置加载后只读，缓存安全）
        /// </summary>
        private readonly Dictionary<string, object[]> m_RowsCache = new();

        /// <summary>
        /// 表内行搜索过滤字符串（按表名）
        /// </summary>
        private readonly Dictionary<string, string> m_RowSearchFilters = new();

        /// <summary>
        /// 行折叠状态缓存（key 为 表名|行索引，行数据只读稳定，索引稳定）
        /// </summary>
        private readonly Dictionary<string, bool> m_RowFoldoutStates = new();

        /// <summary>
        /// 上次刷新时间
        /// </summary>
        private double m_LastRefreshTime;

        #endregion

        #region 反射缓存

        /// <summary>
        /// ConfigModule 类型
        /// </summary>
        private Type m_ConfigModuleType;

        /// <summary>
        /// ConfigModule 实例
        /// </summary>
        private object m_ModuleInstance;

        /// <summary>
        /// ConfigModule.Instance 静态属性
        /// </summary>
        private PropertyInfo m_InstanceProperty;

        /// <summary>
        /// ConfigModule.Count 属性
        /// </summary>
        private PropertyInfo m_ModuleCountProperty;

        /// <summary>
        /// ConfigModule.CfgNames 属性
        /// </summary>
        private PropertyInfo m_CfgNamesProperty;

        /// <summary>
        /// ConfigModule.GetConfig(string) 方法
        /// </summary>
        private MethodInfo m_GetConfigMethod;

        #endregion

        #region 生命周期

        /// <summary>
        /// 启用：订阅 EditorApplication.update
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 禁用：取消订阅 EditorApplication.update
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 编辑器帧更新：定时重绘
        /// </summary>
        private void OnEditorUpdate()
        {
            if (!m_AutoRefresh || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - m_LastRefreshTime < 0.5f) return;

            m_LastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        /// <summary>
        /// 绘制 GUI
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                ResetReflection();
                EditorGUILayout.HelpBox("需要在 Play 模式下使用", MessageType.Info);
                return;
            }

            if (!EnsureReflection())
            {
                EditorGUILayout.HelpBox("未能通过反射访问 ConfigModule，请确认 Hotfix 已加载", MessageType.Warning);
                return;
            }

            DrawModuleOverview();
            EditorGUILayout.Separator();

            var cfgNames = m_CfgNamesProperty?.GetValue(m_ModuleInstance) as string[];
            if (cfgNames == null || cfgNames.Length == 0)
            {
                EditorGUILayout.HelpBox("配置表为空", MessageType.Info);
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
            foreach (var cfgName in cfgNames)
            {
                DrawTable(cfgName);
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 工具栏

        /// <summary>
        /// 绘制顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("搜索:", GUILayout.Width(40));
            m_SearchFilter = GUILayout.TextField(m_SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));

            GUILayout.Space(20);
            m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 模块概览

        /// <summary>
        /// 绘制模块级概览
        /// </summary>
        private void DrawModuleOverview()
        {
            var count = m_ModuleCountProperty?.GetValue(m_ModuleInstance) ?? 0;
            EditorGUILayout.LabelField($"配置表总个数：{count}");
        }

        #endregion

        #region 配置表绘制

        /// <summary>
        /// 绘制单个配置表（Foldout + 加载信息 + 行数据）
        /// </summary>
        /// <param name="cfgName">配置表名称</param>
        private void DrawTable(string cfgName)
        {
            // 表名搜索过滤
            if (!string.IsNullOrEmpty(m_SearchFilter)
                && !cfgName.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var table = m_GetConfigMethod?.Invoke(m_ModuleInstance, new object[] { cfgName });
            if (table == null) return;

            if (!m_TableFoldoutStates.TryGetValue(cfgName, out var isOpen))
            {
                isOpen = true;
                m_TableFoldoutStates[cfgName] = true;
            }

            var rowCount = GetTableCount(table);
            var foldoutOldColor = GUI.color;
            GUI.color = Color.cyan;
            m_TableFoldoutStates[cfgName] = EditorGUILayout.Foldout(isOpen, $"{cfgName} ({rowCount} 行)", true);
            GUI.color = foldoutOldColor;
            if (!m_TableFoldoutStates[cfgName]) return;

            EditorGUILayout.BeginVertical("box");
            {
                DrawTableStats(table);
                EditorGUILayout.Separator();
                DrawRows(table, cfgName);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        /// <summary>
        /// 获取配置表数据行数
        /// </summary>
        /// <param name="table">配置表实例</param>
        /// <returns>行数</returns>
        private static int GetTableCount(object table)
        {
            var countProp = table.GetType().GetProperty("Count");
            return (int)(countProp?.GetValue(table) ?? 0);
        }

        /// <summary>
        /// 绘制加载信息统计行（类型、long/string key 数量）
        /// </summary>
        /// <param name="table">配置表实例</param>
        private void DrawTableStats(object table)
        {
            var longKeyCount = GetDictCount(table, "LongKeyDataDict");
            var strKeyCount  = GetDictCount(table, "StrKeyDataDict");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"类型: {table.GetType().Name}", GUILayout.MinWidth(200));
            DrawColumnSeparator();
            GUILayout.Label($"long key: {longKeyCount}", GUILayout.MinWidth(100));
            DrawColumnSeparator();
            GUILayout.Label($"string key: {strKeyCount}", GUILayout.MinWidth(100));
            if (longKeyCount == 0 && strKeyCount > 0)
            {
                DrawColumnSeparator();
                GUILayout.Label("本地化表(仅 string key)", GUILayout.MinWidth(160));
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 读取配置表内部 key 字典数量（反射 protected 字段）
        /// </summary>
        /// <param name="table">配置表实例</param>
        /// <param name="fieldName">字段名</param>
        /// <returns>字典数量</returns>
        private static int GetDictCount(object table, string fieldName)
        {
            var field = table.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field?.GetValue(table);
            return value is IDictionary dict ? dict.Count : 0;
        }

        #endregion

        #region 行数据绘制

        /// <summary>
        /// 绘制配置表内的行数据列表（含行搜索框）
        /// </summary>
        /// <param name="table">配置表实例</param>
        /// <param name="cfgName">配置表名称</param>
        private void DrawRows(object table, string cfgName)
        {
            if (!m_RowSearchFilters.TryGetValue(cfgName, out var rowFilter))
            {
                rowFilter = "";
                m_RowSearchFilters[cfgName] = rowFilter;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("行搜索:", GUILayout.Width(60));
            rowFilter = GUILayout.TextField(rowFilter, GUILayout.MinWidth(200));
            m_RowSearchFilters[cfgName] = rowFilter;
            EditorGUILayout.EndHorizontal();

            var rows = GetRows(table, cfgName);
            var filteredRows = FilterRows(rows, rowFilter);

            const int maxRows = 200;
            var showCount = Math.Min(filteredRows.Count, maxRows);
            for (var i = 0; i < showCount; i++)
            {
                DrawRow(filteredRows[i], cfgName, i);
            }

            if (filteredRows.Count > maxRows)
            {
                EditorGUILayout.LabelField($"… 共 {filteredRows.Count} 行，仅显示前 {maxRows} 行");
            }
        }

        /// <summary>
        /// 获取配置表行数据（反射 All 属性，按表名缓存）
        /// </summary>
        /// <param name="table">配置表实例</param>
        /// <param name="cfgName">配置表名称</param>
        /// <returns>行数据列表</returns>
        private List<object> GetRows(object table, string cfgName)
        {
            if (m_RowsCache.TryGetValue(cfgName, out var cached))
            {
                return new List<object>(cached);
            }

            var allProp = table.GetType().GetProperty("All");
            var allValue = allProp?.GetValue(table);
            if (allValue is IEnumerable enumerable)
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    if (item != null) list.Add(item);
                }

                m_RowsCache[cfgName] = list.ToArray();
                return list;
            }

            return new List<object>();
        }

        /// <summary>
        /// 按字段值模糊过滤行数据
        /// </summary>
        /// <param name="rows">行数据列表</param>
        /// <param name="filter">过滤字符串，空则返回全部</param>
        /// <returns>过滤后的行数据列表</returns>
        private static List<object> FilterRows(List<object> rows, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return rows;
            var result = new List<object>();
            foreach (var row in rows)
            {
                if (RowMatches(row, filter)) result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 判断行是否匹配过滤字符串（任一字段值包含即匹配）
        /// </summary>
        /// <param name="row">行对象</param>
        /// <param name="filter">过滤字符串</param>
        /// <returns>是否匹配</returns>
        private static bool RowMatches(object row, string filter)
        {
            foreach (var prop in row.GetType().GetProperties())
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

                if (value != null && value.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 绘制单行（Foldout + 字段键值对）
        /// </summary>
        /// <param name="row">行对象</param>
        /// <param name="cfgName">配置表名称</param>
        /// <param name="index">行序号</param>
        private void DrawRow(object row, string cfgName, int index)
        {
            var foldoutKey = $"{cfgName}|{index}";
            if (!m_RowFoldoutStates.TryGetValue(foldoutKey, out var isOpen))
            {
                isOpen = false;
                m_RowFoldoutStates[foldoutKey] = isOpen;
            }

            m_RowFoldoutStates[foldoutKey] = EditorGUILayout.Foldout(isOpen, GetRowLabel(row, index), true);
            if (!m_RowFoldoutStates[foldoutKey]) return;

            EditorGUILayout.BeginVertical("box");
            {
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
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 获取行标题（优先 Id/Key 属性值，缺失退化为序号）
        /// </summary>
        /// <param name="row">行对象</param>
        /// <param name="index">行序号</param>
        /// <returns>行标题</returns>
        private static string GetRowLabel(object row, int index)
        {
            foreach (var name in new[] { "Id", "Key" })
            {
                var prop = row.GetType().GetProperty(name);
                if (prop != null)
                {
                    var value = prop.GetValue(row);
                    if (value != null) return $"[{index}] {value}";
                }
            }

            return $"[{index}]";
        }

        /// <summary>
        /// 绘制列与列之间的分隔竖线
        /// </summary>
        private static void DrawColumnSeparator()
        {
            GUILayout.Label("|", GUILayout.Width(12));
        }

        #endregion

        #region 反射

        /// <summary>
        /// 确保反射缓存已初始化
        /// </summary>
        /// <returns>初始化成功返回 true</returns>
        private bool EnsureReflection()
        {
            if (m_ModuleInstance != null) return true;

            m_ConfigModuleType = Type.GetType("Hotfix.Framework.Config.ConfigModule, Hotfix");
            if (m_ConfigModuleType == null) return false;

            m_InstanceProperty = m_ConfigModuleType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            m_ModuleInstance = m_InstanceProperty?.GetValue(null);
            if (m_ModuleInstance == null) return false;

            m_ModuleCountProperty = m_ConfigModuleType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            m_CfgNamesProperty    = m_ConfigModuleType.GetProperty("CfgNames", BindingFlags.Public | BindingFlags.Instance);
            m_GetConfigMethod     = m_ConfigModuleType.GetMethod("GetConfig", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);

            return true;
        }

        /// <summary>
        /// 重置反射缓存与展示缓存（停止运行时调用，避免持有失效的热更实例）
        /// </summary>
        private void ResetReflection()
        {
            m_ConfigModuleType    = null;
            m_ModuleInstance      = null;
            m_InstanceProperty    = null;
            m_ModuleCountProperty = null;
            m_CfgNamesProperty    = null;
            m_GetConfigMethod     = null;
            m_TableFoldoutStates.Clear();
            m_RowsCache.Clear();
            m_RowSearchFilters.Clear();
            m_RowFoldoutStates.Clear();
        }

        #endregion
    }
}
#endif
```

- [ ] **Step 2: 校验无移除类功能**

Run:

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Editor/FuFramework/Config"
grep -n "RemoveConfig\|RemoveAllConfigs\|Button(" ConfigModuleWindow.cs
```

Expected: `RemoveConfig`/`RemoveAllConfigs` 无任何引用；`Button(` 仅出现在「刷新」一处（面板无移除/修改按钮）。

- [ ] **Step 3: 校验命名空间与结构对齐**

确认：
- 命名空间 `FuFramework.Config.Editor`，`#if UNITY_EDITOR` 包裹整个文件。
- 与 `Unity/Assets/Editor/FuFramework/ReferencePool/ReferencePoolModuleWindow.cs` 的模式一致（`EditorWindow` 子类 + `[MenuItem]` + `OnEnable/OnDisable` 订阅 `EditorApplication.update` + `EnsureReflection`/`ResetReflection`）。

- [ ] **Step 4: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Editor/FuFramework/Config/ConfigModuleWindow.cs
# 若 Unity 已生成 ConfigModuleWindow.cs.meta，一并 add
git commit -m "[AI]feat: 新增 ConfigModuleWindow 配置调试面板（只读：表列表/行展开/行搜索/加载统计）"
```

---

### Task 4: README 同步

**Files:**
- Modify: `Unity/Assets/Scripts/Hotfix/Framework/Config/README.md`

**Interfaces:**
- Consumes: Task 1 产出的 `ConfigModule` API（`AddConfig` 返回 `bool`、`CfgNames` 返回 `string[]`）
- Produces: 与代码一致的 README 文档

- [ ] **Step 1: 同步接口清单**

在 `README.md` 的「4.1 ConfigModule 核心方法」代码块中做两处替换：

将 `void AddConfig(string cfgName, IDataTable cfgValue)` 替换为 `bool AddConfig(string cfgName, IDataTable cfgValue)`；
将 `IEnumerable<string> CfgNames     // 所有配置表名称` 替换为 `string[] CfgNames        // 所有配置表名称（快照）`。

- [ ] **Step 2: 修正线程安全表述**

将第 8 节「最佳实践」第 4 条：

```markdown
4. **线程安全**：配置数据读取通过 `ConcurrentDictionary` 保证线程安全
```

替换为：

```markdown
4. **只读契约**：配置表在启动期 `LoadConfigAsync` 一次性加载，加载后只读；内部使用普通 `Dictionary`，加载期单线程注册，读取路径无锁且最快
```

- [ ] **Step 3: 更新重复添加与名称约束说明**

将第 9 节「注意事项」第 2 条：

```markdown
2. 重复添加同名配置表会被忽略（`AddConfig` 内部检查存在性）
```

替换为：

```markdown
2. 重复添加同名配置表返回 `false` 并 `FuLogger.LogWarning` 告警；string 版本接口（`GetConfig`/`HasConfig`/`RemoveConfig`）对空名称抛 `ArgumentNullException`
```

并在第 9 节第 1 条后补充一条约束说明：

```markdown
2. 配置表名称 = 类名（`typeof(T).Name` / `nameof(TbXxx)` 必须一致），注册与泛型查询依赖该约定
```

> 说明：原第 1 条已含「配置表名称使用类名…」，此条将其明确为约束；替换后需重新核对第 9 节编号连续。

- [ ] **Step 4: 更新目录结构与新增调试面板章节**

将第 6 节「目录结构」代码块：

```text
Config/
├── ConfigModule.cs           # 配置管理模块
├── BaseDataTable.cs          # 配置表基类 (双重索引)
├── IDataTable.cs             # 配置表接口定义
└── README.md                 # 本文档
```

替换为：

```text
Config/
├── ConfigModule.cs           # 配置管理模块（私有状态 + 生命周期）
├── ConfigModule.API.cs       # 配置管理模块公共 API（分部）
├── BaseDataTable.cs          # 配置表基类 (双重索引)
├── IDataTable.cs             # 配置表接口定义
└── README.md                 # 本文档
```

并在第 6 节后新增一小节（编号顺延）：

```markdown
## 7. 调试面板

编辑器菜单 `FuFramework/调试/配置调试面板` 提供只读配置查询面板（仅 Play 模式）：
- 展示所有配置表及加载信息（类型、行数、long/string key 数量）。
- 展开配置表查看行数据，再展开行查看字段键值对。
- 支持表名搜索过滤、表内字段值模糊搜索、自动刷新。
```

> 说明：原文档第 7-9 节（依赖/最佳实践/注意事项）编号需对应顺延（7→8、8→9、9→10），各节标题与内容不变。

- [ ] **Step 5: 提交**

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0"
git add Unity/Assets/Scripts/Hotfix/Framework/Config/README.md
git commit -m "[AI]docs: 同步 ConfigModule README 接口清单与只读契约，新增调试面板章节"
```

---

### Task 5: 验证与反馈收集

**Files:** 无新增/修改

**Interfaces:**
- Consumes: Task 1-4 全部产出

- [ ] **Step 1: 用户手动编译**

请用户手动触发 Unity 编译。执行者不主动跑 unity-cli 编译。预期：无编译错误（重点确认分部类声明一致、`FuGuardEx` 扩展方法 using、`FuLogger` using、`Dictionary` 容器兼容生成代码）。

- [ ] **Step 2: 全局残留复核**

Run（全库确认无遗留旧符号）：

```bash
cd "D:/_WorkSpace/Unity/FuFramework2.0/Unity/Assets/Scripts"
grep -rn "m_CfgNameTypeDict\|GetTypeName<\|ConfigModule.cs" Hotfix/Framework/Config
```

Expected: 无输出。

- [ ] **Step 3: Play 冒烟 + 调试面板验证**

请用户：进入 Play 模式，框架正常启动、`LoadConfigAsync` 加载成功；打开 `FuFramework/调试/配置调试面板`，验证：
- 配置表列表与总个数正确；
- 展开某表显示加载信息（类型、行数、long/string key 数量）与行数据；
- 展开某行显示字段键值对；
- 表名搜索与行搜索过滤正常；
- 面板无任何移除/修改按钮。

- [ ] **Step 4: 反馈处理**

收集用户编译/运行反馈。若存在编译错误或运行问题，回到对应 Task 修复；全部通过则本计划完成。

---

## Self-Review

### Spec coverage

| 规格要求 | 对应任务 |
|---|---|
| ConfigModule 拆 ConfigModule.API.cs 分部（§3.1） | Task 1 |
| ConcurrentDictionary → Dictionary 只读契约（§2.2/§4.1） | Task 1 |
| 删除 m_CfgNameTypeDict/GetTypeName（§4.2） | Task 1 Step 1 |
| GetConfig<T> 单次查询（§4.2） | Task 1 Step 2 |
| AddConfig 返回 bool + FuLogger 告警 + 双卫语句（§4.2） | Task 1 Step 2 |
| CfgNames 快照 string[]（§4.2） | Task 1 Step 2 |
| string 版本空名卫语句（§4.2/§4.3） | Task 1 Step 2 |
| BaseDataTable SortedDictionary → Dictionary（§5） | Task 2 |
| All/ToArray 复用（§5） | Task 2 |
| IDataTable 全成员保留（§2.2） | 未改 IDataTable.cs，满足 |
| 不动 Luban 生成器（§2.2） | 未改生成代码/模板，满足 |
| ConfigModuleWindow 调试面板（§6，只读/行浏览器/行搜索/加载统计） | Task 3 |
| README 同步（§7） | Task 4 |
| 验证方式：用户手动编译后反馈（§8） | Task 5 |

### Placeholder scan

所有代码步骤均含完整可编译代码；无 "TBD"/"TODO"/"实现细节待定" 类占位。

### Type consistency

- `GetConfig(string)` 在 Task 1 定义为返回 `IDataTable` 并带 `NotNullOrEmpty` 卫语句；Task 3 窗口经反射调用 `GetConfig(string)` 且从不传空串——一致。
- `CfgNames` 返回 `string[]`：Task 1 定义，Task 3 用 `as string[]` 接收——一致。
- `AddConfig` 返回 `bool`：Task 1 定义，Task 4 README 同步——一致。
- `LongKeyDataDict`/`StrKeyDataDict` 字段名：Task 2 保持，Task 3 窗口反射读取同名——一致。
