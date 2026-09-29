using UnityEngine;
using UnityEditor;
using Unity.CodeEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// 编辑器顶部工具栏扩展。
	/// 功能：
	///     1. 目前包括快速切换场景按钮、打开C#工程按钮，后续可扩展更多功能
	/// </summary>
	public static class EditorToolbarExtension
	{
		/// <summary>
		/// 场景资源查找路径
		/// </summary>
		private const string SCENE_ASSET_PATH = "Assets";

		/// <summary>
		/// 快速切换场景按钮内容
		/// </summary>
		private static GUIContent s_switchSceneBtContent;

		/// <summary>
		/// 打开C#工程按钮内容
		/// </summary>
		private static GUIContent s_openCsProjectBtContent;

		/// <summary>
		/// 场景资源列表
		/// </summary>
		private static List<string> s_sceneAssetList;

		/// <summary>
		/// 初始化
		/// </summary>
		[InitializeOnLoadMethod]
		private static void Init()
		{
			s_sceneAssetList = new List<string>();

			var curOpenSceneName = SceneManager.GetActiveScene().name;
			var tarTxt           = string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName;
			s_switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(tarTxt, "切换场景", "UnityLogo");

			s_openCsProjectBtContent = EditorGUIUtility.TrTextContentWithIcon("Open C# Project", "打开C#工程", "dll Script Icon");

			// 场景打开后更新按钮文字为当前场景名称
			EditorSceneManager.sceneOpened += (scene, _) => { s_switchSceneBtContent.text = scene.name; };

			// 注册左右两侧工具栏GUI绘制回调
			UnityEditorToolbar.sr_LeftToolbarCallBackList.Add(OnLeftToolbarGUI);
			UnityEditorToolbar.sr_RightToolbarCallBackList.Add(OnRightToolbarGUI);
		}

		/// <summary>
		/// 左边快速切换场景按钮
		/// </summary>
		private static void OnLeftToolbarGUI()
		{
			GUILayout.FlexibleSpace();
			if (EditorGUILayout.DropdownButton(s_switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
			{
				// 点击后弹出下拉菜单
				var popMenu = new GenericMenu
				{
					allowDuplicateNames = true
				};

				// 查找指定路径下所有的场景资源
				var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SCENE_ASSET_PATH });
				s_sceneAssetList.Clear();
				for (var i = 0; i < sceneGuids.Length; i++)
				{
					var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
					s_sceneAssetList.Add(scenePath);
					var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
					popMenu.AddItem(new GUIContent(sceneName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
				}

				popMenu.ShowAsContext();
			}
		}

		/// <summary>
		/// 右边打开C#工程按钮
		/// </summary>
		private static void OnRightToolbarGUI()
		{
			if (GUILayout.Button(s_openCsProjectBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(120)))
			{
				AssetDatabase.Refresh();
				CodeEditor.Editor.CurrentCodeEditor.SyncAll();
				CodeEditor.Editor.CurrentCodeEditor.OpenProject();
			}

			GUILayout.FlexibleSpace();
		}

		/// <summary>
		/// 切换场景
		/// 1. 保存当前场景
		/// 2. 打开指定场景
		/// </summary>
		/// <param name="menuIdx"></param>
		private static void SwitchScene(int menuIdx)
		{
			if (menuIdx < 0 || menuIdx >= s_sceneAssetList.Count) return;
			var scenePath = s_sceneAssetList[menuIdx];
			var curScene  = SceneManager.GetActiveScene();
			if (curScene is { isDirty: true })
			{
				var opIndex = EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
				switch (opIndex)
				{
					case 0:
						if (!EditorSceneManager.SaveOpenScenes()) return;
						break;
					case 1:
						return;
				}
			}

			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
		}
	}
}