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
    /// 目前包括快速切换场景按钮、打开C#工程按钮，后续可扩展更多功能
    /// </summary>
    public static class EditorToolbarExtension
    {
        private const string SceneAssetPath = "Assets"; // 场景资源查找路径

        private static GUIContent m_SwitchSceneBtContent; // 切换场景按钮
        private static GUIContent m_OpenCsProjectBtContent; // 打开C#工程按钮

        private static List<string> m_SceneAssetList; // 场景资源列表

        /// <summary>
        /// 初始化
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Init()
        {
            m_SceneAssetList = new List<string>();

            var curOpenSceneName = SceneManager.GetActiveScene().name;
            var tarTxt = string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName;
            m_SwitchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(tarTxt, "切换场景", "UnityLogo");

            m_OpenCsProjectBtContent = EditorGUIUtility.TrTextContentWithIcon("Open C# Project", "打开C#工程", "dll Script Icon");

            // 场景打开后更新按钮文字为当前场景名称
            EditorSceneManager.sceneOpened += (scene, _) => { m_SwitchSceneBtContent.text = scene.name; };

            // 注册左右两侧工具栏GUI绘制回调
            UnityEditorToolbar.LeftToolbarGUI.Add(OnLeftToolbarGUI);
            UnityEditorToolbar.RightToolbarGUI.Add(OnRightToolbarGUI);
        }

        /// <summary>
        /// 左边快速切换场景按钮
        /// </summary>
        private static void OnLeftToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (EditorGUILayout.DropdownButton(m_SwitchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150)))
            {
                // 点击后弹出下拉菜单
                var popMenu = new GenericMenu
                {
                    allowDuplicateNames = true
                };

                // 查找指定路径下所有的场景资源
                var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneAssetPath });
                m_SceneAssetList.Clear();
                for (var i = 0; i < sceneGuids.Length; i++)
                {
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    m_SceneAssetList.Add(scenePath);
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
            if (GUILayout.Button(m_OpenCsProjectBtContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(120)))
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
            if (menuIdx < 0 || menuIdx >= m_SceneAssetList.Count) return;
            var scenePath = m_SceneAssetList[menuIdx];
            var curScene = SceneManager.GetActiveScene();
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