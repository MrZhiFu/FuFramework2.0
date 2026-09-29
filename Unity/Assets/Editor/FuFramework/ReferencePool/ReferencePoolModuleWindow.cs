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
namespace FuFramework.ReferencePool.Editor
{
	/// <summary>
	/// 引用池调试面板
	/// 仅在 Play 模式下可用，通过反射访问 Hotfix 中的静态基座 ReferencePool。
	/// 功能：
	///     1. 展示所有引用池及其统计信息（类型、闲置、使用中、累计获取/释放/新增/移除）。
	///     2. 支持按类型名过滤搜索、自动刷新、全部展开/折叠。
	///     3. 支持模块级/单池级一键移除（移除所有闲置引用、移除指定类型闲置引用）。
	/// 注意：ReferencePool.ClearAll 的语义是「只清闲置、保留类型条目与累计计数」，故一键移除后类型条目数不归零，
	/// 面板按「闲置为 0 且无在用」过滤掉无可展示内容的空集合，与列表口径保持一致。
	/// </summary>
	public sealed class ReferencePoolModuleWindow : DebugWindowBase
	{
		/// <summary>
		/// 打开调试面板
		/// </summary>
		[MenuItem("FuFramework/调试/引用池调试面板", false, FuMenuPriority.DEBUG_PANEL_REFERENCE_POOL)]
		public static void ShowWindow()
		{
			var window = GetWindow<ReferencePoolModuleWindow>("引用池调试");
			window.minSize = new Vector2(800, 800);

			// 初始位置居中显示
			const float width  = 1000f;
			const float height = 600f;
			var x = (Screen.currentResolution.width  - width)  / 2f;
			var y = (Screen.currentResolution.height - height) / 2f;
			window.position = new Rect(x, y, width, height);
		}

		#region 基类配置

		/// <summary>
		/// 模块显示名（反射初始化失败提示用）
		/// </summary>
		protected override string ModuleDisplayName => "ReferencePool";

		#endregion

		#region 私有字段

		/// <summary>
		/// 引用池折叠状态缓存。ReferencePoolInfo 是 struct，每次查询返回新副本，须以稳定的 Type 为键。
		/// </summary>
		private readonly Dictionary<Type, bool> m_foldoutStates = new();

		#endregion

		#region 反射缓存

		/// <summary>
		/// ReferencePool 静态类型
		/// </summary>
		private Type m_referencePoolType;

		/// <summary>
		/// ReferencePoolInfo 类型
		/// </summary>
		private Type m_referencePoolInfoType;

		/// <summary>
		/// ReferencePool.Count 属性（静态）
		/// </summary>
		private PropertyInfo m_moduleCountProperty;

		/// <summary>
		/// ReferencePool.GetAllReferencePoolInfos 方法（静态）
		/// </summary>
		private MethodInfo m_getAllReferencePoolInfosMethod;

		/// <summary>
		/// ReferencePool.ClearAll 方法（静态）
		/// </summary>
		private MethodInfo m_moduleRemoveAllPoolsMethod;

		/// <summary>
		/// ReferencePool.RemoveAllUnused 泛型方法定义（静态）
		/// </summary>
		private MethodInfo m_removeAllUnusedGenericMethod;

		/// <summary>
		/// ReferencePoolInfo.Type 属性
		/// </summary>
		private PropertyInfo m_infoTypeProperty;

		/// <summary>
		/// ReferencePoolInfo.UnusedReferenceCount 属性
		/// </summary>
		private PropertyInfo m_infoUnusedReferenceCountProperty;

		/// <summary>
		/// ReferencePoolInfo.UsingReferenceCount 属性
		/// </summary>
		private PropertyInfo m_infoUsingReferenceCountProperty;

		/// <summary>
		/// ReferencePoolInfo.AcquireReferenceCount 属性
		/// </summary>
		private PropertyInfo m_infoAcquireReferenceCountProperty;

