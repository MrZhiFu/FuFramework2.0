#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using FuMenuPriority   = FuFramework.Core.Editor.FuMenuPriority;
using DebugWindowBase  = FuFramework.Core.Editor.DebugWindowBase;
using HotfixReflection = FuFramework.Core.Editor.HotfixReflection;

// ReSharper disable once CheckNamespace
namespace FuFramework.RedDot.Editor
{
	/// <summary>
	/// 红点调试面板
	/// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的 RedDotModule。
	/// </summary>
	public sealed class RedDotDebugWindow : DebugWindowBase
	{
		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/红点调试面板", false, FuMenuPriority.DEBUG_PANEL_RED_DOT)]
		public static void ShowWindow()
		{
			var window = GetWindow<RedDotDebugWindow>("红点调试");

			// 初始位置居中显示，初始宽高 1000x800
			const float width  = 1000f;
			const float height = 800f;
			var x = (Screen.currentResolution.width  - width)  / 2f;
			var y = (Screen.currentResolution.height - height) / 2f;
			window.position = new Rect(x, y, width, height);
		}

		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "RedDotModule";

		#endregion

		#region 私有字段

		/// <summary>
		/// 节点折叠状态缓存
		/// </summary>
		private readonly Dictionary<object, bool> m_foldoutStates = new();

		#endregion

		#region 反射缓存

		/// <summary>
		/// RedDotModule 类型
		/// </summary>
		private Type m_moduleType;

		/// <summary>
		/// RedDotModule 实例
		/// </summary>
		private object m_moduleInstance;

		/// <summary>
		/// 获取所有节点的方法
		/// </summary>
		private MethodInfo m_getAllNodesMethod;

		/// <summary>
		/// 获取节点状态的方法
		/// </summary>
		private MethodInfo m_getStateMethod;

		/// <summary>
		/// 标记节点为已读的方法
		/// </summary>
		private MethodInfo m_markReadMethod;

		/// <summary>
		/// 获取子节点的方法
		/// </summary>
		private MethodInfo m_getChildrenMethod;

		/// <summary>
		/// 节点Key属性
		/// </summary>
		private PropertyInfo m_keyProperty;

		/// <summary>
		/// 父节点属性
		/// </summary>
		private PropertyInfo m_parentProperty;

		/// <summary>
		/// 节点的原始计数（自身，不含子节点）
		/// </summary>
		private PropertyInfo m_rawCountProperty;

		/// <summary>
		/// 节点的总计数（自身 + 所有子节点）
		/// </summary>
		private PropertyInfo m_totalCountProperty;

		/// <summary>
		/// 是否是静态节点属性
		/// </summary>
		private PropertyInfo m_isStaticProperty;

		/// <summary>
		/// 节点是否激活属性
		/// </summary>
		private PropertyInfo m_isActiveProperty;

		/// <summary>
		/// 节点是否已读属性
		/// </summary>
		private PropertyInfo m_isReadProperty;

		/// <summary>
		/// 节点脏标记属性
		/// </summary>
		private PropertyInfo m_isDirtyProperty;

		/// <summary>
		/// 子节点聚合逻辑属性
		/// </summary>
		private PropertyInfo m_logicTypeProperty;

		/// <summary>
		/// 节点清理策略属性
		/// </summary>
		private PropertyInfo m_cleanStrategyProperty;

		/// <summary>
		/// 显示模式属性
		/// </summary>
		private PropertyInfo m_displayModeProperty;

		/// <summary>
		/// 计算逻辑属性
		/// </summary>
		private PropertyInfo m_calculatorProperty;

		/// <summary>
		/// 触发重算的事件ID列表属性
		/// </summary>
		private PropertyInfo m_triggerEventsProperty;

		#endregion

		#region 主体绘制

		/// <summary>
		/// 绘制红点树主体（基类滚动容器内调用）
		/// </summary>
		protected override void DrawContent()
		{
			var nodes = GetAllNodes();
			if (nodes == null || nodes.Count == 0)
			{
				EditorGUILayout.HelpBox("红点树为空", MessageType.Info);
				return;
			}

			// 绘制节点树
			foreach (var node in nodes)
			{
				if (GetParent(node) == null)
				{
					DrawNodeTree(node, 0);
				}
			}
		}

		#endregion

		#region 节点绘制

		/// <summary>
		/// 递归绘制节点树
		/// </summary>
		/// <param name="node">当前节点</param>
		/// <param name="indentLevel">缩进层级</param>
		private void DrawNodeTree(object node, int indentLevel)
		{
			if (node == null) return;

			// 检查是否需要过滤
			var keyString = GetKeyString(node);
			if (!string.IsNullOrEmpty(m_searchFilter))
			{
				if (!keyString.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase))
				{
					// 如果当前节点不匹配，检查子节点是否匹配
					var childMatches = false;
					foreach (var child in GetChildren(node))
					{
						if (!GetKeyString(child).Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)) continue;
						childMatches = true;
						break;
					}

					if (!childMatches) return;
				}
			}

