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
					object value;
					try
					{
						value = prop.GetValue(row);
					}
					catch (Exception)
					{
						continue;
					}

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