		/// <summary>
		/// ReferencePoolInfo.ReleaseReferenceCount 属性
		/// </summary>
		private PropertyInfo m_infoReleaseReferenceCountProperty;

		/// <summary>
		/// ReferencePoolInfo.AddReferenceCount 属性
		/// </summary>
		private PropertyInfo m_infoAddReferenceCountProperty;

		/// <summary>
		/// ReferencePoolInfo.RemoveReferenceCount 属性
		/// </summary>
		private PropertyInfo m_infoRemoveReferenceCountProperty;

		#endregion

		#region 概览与主体绘制

		/// <summary>
		/// 绘制模块级概览与一键移除操作
		/// </summary>
		protected override void DrawOverview()
		{
			var count = m_moduleCountProperty?.GetValue(null) ?? 0;

			// 文案对齐 ReferencePool.ClearAll 的新语义：ClearAll 只清空闲置引用、保留类型条目与累计计数，
			// 故此处显示的是「类型条目数」，一键移除后不会归零（旧文案「引用池总个数」易被误解为移除后清零）。
			EditorGUILayout.LabelField($"引用池类型条目数：{count}（移除闲置后条目与计数保留，不会归零）");

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent("移除所有闲置引用", "仅清空各类型池中的闲置引用：类型条目与累计计数保留，使用中的引用不受影响。"), GUILayout.Width(200)))
			{
				try
				{
					m_moduleRemoveAllPoolsMethod?.Invoke(null, null);
				}
				catch (Exception e)
				{
					Debug.LogError($"移除所有引用池失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制引用池列表主体（基类滚动容器内调用）
		/// </summary>
		protected override void DrawContent()
		{
			var infos = GetAllReferencePoolInfos();
			if (infos.Count == 0)
			{
				EditorGUILayout.HelpBox("引用池为空（无闲置引用且无使用中的引用）", MessageType.Info);
				return;
			}

			foreach (var info in infos)
			{
				DrawReferencePool(info);
			}
		}

		#endregion

		#region 引用池绘制

		/// <summary>
		/// 绘制单个引用池信息
		/// </summary>
		/// <param name="info">引用池信息（ReferencePoolInfo 装箱实例）</param>
		private void DrawReferencePool(object info)
		{
			if (info == null) return;

			var poolType = m_infoTypeProperty?.GetValue(info) as Type;
			if (poolType == null) return;

			var typeName = poolType.Name;
			var fullName = poolType.FullName ?? typeName;

			// 搜索过滤：类型名或全名匹配才展示
			if (!string.IsNullOrEmpty(m_searchFilter))
			{
				if (!typeName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase)
					&& !fullName.Contains(m_searchFilter, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			if (!m_foldoutStates.TryGetValue(poolType, out var isOpen))
			{
				isOpen = true;
				m_foldoutStates[poolType] = true;
			}

			var unusedCount = (int)(m_infoUnusedReferenceCountProperty?.GetValue(info) ?? 0);
			var usingCount  = (int)(m_infoUsingReferenceCountProperty?.GetValue(info)    ?? 0);
			var totalCount  = usingCount + unusedCount;

			// 引用池类型名（Foldout 标题）用青色高亮
			var foldoutOldColor = GUI.color;
			GUI.color             = Color.cyan;
			m_foldoutStates[poolType] = EditorGUILayout.Foldout(isOpen, $"{typeName} ({usingCount}/{totalCount})", true);
			GUI.color             = foldoutOldColor;
			if (!m_foldoutStates[poolType]) return;

			EditorGUILayout.BeginVertical("box");
			{
				DrawPoolStats(info);
				EditorGUILayout.Separator();
				DrawPoolActions(poolType);
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.Separator();
		}

		/// <summary>
		/// 绘制引用池统计信息（单行展示）
		/// </summary>
		/// <param name="info">引用池信息（ReferencePoolInfo 装箱实例）</param>
		private void DrawPoolStats(object info)
		{
			var unusedCount  = (int)(m_infoUnusedReferenceCountProperty?.GetValue(info)   ?? 0);
			var usingCount   = (int)(m_infoUsingReferenceCountProperty?.GetValue(info)    ?? 0);
			var acquireCount = (int)(m_infoAcquireReferenceCountProperty?.GetValue(info)  ?? 0);
			var releaseCount = (int)(m_infoReleaseReferenceCountProperty?.GetValue(info)  ?? 0);
			var addCount     = (int)(m_infoAddReferenceCountProperty?.GetValue(info)      ?? 0);
			var removeCount  = (int)(m_infoRemoveReferenceCountProperty?.GetValue(info)   ?? 0);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label($"闲置: {unusedCount}", GUILayout.MinWidth(80));
			DrawColumnSeparator();
			GUILayout.Label($"使用中: {usingCount}", GUILayout.MinWidth(80));
			DrawColumnSeparator();
			GUILayout.Label($"累计获取: {acquireCount}", GUILayout.MinWidth(100));
			DrawColumnSeparator();
			GUILayout.Label($"累计释放: {releaseCount}", GUILayout.MinWidth(100));
			DrawColumnSeparator();
			GUILayout.Label($"累计新增: {addCount}", GUILayout.MinWidth(100));
			DrawColumnSeparator();
			GUILayout.Label($"累计移除: {removeCount}", GUILayout.MinWidth(100));
			EditorGUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制引用池级操作按钮
		/// </summary>
		/// <param name="poolType">引用池类型</param>
		private void DrawPoolActions(Type poolType)
		{
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("移除该类型闲置引用", GUILayout.Width(160)))
			{
				try
				{
					var removeAllUnusedMethod = m_removeAllUnusedGenericMethod?.MakeGenericMethod(poolType);
					removeAllUnusedMethod?.Invoke(null, null);
				}
				catch (Exception e)
				{
					Debug.LogError($"移除引用池 {poolType.Name} 的闲置引用失败，异常为“{e.InnerException?.Message ?? e.Message}”.");
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region 折叠操作

		/// <summary>
		/// 全部展开
		/// </summary>
		protected override void ExpandAll()
		{
			var infos = GetAllReferencePoolInfos();
			foreach (var info in infos)
			{
				var poolType = m_infoTypeProperty?.GetValue(info) as Type;
				if (poolType != null) m_foldoutStates[poolType] = true;
			}
		}

		/// <summary>
		/// 全部折叠
		/// </summary>
		protected override void CollapseAll()
		{
			var infos = GetAllReferencePoolInfos();
			foreach (var info in infos)
			{
				var poolType = m_infoTypeProperty?.GetValue(info) as Type;
				if (poolType != null) m_foldoutStates[poolType] = false;
			}
		}

		#endregion

		#region 反射

		/// <summary>
		/// 确保反射缓存已初始化
		/// </summary>
		/// <returns>初始化成功返回 true</returns>
		protected override bool EnsureReflection()
		{
			if (m_referencePoolType != null) return true;

			m_referencePoolType = HotfixReflection.ReferencePool;
			if (m_referencePoolType == null) return false;

			m_referencePoolInfoType = HotfixReflection.ReferencePoolInfo;
			if (m_referencePoolInfoType == null) return false;

			// ReferencePool 已剥离为静态基座，成员均为 static，无需再经 ModuleManager 取模块实例

			// ReferencePool 成员（静态）
			m_moduleCountProperty              = m_referencePoolType.GetProperty("Count", BindingFlags.Public | BindingFlags.Static);
			m_getAllReferencePoolInfosMethod   = m_referencePoolType.GetMethod("GetAllReferencePoolInfos", BindingFlags.Public | BindingFlags.Static);
			m_moduleRemoveAllPoolsMethod       = m_referencePoolType.GetMethod("ClearAll", BindingFlags.Public | BindingFlags.Static);
			m_removeAllUnusedGenericMethod     = m_referencePoolType.GetMethod("RemoveAllUnused", BindingFlags.Public | BindingFlags.Static);

			// ReferencePoolInfo 成员
			m_infoTypeProperty                  = m_referencePoolInfoType.GetProperty("Type", BindingFlags.Public | BindingFlags.Instance);
			m_infoUnusedReferenceCountProperty  = m_referencePoolInfoType.GetProperty("UnusedReferenceCount", BindingFlags.Public | BindingFlags.Instance);
			m_infoUsingReferenceCountProperty   = m_referencePoolInfoType.GetProperty("UsingReferenceCount", BindingFlags.Public | BindingFlags.Instance);
			m_infoAcquireReferenceCountProperty = m_referencePoolInfoType.GetProperty("AcquireReferenceCount", BindingFlags.Public | BindingFlags.Instance);
			m_infoReleaseReferenceCountProperty = m_referencePoolInfoType.GetProperty("ReleaseReferenceCount", BindingFlags.Public | BindingFlags.Instance);
			m_infoAddReferenceCountProperty     = m_referencePoolInfoType.GetProperty("AddReferenceCount", BindingFlags.Public | BindingFlags.Instance);
			m_infoRemoveReferenceCountProperty  = m_referencePoolInfoType.GetProperty("RemoveReferenceCount", BindingFlags.Public | BindingFlags.Instance);

			return true;
		}

		/// <summary>
		/// 重置反射缓存（停止运行时调用，避免持有失效的热更实例）
		/// </summary>
		protected override void ResetReflection()
		{
			m_referencePoolType                    = null;
			m_referencePoolInfoType                = null;
			m_moduleCountProperty                  = null;
			m_getAllReferencePoolInfosMethod       = null;
			m_moduleRemoveAllPoolsMethod           = null;
			m_removeAllUnusedGenericMethod         = null;
			m_infoTypeProperty                     = null;
			m_infoUnusedReferenceCountProperty     = null;
			m_infoUsingReferenceCountProperty      = null;
			m_infoAcquireReferenceCountProperty    = null;
			m_infoReleaseReferenceCountProperty    = null;
			m_infoAddReferenceCountProperty        = null;
			m_infoRemoveReferenceCountProperty     = null;
		}

		/// <summary>
		/// 获取所有引用池信息。ReferencePoolInfo 是结构体数组，不能协变为 object[]，以 IEnumerable 枚举逐元素装箱。
		/// </summary>
		/// <returns>按类型全名升序排列的引用池信息列表</returns>
		private List<object> GetAllReferencePoolInfos()
		{
			var list = new List<object>();
			var result = m_getAllReferencePoolInfosMethod?.Invoke(null, null) as IEnumerable;
			if (result == null) return list;

			foreach (var item in result)
			{
				if (item == null) continue;

				// 过滤「闲置为 0 且无在用」的空集合：ClearAll 只清闲置、保留类型条目与计数，
				// 这类条目在面板上既无可展示的引用也无操作价值，保留展示会让列表与「一键移除后不归零」的新语义割裂。
				var unusedCount = (int)(m_infoUnusedReferenceCountProperty?.GetValue(item) ?? 0);
				var usingCount  = (int)(m_infoUsingReferenceCountProperty?.GetValue(item)  ?? 0);
				if (unusedCount == 0 && usingCount == 0) continue;

				list.Add(item);
			}

			// 按类型全名升序排列（引用池无优先级概念）
			list.Sort((a, b) =>
			{
				var typeA = m_infoTypeProperty?.GetValue(a) as Type;
				var typeB = m_infoTypeProperty?.GetValue(b) as Type;
				return string.CompareOrdinal(typeA?.FullName ?? "", typeB?.FullName ?? "");
			});

			return list;
		}

		#endregion
	}
}
#endif