			// 获取节点的所有子节点
			var children = GetChildren(node);

			var hasChildren = children.Count > 0;

			var totalCount = (int)(m_totalCountProperty?.GetValue(node) ?? 0);
			var rawCount   = (int)(m_rawCountProperty?.GetValue(node)   ?? 0);
			var isActive   = (bool)(m_isActiveProperty?.GetValue(node)  ?? true);
			var isRead     = (bool)(m_isReadProperty?.GetValue(node)    ?? false);
			var isDirty    = (bool)(m_isDirtyProperty?.GetValue(node)   ?? false);
			var isStatic   = (bool)(m_isStaticProperty?.GetValue(node)  ?? false);

			var logicType     = m_logicTypeProperty?.GetValue(node)?.ToString()     ?? "-";
			var cleanStrategy = m_cleanStrategyProperty?.GetValue(node)?.ToString() ?? "-";
			var displayMode   = m_displayModeProperty?.GetValue(node)?.ToString()   ?? "-";

			var hasCalculator = m_calculatorProperty?.GetValue(node) != null;

			var triggerEvents    = (string[])m_triggerEventsProperty?.GetValue(node);
			var triggerEventsStr = triggerEvents is { Length: > 0 } ? string.Join(",", triggerEvents) : "";

			EditorGUILayout.BeginHorizontal();

			// Key 区域：固定宽度子 Horizontal，缩进在内部，所有层级信息列起点一致
			const int keyAreaWidth = 260;
			EditorGUILayout.BeginHorizontal(GUILayout.Width(keyAreaWidth));
			GUILayout.Space(indentLevel * 20);
			if (hasChildren)
			{
				if (!m_foldoutStates.TryGetValue(node, out var foldout))
				{
					foldout               = true;
					m_foldoutStates[node] = true;
				}

				m_foldoutStates[node] = EditorGUILayout.Foldout(foldout, keyString, true);
			}
			else
			{
				GUILayout.Label(keyString);
			}

			EditorGUILayout.EndHorizontal();

			// 节点信息列：所有行的以下列宽固定，靠左排列
			GUILayout.Label(isStatic ? "静态" : "动态", GUILayout.Width(40));

			var oldColor = GUI.color;
			GUI.color = isActive ? Color.green : Color.gray;
			GUILayout.Label(isActive ? "激活" : "未激活", GUILayout.Width(50));
			GUI.color = oldColor;

			GUILayout.Label($"最终计数: {totalCount}",    GUILayout.Width(80));
			GUILayout.Label($"原始计数: {rawCount}",      GUILayout.Width(80));
			GUILayout.Label($"聚合逻辑: {logicType}",     GUILayout.Width(90));
			GUILayout.Label($"显示模式: {displayMode}",   GUILayout.Width(130));
			GUILayout.Label($"清理策略: {cleanStrategy}", GUILayout.Width(110));

			if (isRead)
				GUILayout.Label("已读", GUILayout.Width(40));
			if (isDirty)
				GUILayout.Label("脏", GUILayout.Width(30));
			if (hasCalculator)
				GUILayout.Label("有计算函数", GUILayout.Width(80));
			if (!string.IsNullOrEmpty(triggerEventsStr))
				GUILayout.Label($"触发重算事件: {triggerEventsStr}", GUILayout.Width(200));

			// 操作按钮：设置为已读
			if (isStatic && GUILayout.Button("已读", GUILayout.Width(50)))
			{
				var key = m_keyProperty?.GetValue(node);
				if (key != null)
					m_markReadMethod?.Invoke(m_moduleInstance, new[] { key });
			}

			// 操作按钮：刷新状态
			if (GUILayout.Button("刷新", GUILayout.Width(50)))
			{
				var key = m_keyProperty?.GetValue(node);
				if (key != null)
					m_getStateMethod?.Invoke(m_moduleInstance, new[] { key });
			}

			EditorGUILayout.EndHorizontal();

