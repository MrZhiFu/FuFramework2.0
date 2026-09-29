using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// 工具栏绘制相关回调。
	/// 功能：
	///     1. 通过反射获取工具栏实例并注册回调。
	/// </summary>
	public static class ToolbarCallback
	{
		/// <summary>
		/// 工具栏类型
		/// </summary>
		private static readonly System.Type sr_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");

		/// <summary>
		/// 当前工具栏实例
		/// </summary>
		private static ScriptableObject s_currentToolbar;

		/// <summary>
		/// 工具栏 OnGUILeft 方法的回调。
		/// </summary>
		public static Action s_OnToolbarGUILeft;

		/// <summary>
		/// 工具栏 OnGUIRight 方法的回调。
		/// </summary>
		public static Action s_OnToolbarGUIRight;

		static ToolbarCallback()
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}

		private static void OnUpdate()
		{
			// 依赖于工具栏是 ScriptableObject 并在布局更改时被删除的事实
			if (s_currentToolbar != null) return;

			// 查找工具栏实例
			var toolbars = Resources.FindObjectsOfTypeAll(sr_toolbarType);
			s_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
			if (s_currentToolbar == null) return;

			// 获取工具栏根节点
			var root = s_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
			if (root == null) return;
			var rawRoot = root.GetValue(s_currentToolbar);
			var mRoot   = rawRoot as VisualElement;

			// 注册绘制回调
			RegisterCallback("ToolbarZoneLeftAlign",  s_OnToolbarGUILeft);
			RegisterCallback("ToolbarZoneRightAlign", s_OnToolbarGUIRight);
			return;

			// 注册绘制回调
			void RegisterCallback(string rootTemp, Action cb)
			{
				// 获取工具栏区域节点
				var toolbarZone = mRoot.Q(rootTemp);

				// 创建父节点并设置样式
				var parent = new VisualElement
				{
					style =
					{
						flexGrow      = 1,
						flexDirection = FlexDirection.Row,
					}
				};

				// 创建 IMGUIContainer 并注册绘制回调
				var container = new IMGUIContainer();
				container.style.flexGrow =  1;
				container.onGUIHandler   += () => { cb?.Invoke(); };
				parent.Add(container);
				toolbarZone.Add(parent);
			}
		}
	}

	/// <summary>
	/// 编辑器工具栏
	/// 左右两侧是相对于Play按钮的位置
	/// </summary>
	[InitializeOnLoad]
	public static class UnityEditorToolbar
	{
		/// <summary> 左侧工具栏设置绘制内容回调 </summary>
		public static readonly List<Action> sr_LeftToolbarCallBackList = new();

		/// <summary> 右侧工具栏设置绘制内容回调 </summary>
		public static readonly List<Action> sr_RightToolbarCallBackList = new();

		static UnityEditorToolbar()
		{
			// 注册工具栏左右两侧绘制回调
			ToolbarCallback.s_OnToolbarGUILeft  = GUILeft;
			ToolbarCallback.s_OnToolbarGUIRight = GUIRight;
		}

		/// <summary>
		/// 绘制左侧工具栏
		/// </summary>
		private static void GUILeft()
		{
			GUILayout.BeginHorizontal();
			foreach (var callback in sr_LeftToolbarCallBackList)
			{
				callback();
			}

			GUILayout.EndHorizontal();
		}

		/// <summary>
		/// 绘制右侧工具栏
		/// </summary>
		private static void GUIRight()
		{
			GUILayout.BeginHorizontal();
			foreach (var callback in sr_RightToolbarCallBackList)
			{
				callback();
			}

			GUILayout.EndHorizontal();
		}
	}
}