#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// Play 模式调试窗口抽象基类。
	/// 收编各模块调试面板的公共骨架：自动刷新节拍（订阅 EditorApplication.update）、
	/// 非 Play 守卫（重置反射缓存 + 提示）、反射初始化失败提示、
	/// 通用工具栏（搜索 / 自动刷新 / 刷新 / 全部展开 / 全部折叠）、滚动容器与列分隔线。
	/// 子类职责：实现 DrawContent 绘制主体，EnsureReflection / ResetReflection 管理各自的反射缓存
	/// （类型解析统一经 <see cref="HotfixReflection"/>，勿写字面量程序集限定名）。
	/// </summary>
	public abstract class DebugWindowBase : EditorWindow
	{
		#region 基类字段

		/// <summary>
		/// 滚动位置
		/// </summary>
		protected Vector2 m_scrollPos;

		/// <summary>
		/// 搜索过滤字符串（由子类消费）
		/// </summary>
		protected string m_searchFilter = "";

		/// <summary>
		/// 是否自动刷新（每 RefreshInterval 秒重绘一次）
		/// </summary>
		protected bool m_autoRefresh = true;

		/// <summary>
		/// 上次自动刷新时间
		/// </summary>
		private double m_lastRefreshTime;

		/// <summary>
		/// 上一帧的 Play 状态（用于 Play→非 Play 跳变时仅执行一次 ResetReflection）
		/// </summary>
		private bool m_wasPlaying;

		#endregion

		#region 子类可配置

		/// <summary>
		/// 工具栏搜索框宽度（子类可覆写以适配长过滤串场景）
		/// </summary>
		protected virtual int SearchFieldWidth => 150;

		/// <summary>
		/// 是否显示「全部展开 / 全部折叠」按钮（需子类实现 ExpandAll / CollapseAll）
		/// </summary>
		protected virtual bool HasExpandCollapse => true;

		/// <summary>
		/// 自动刷新间隔（秒）
		/// </summary>
		protected virtual float RefreshInterval => 0.5f;

		/// <summary>
		/// 模块显示名（反射初始化失败的提示中展示，如 "EventModule"）
		/// </summary>
		protected abstract string ModuleDisplayName { get; }

		#endregion

		#region 子类钩子

		/// <summary>
		/// 确保反射缓存已初始化；成功返回 true（失败时基类显示提示并不绘制主体）。
		/// </summary>
		protected abstract bool EnsureReflection();

		/// <summary>
		/// 重置反射缓存（非 Play 模式下调用，避免持有已失效的热更实例）
		/// </summary>
		protected abstract void ResetReflection();

		/// <summary>
		/// 绘制主体内容（已处于 Play 模式且反射就绪；在基类滚动容器内调用）
		/// </summary>
		protected abstract void DrawContent();

		/// <summary>
		/// 绘制滚动容器之前的概览区（可选）
		/// </summary>
		protected virtual void DrawOverview() { }

		/// <summary>
		/// 刷新动作（自动刷新到点与手动点击「刷新」时触发，可选；
		/// 如 Event 窗口在此重建快照）
		/// </summary>
		protected virtual void OnRefresh() { }

		/// <summary>
		/// 全部展开（HasExpandCollapse 为 true 时由工具栏触发）
		/// </summary>
		protected virtual void ExpandAll() { }

		/// <summary>
		/// 全部折叠（HasExpandCollapse 为 true 时由工具栏触发）
		/// </summary>
		protected virtual void CollapseAll() { }

		/// <summary>
		/// 工具栏自定义按钮扩展位（在「刷新」按钮前、FlexibleSpace 之后绘制，可选）
		/// </summary>
		protected virtual void DrawToolbarExtraButtons() { }

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
		/// 编辑器帧更新：自动刷新到点时促发重绘（实际的数据重建在可见的 OnGUI 内执行，
		/// 窗口被遮挡/最小化时不产生重建开销）
		/// </summary>
		private void OnEditorUpdate()
		{
			if (!m_autoRefresh || !Application.isPlaying) return;
			if (EditorApplication.timeSinceStartup - m_lastRefreshTime < RefreshInterval) return;

			Repaint();
		}

		/// <summary>
		/// 绘制 GUI：工具栏 → 非 Play 守卫 → 反射守卫 → 自动刷新节拍 → 概览 → 滚动主体
		/// </summary>
		private void OnGUI()
		{
			DrawToolbar();

			// 非 Play 模式：仅在 Play→非 Play 跳变的那一次重置反射缓存（避免每个 GUI 事件重复清理
			// 字典与字段——如 ConfigModuleWindow 的字段编辑撤销缓存），防止持有已失效的热更实例
			if (!Application.isPlaying)
			{
				if (m_wasPlaying)
				{
					m_wasPlaying = false;
					ResetReflection();
				}

				EditorGUILayout.HelpBox("需要在 Play 模式下使用", MessageType.Info);
				return;
			}

			m_wasPlaying = true;

			if (!EnsureReflection())
			{
				EditorGUILayout.HelpBox($"未能通过反射访问 {ModuleDisplayName}，请确认 Hotfix 已加载", MessageType.Warning);
				return;
			}

			// 自动刷新到点：重建钩子在可见的 OnGUI 内按节拍执行
			if (m_autoRefresh && EditorApplication.timeSinceStartup - m_lastRefreshTime >= RefreshInterval)
			{
				InvokeRefresh();
			}

			DrawOverview();
			EditorGUILayout.Separator();

			m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
			DrawContent();
			EditorGUILayout.EndScrollView();
		}

		/// <summary>
		/// 执行一次刷新动作并记录节拍时间（工具栏「刷新」按钮与自动刷新到点的统一入口）
		/// </summary>
		private void InvokeRefresh()
		{
			m_lastRefreshTime = EditorApplication.timeSinceStartup;
			OnRefresh();
		}

		#endregion

		#region 工具栏

		/// <summary>
		/// 绘制顶部工具栏：搜索 / 自动刷新 / 刷新 / 全部展开 / 全部折叠
		/// </summary>
		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			GUILayout.Label("搜索:", GUILayout.Width(40));
			m_searchFilter = GUILayout.TextField(m_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(SearchFieldWidth));

			GUILayout.Space(20);
			m_autoRefresh = GUILayout.Toggle(m_autoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(80));

			GUILayout.FlexibleSpace();

			// 子类扩展位：在「刷新」按钮前绘制自定义工具栏按钮
			DrawToolbarExtraButtons();

			if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
			{
				InvokeRefresh();
				Repaint();
			}

			if (HasExpandCollapse)
			{
				if (GUILayout.Button("全部展开", EditorStyles.toolbarButton, GUILayout.Width(80)))
				{
					ExpandAll();
				}

				if (GUILayout.Button("全部折叠", EditorStyles.toolbarButton, GUILayout.Width(80)))
				{
					CollapseAll();
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region 共用设施

		/// <summary>
		/// 绘制列与列之间的分隔竖线
		/// </summary>
		protected static void DrawColumnSeparator()
		{
			GUILayout.Label("|", GUILayout.Width(12));
		}

		/// <summary>
		/// 将折叠状态字典中指定键的值批量置为 open（ExpandAll / CollapseAll 的通用实现，
		/// 适用于键为元素本身的折叠字典）。
		/// </summary>
		/// <param name="keys">折叠项的键集合。</param>
		/// <param name="states">折叠状态字典。</param>
		/// <param name="open">置为展开（true）或折叠（false）。</param>
		protected static void SetAllFoldouts<TKey>(IEnumerable<TKey> keys, Dictionary<TKey, bool> states, bool open)
		{
			foreach (var key in keys)
			{
				states[key] = open;
			}
		}

		#endregion
	}
}
#endif