			// 折叠/展开子节点
			if (hasChildren && m_foldoutStates.TryGetValue(node, out var open) && open)
			{
				foreach (var child in children)
				{
					DrawNodeTree(child, indentLevel + 1);
				}
			}
		}

		/// <summary>
		/// 全部展开
		/// </summary>
		protected override void ExpandAll()
		{
			var nodes = GetAllNodes();
			if (nodes == null) return;
			SetAllFoldouts(nodes, m_foldoutStates, true);
		}

		/// <summary>
		/// 全部折叠
		/// </summary>
		protected override void CollapseAll()
		{
			var nodes = GetAllNodes();
			if (nodes == null) return;
			SetAllFoldouts(nodes, m_foldoutStates, false);
		}

		#endregion

		#region 反射

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_moduleType != null && m_moduleInstance != null) return true;

			m_moduleType = HotfixReflection.RedDotModule;
			if (m_moduleType == null) return false;

			m_moduleInstance = HotfixReflection.GetStaticInstance(m_moduleType);
			if (m_moduleInstance == null) return false;

			var nodeType = HotfixReflection.RedDotNode;
			if (nodeType == null) return false;

			var keyType = HotfixReflection.RedDotKey;
			if (keyType == null) return false;

			m_getAllNodesMethod = m_moduleType.GetMethod("GetAllNodes", BindingFlags.Public | BindingFlags.Instance);
			m_getStateMethod    = m_moduleType.GetMethod("GetState",    BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType }, null);
			m_markReadMethod    = m_moduleType.GetMethod("MarkRead",    BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType }, null);

			m_getChildrenMethod = nodeType.GetMethod("GetChildren", BindingFlags.Public | BindingFlags.Instance);

			m_keyProperty           = nodeType.GetProperty("Key",           BindingFlags.Public | BindingFlags.Instance);
			m_parentProperty        = nodeType.GetProperty("Parent",        BindingFlags.Public | BindingFlags.Instance);
			m_rawCountProperty      = nodeType.GetProperty("RawCount",      BindingFlags.Public | BindingFlags.Instance);
			m_totalCountProperty    = nodeType.GetProperty("TotalCount",    BindingFlags.Public | BindingFlags.Instance);
			m_isActiveProperty      = nodeType.GetProperty("IsActive",      BindingFlags.Public | BindingFlags.Instance);
			m_isReadProperty        = nodeType.GetProperty("IsRead",        BindingFlags.Public | BindingFlags.Instance);
			m_isDirtyProperty       = nodeType.GetProperty("IsDirty",       BindingFlags.Public | BindingFlags.Instance);
			m_logicTypeProperty     = nodeType.GetProperty("LogicType",     BindingFlags.Public | BindingFlags.Instance);
			m_cleanStrategyProperty = nodeType.GetProperty("CleanStrategy", BindingFlags.Public | BindingFlags.Instance);
			m_displayModeProperty   = nodeType.GetProperty("DisplayMode",   BindingFlags.Public | BindingFlags.Instance);
			m_calculatorProperty    = nodeType.GetProperty("Calculator",    BindingFlags.Public | BindingFlags.Instance);
			m_triggerEventsProperty = nodeType.GetProperty("TriggerEvents", BindingFlags.Public | BindingFlags.Instance);
			m_isStaticProperty      = nodeType.GetProperty("IsStatic",      BindingFlags.Public | BindingFlags.Instance);

			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_moduleType            = null;
			m_moduleInstance        = null;
			m_getAllNodesMethod     = null;
			m_getStateMethod        = null;
			m_markReadMethod        = null;
			m_getChildrenMethod     = null;
			m_keyProperty           = null;
			m_parentProperty        = null;
			m_rawCountProperty      = null;
			m_totalCountProperty    = null;
			m_isActiveProperty      = null;
			m_isReadProperty        = null;
			m_isDirtyProperty       = null;
			m_logicTypeProperty     = null;
			m_cleanStrategyProperty = null;
			m_displayModeProperty   = null;
			m_calculatorProperty    = null;
			m_triggerEventsProperty = null;
			m_isStaticProperty      = null;

			m_foldoutStates.Clear();
		}

		/// <summary>
		/// 获取所有节点
		/// </summary>
		/// <returns>所有红点节点列表</returns>
		private List<object> GetAllNodes()
		{
			var nodes = new List<object>();
			var result = m_getAllNodesMethod?.Invoke(m_moduleInstance, null);
			if (result is IEnumerable<object> enumerable)
			{
				foreach (var node in enumerable)
				{
					nodes.Add(node);
				}
			}
			else if (result is IEnumerable rawEnumerable)
			{
				foreach (var node in rawEnumerable)
				{
					if (node != null) nodes.Add(node);
				}
			}

			return nodes;
		}

		/// <summary>
		/// 获取节点父节点
		/// </summary>
		/// <param name="node">目标节点</param>
		/// <returns>父节点，无父节点时返回 null</returns>
		private object GetParent(object node) => m_parentProperty?.GetValue(node);

		/// <summary>
		/// 获取节点的所有子节点
		/// </summary>
		/// <param name="node">目标节点</param>
		/// <returns>子节点集合</returns>
		private List<object> GetChildren(object node)
		{
			var children = new List<object>();
			var value    = m_getChildrenMethod?.Invoke(node, null);
			if (value is IEnumerable<object> enumerable)
			{
				foreach (var child in enumerable)
				{
					children.Add(child);
				}
			}
			else if (value is IEnumerable rawEnumerable)
			{
				foreach (var child in rawEnumerable)
				{
					if (child != null) children.Add(child);
				}
			}

			return children;
		}

		/// <summary>
		/// 获取节点的 Key 字符串
		/// </summary>
		/// <param name="node">目标节点</param>
		/// <returns>Key 字符串，无法获取时返回 &lt;null&gt;</returns>
		private string GetKeyString(object node)
		{
			var key = m_keyProperty?.GetValue(node);
			return key?.ToString() ?? "<null>";
		}

		#endregion
	}
}
#endif