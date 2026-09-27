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
		private readonly Dictionary<object, bool> m_FoldoutStates = new();

		#endregion

		#region 反射缓存

		/// <summary>
		/// RedDotModule 类型
		/// </summary>
		private Type m_ModuleType;

		/// <summary>
		/// RedDotModule 实例
		/// </summary>
		private object m_ModuleInstance;

		/// <summary>
		/// 获取所有节点的方法
		/// </summary>
		private MethodInfo m_GetAllNodesMethod;

		/// <summary>
		/// 获取节点状态的方法
		/// </summary>
		private MethodInfo m_GetStateMethod;

		/// <summary>
		/// 标记节点为已读的方法
		/// </summary>
		private MethodInfo m_MarkReadMethod;

		/// <summary>
		/// 获取子节点的方法
		/// </summary>
		private MethodInfo m_GetChildrenMethod;

		/// <summary>
		/// 节点Key属性
		/// </summary>
		private PropertyInfo m_KeyProperty;

		/// <summary>
		/// 父节点属性
		/// </summary>
		private PropertyInfo m_ParentProperty;

		/// <summary>
		/// 节点的原始计数（自身，不含子节点）
		/// </summary>
		private PropertyInfo m_RawCountProperty;

		/// <summary>
		/// 节点的总计数（自身 + 所有子节点）
		/// </summary>
		private PropertyInfo m_TotalCountProperty;

		/// <summary>
		/// 是否是静态节点属性
		/// </summary>
		private PropertyInfo m_IsStaticProperty;

		/// <summary>
		/// 节点是否激活属性
		/// </summary>
		private PropertyInfo m_IsActiveProperty;

		/// <summary>
		/// 节点是否已读属性
		/// </summary>
		private PropertyInfo m_IsReadProperty;

		/// <summary>
		/// 节点脏标记属性
		/// </summary>
		private PropertyInfo m_IsDirtyProperty;

		/// <summary>
		/// 子节点聚合逻辑属性
		/// </summary>
		private PropertyInfo m_LogicTypeProperty;

		/// <summary>
		/// 节点清理策略属性
		/// </summary>
		private PropertyInfo m_CleanStrategyProperty;

		/// <summary>
		/// 显示模式属性
		/// </summary>
		private PropertyInfo m_DisplayModeProperty;

		/// <summary>
		/// 计算逻辑属性
		/// </summary>
		private PropertyInfo m_CalculatorProperty;

		/// <summary>
		/// 触发重算的事件ID列表属性
		/// </summary>
		private PropertyInfo m_TriggerEventsProperty;

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
			if (!string.IsNullOrEmpty(m_SearchFilter))
			{
				if (!keyString.Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase))
				{
					// 如果当前节点不匹配，检查子节点是否匹配
					bool childMatches = false;
					foreach (var child in GetChildren(node))
					{
						if (!GetKeyString(child).Contains(m_SearchFilter, StringComparison.OrdinalIgnoreCase)) continue;
						childMatches = true;
						break;
					}

					if (!childMatches) return;
				}
			}

			// 获取节点的所有子节点
			var children = GetChildren(node);

			var hasChildren = children.Count > 0;

			var totalCount = (int)(m_TotalCountProperty?.GetValue(node) ?? 0);
			var rawCount   = (int)(m_RawCountProperty?.GetValue(node)   ?? 0);
			var isActive   = (bool)(m_IsActiveProperty?.GetValue(node)  ?? true);
			var isRead     = (bool)(m_IsReadProperty?.GetValue(node)    ?? false);
			var isDirty    = (bool)(m_IsDirtyProperty?.GetValue(node)   ?? false);
			var isStatic   = (bool)(m_IsStaticProperty?.GetValue(node)  ?? false);

			var logicType     = m_LogicTypeProperty?.GetValue(node)?.ToString()     ?? "-";
			var cleanStrategy = m_CleanStrategyProperty?.GetValue(node)?.ToString() ?? "-";
			var displayMode   = m_DisplayModeProperty?.GetValue(node)?.ToString()   ?? "-";

			var hasCalculator = m_CalculatorProperty?.GetValue(node) != null;

			var triggerEvents    = (string[])m_TriggerEventsProperty?.GetValue(node);
			var triggerEventsStr = triggerEvents is { Length: > 0 } ? string.Join(",", triggerEvents) : "";

			EditorGUILayout.BeginHorizontal();

			// Key 区域：固定宽度子 Horizontal，缩进在内部，所有层级信息列起点一致
			const int keyAreaWidth = 260;
			EditorGUILayout.BeginHorizontal(GUILayout.Width(keyAreaWidth));
			GUILayout.Space(indentLevel * 20);
			if (hasChildren)
			{
				if (!m_FoldoutStates.TryGetValue(node, out var foldout))
				{
					foldout               = true;
					m_FoldoutStates[node] = true;
				}

				m_FoldoutStates[node] = EditorGUILayout.Foldout(foldout, keyString, true);
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
				var key = m_KeyProperty?.GetValue(node);
				if (key != null)
					m_MarkReadMethod?.Invoke(m_ModuleInstance, new[] { key });
			}

			// 操作按钮：刷新状态
			if (GUILayout.Button("刷新", GUILayout.Width(50)))
			{
				var key = m_KeyProperty?.GetValue(node);
				if (key != null)
					m_GetStateMethod?.Invoke(m_ModuleInstance, new[] { key });
			}

			EditorGUILayout.EndHorizontal();

			// 折叠/展开子节点
			if (hasChildren && m_FoldoutStates.TryGetValue(node, out var open) && open)
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
			SetAllFoldouts(nodes, m_FoldoutStates, true);
		}

		/// <summary>
		/// 全部折叠
		/// </summary>
		protected override void CollapseAll()
		{
			var nodes = GetAllNodes();
			if (nodes == null) return;
			SetAllFoldouts(nodes, m_FoldoutStates, false);
		}

		#endregion

		#region 反射

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_ModuleType != null && m_ModuleInstance != null) return true;

			m_ModuleType = HotfixReflection.RedDotModule;
			if (m_ModuleType == null) return false;

			m_ModuleInstance = HotfixReflection.GetStaticInstance(m_ModuleType);
			if (m_ModuleInstance == null) return false;

			var nodeType = HotfixReflection.RedDotNode;
			if (nodeType == null) return false;

			var keyType = HotfixReflection.RedDotKey;
			if (keyType == null) return false;

			m_GetAllNodesMethod = m_ModuleType.GetMethod("GetAllNodes", BindingFlags.Public | BindingFlags.Instance);
			m_GetStateMethod    = m_ModuleType.GetMethod("GetState",    BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType }, null);
			m_MarkReadMethod    = m_ModuleType.GetMethod("MarkRead",    BindingFlags.Public | BindingFlags.Instance, null, new[] { keyType }, null);

			m_GetChildrenMethod = nodeType.GetMethod("GetChildren", BindingFlags.Public | BindingFlags.Instance);

			m_KeyProperty           = nodeType.GetProperty("Key",           BindingFlags.Public | BindingFlags.Instance);
			m_ParentProperty        = nodeType.GetProperty("Parent",        BindingFlags.Public | BindingFlags.Instance);
			m_RawCountProperty      = nodeType.GetProperty("RawCount",      BindingFlags.Public | BindingFlags.Instance);
			m_TotalCountProperty    = nodeType.GetProperty("TotalCount",    BindingFlags.Public | BindingFlags.Instance);
			m_IsActiveProperty      = nodeType.GetProperty("IsActive",      BindingFlags.Public | BindingFlags.Instance);
			m_IsReadProperty        = nodeType.GetProperty("IsRead",        BindingFlags.Public | BindingFlags.Instance);
			m_IsDirtyProperty       = nodeType.GetProperty("IsDirty",       BindingFlags.Public | BindingFlags.Instance);
			m_LogicTypeProperty     = nodeType.GetProperty("LogicType",     BindingFlags.Public | BindingFlags.Instance);
			m_CleanStrategyProperty = nodeType.GetProperty("CleanStrategy", BindingFlags.Public | BindingFlags.Instance);
			m_DisplayModeProperty   = nodeType.GetProperty("DisplayMode",   BindingFlags.Public | BindingFlags.Instance);
			m_CalculatorProperty    = nodeType.GetProperty("Calculator",    BindingFlags.Public | BindingFlags.Instance);
			m_TriggerEventsProperty = nodeType.GetProperty("TriggerEvents", BindingFlags.Public | BindingFlags.Instance);
			m_IsStaticProperty      = nodeType.GetProperty("IsStatic",      BindingFlags.Public | BindingFlags.Instance);

			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_ModuleType            = null;
			m_ModuleInstance        = null;
			m_GetAllNodesMethod     = null;
			m_GetStateMethod        = null;
			m_MarkReadMethod        = null;
			m_GetChildrenMethod     = null;
			m_KeyProperty           = null;
			m_ParentProperty        = null;
			m_RawCountProperty      = null;
			m_TotalCountProperty    = null;
			m_IsActiveProperty      = null;
			m_IsReadProperty        = null;
			m_IsDirtyProperty       = null;
			m_LogicTypeProperty     = null;
			m_CleanStrategyProperty = null;
			m_DisplayModeProperty   = null;
			m_CalculatorProperty    = null;
			m_TriggerEventsProperty = null;
			m_IsStaticProperty      = null;

			m_FoldoutStates.Clear();
		}

		/// <summary>
		/// 获取所有节点
		/// </summary>
		/// <returns>所有红点节点列表</returns>
		private List<object> GetAllNodes()
		{
			var nodes = new List<object>();
			var result = m_GetAllNodesMethod?.Invoke(m_ModuleInstance, null);
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
		private object GetParent(object node) => m_ParentProperty?.GetValue(node);

		/// <summary>
		/// 获取节点的所有子节点
		/// </summary>
		/// <param name="node">目标节点</param>
		/// <returns>子节点集合</returns>
		private List<object> GetChildren(object node)
		{
			var children = new List<object>();
			var value    = m_GetChildrenMethod?.Invoke(node, null);
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
			var key = m_KeyProperty?.GetValue(node);
			return key?.ToString() ?? "<null>";
		}

		#endregion
	}
}
#endif