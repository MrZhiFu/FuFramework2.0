#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
	///     4. 支持实时编辑配置行简单值字段（Id/Key 锁定、字段级撤销、编辑高亮）。
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

		/// <summary>
		/// 数值字段在途编辑文本缓存：行引用 → 属性名 → 用户正在输入的文本。
		/// 数值字段用 TextField + TryParse 校验，仅在文本可完整解析时提交，避免 Unity 数值控件对非法输入提交 0。
		/// </summary>
		private readonly Dictionary<object, Dictionary<string, string>> m_FieldEditText = new();

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

			if (GUILayout.Button("记录变更", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				ExportChangeLog();
			}

			if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
			{
				Repaint();
			}

			if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				ExpandAllTables();
			}

			if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				CollapseAllTables();
			}

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 全部展开：展开所有配置表的折叠项。
		/// </summary>
		private void ExpandAllTables()
		{
			var cfgNames = m_CfgNamesProperty?.GetValue(m_ModuleInstance) as string[];
			if (cfgNames == null) return;
			foreach (var cfgName in cfgNames)
			{
				m_TableFoldoutStates[cfgName] = true;
			}
		}

		/// <summary>
		/// 全部折叠：折叠所有配置表的折叠项。
		/// </summary>
		private void CollapseAllTables()
		{
			var cfgNames = m_CfgNamesProperty?.GetValue(m_ModuleInstance) as string[];
			if (cfgNames == null) return;
			foreach (var cfgName in cfgNames)
			{
				m_TableFoldoutStates[cfgName] = false;
			}
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

		/// <summary>
		/// 渲染属性对应控件并返回新值。
		/// string/bool/enum 用类型化控件；数值类型用 TextField + TryParse 校验（非法输入不提交）。
		/// </summary>
		/// <param name="row">行对象</param>
		/// <param name="prop">属性</param>
		/// <param name="currentValue">当前值</param>
		/// <returns>新值（非法数值输入返回 currentValue，值不变）</returns>
		private object RenderFieldControl(object row, PropertyInfo prop, object currentValue)
		{
			var type = prop.PropertyType;

			if (type == typeof(string))
				return EditorGUILayout.TextField((string)currentValue, GUILayout.MinWidth(120));

			if (type == typeof(bool))
				return EditorGUILayout.Toggle((bool)currentValue, GUILayout.MinWidth(120));

			if (type.IsEnum)
				return EditorGUILayout.EnumPopup((Enum)currentValue, GUILayout.MinWidth(120));

			// 数值类型：TextField + TryParse 校验——仅在文本可完整解析时提交；
			// 非法输入不提交（值不变）；失焦时恢复显示原配置值，避免非法文本红框停留。
			if (IsNumericType(type))
			{
				if (!m_FieldEditText.TryGetValue(row, out var editDict))
				{
					editDict = new Dictionary<string, string>();
					m_FieldEditText[row] = editDict;
				}

				if (!editDict.TryGetValue(prop.Name, out var pending))
					pending = Convert.ToString(currentValue, CultureInfo.InvariantCulture);

				// 唯一控件名（按行引用 + 属性名），用于失焦检测
				var controlName = $"cfg_edit_{RuntimeHelpers.GetHashCode(row)}_{prop.Name}";
				GUI.SetNextControlName(controlName);
				var text = EditorGUILayout.TextField(pending, GUILayout.MinWidth(120));
				editDict[prop.Name] = text;

				// 失焦且文本非法：清除在途文本，恢复显示原配置值（值保持 currentValue 不变）
				if (GUI.GetNameOfFocusedControl() != controlName && !TryParseNumeric(type, text, out _))
				{
					editDict.Remove(prop.Name);
					return currentValue;
				}

				return TryParseNumeric(type, text, out var parsed) ? parsed : currentValue;
			}

			return currentValue;
		}

		/// <summary>
		/// 判断类型是否为数值类型（走 TextField + TryParse 校验路径）。
		/// </summary>
		/// <param name="type">属性类型</param>
		/// <returns>是否数值类型</returns>
		private static bool IsNumericType(Type type)
		{
			return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
				|| type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
				|| type == typeof(float) || type == typeof(double);
		}

		/// <summary>
		/// 尝试把文本解析为指定数值类型（不变文化）；解析失败或超出范围返回 false。
		/// </summary>
		/// <param name="type">目标类型</param>
		/// <param name="text">文本</param>
		/// <param name="value">解析结果</param>
		/// <returns>是否解析成功</returns>
		private static bool TryParseNumeric(Type type, string text, out object value)
		{
			value = null;
			try
			{
				if (type == typeof(int))
				{
					if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { value = v; return true; }
				}
				else if (type == typeof(long))
				{
					if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { value = v; return true; }
				}
				else if (type == typeof(float))
				{
					if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) { value = v; return true; }
				}
				else if (type == typeof(double))
				{
					if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) { value = v; return true; }
				}
				else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort))
				{
					if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { value = Convert.ChangeType(v, type); return true; }
				}
				else if (type == typeof(uint))
				{
					// uint 用 long.TryParse 覆盖全范围（0..uint.MaxValue），避免 > int.MaxValue 时无法编辑
					if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v >= 0 && v <= uint.MaxValue)
					{
						value = (uint)v;
						return true;
					}
				}
				else if (type == typeof(ulong))
				{
					if (ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { value = v; return true; }
				}
			}
			catch (Exception)
			{
				value = null;
				return false;
			}

			return false;
		}

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
			// 数值字段在途文本非法（TryParse 失败）→ 红色提示，值不提交
			var isInvalidInput = IsNumericType(prop.PropertyType)
					     && m_FieldEditText.TryGetValue(row, out var editDict)
					     && editDict.TryGetValue(prop.Name, out var editText)
					     && !TryParseNumeric(prop.PropertyType, editText, out _);

			// 已编辑字段黄色高亮；写失败/非法输入红色（优先显示）
			var fieldOldColor = GUI.color;
			if (isWriteFail || isInvalidInput) GUI.color = Color.red;
			else if (isEdited) GUI.color = Color.yellow;

			object newValue;
			try
			{
				newValue = RenderFieldControl(row, prop, currentValue);
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
				try
				{
					prop.SetValue(row, newValue);
					// 写成功后记录原值；写失败不缓存，避免出现误导性的"已编辑"高亮与重置按钮
					if (origDict == null)
					{
						origDict = new Dictionary<string, object>();
						m_FieldOriginalValues[row] = origDict;
					}

					if (!origDict.ContainsKey(prop.Name))
					{
						origDict[prop.Name] = currentValue;
					}
					else if (Equals(newValue, origDict[prop.Name]))
					{
						// 值改回原值：清除撤销缓存条目（面板高亮消失，导出不再出现无意义变更）
						origDict.Remove(prop.Name);
						if (origDict.Count == 0) m_FieldOriginalValues.Remove(row);
					}

					if (failDict != null && failDict.Remove(prop.Name) && failDict.Count == 0)
						m_WriteFailTimes.Remove(row);
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
						// 重置后清空该字段在途编辑文本，避免下一帧从 m_FieldEditText 读回旧文本再次提交（击穿重置）
						if (m_FieldEditText.TryGetValue(row, out var pendingEditDict)) pendingEditDict.Remove(prop.Name);
						origDict.Remove(prop.Name);
						if (origDict.Count == 0) m_FieldOriginalValues.Remove(row);
						if (failDict != null && failDict.Remove(prop.Name) && failDict.Count == 0)
							m_WriteFailTimes.Remove(row);
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

						// 跳过旧值等于新值的无意义条目（防御撤销缓存未清理的残留）
						if (Equals(kv.Value, newValue)) continue;
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
			m_FieldOriginalValues.Clear();
			m_WriteFailTimes.Clear();
			m_FieldEditText.Clear();
		}

		#endregion
	}
}
#endif
