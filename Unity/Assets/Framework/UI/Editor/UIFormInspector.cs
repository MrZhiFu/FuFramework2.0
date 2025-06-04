//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using GameFrameX.UI.Runtime;
using UnityEditor;

namespace GameFrameX.UI.Editor
{
    /// <summary>
    /// UI 表单检查器。
    /// </summary>
    [CustomEditor(typeof(UIForm), true)]
    internal sealed class UIFormInspector : GameFrameworkInspector
    {
        private SerializedProperty m_Available = null;
        private SerializedProperty m_Visible = null;
        private SerializedProperty m_IsInit = null;
        private SerializedProperty m_SerialId = null;
        private SerializedProperty m_OriginalLayer = null;
        private SerializedProperty m_UIFormAssetName = null;
        private SerializedProperty m_AssetPath = null;
        private SerializedProperty m_DepthInUIGroup = null;
        private SerializedProperty m_PauseCoveredUIForm = null;
        private SerializedProperty m_FullName = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_FullName);
                EditorGUILayout.PropertyField(m_SerialId);
                EditorGUILayout.PropertyField(m_IsInit);
                EditorGUILayout.PropertyField(m_OriginalLayer);
                EditorGUILayout.PropertyField(m_UIFormAssetName);
                EditorGUILayout.PropertyField(m_Available);
                EditorGUILayout.PropertyField(m_Visible);
                EditorGUILayout.PropertyField(m_AssetPath);
                EditorGUILayout.PropertyField(m_DepthInUIGroup);
                EditorGUILayout.PropertyField(m_PauseCoveredUIForm);
            }
            EditorGUI.EndDisabledGroup();

            Repaint();
        }


        private void OnEnable()
        {
            m_Available = serializedObject.FindProperty("m_Available");
            m_Visible = serializedObject.FindProperty("m_Visible");
            m_IsInit = serializedObject.FindProperty("m_IsInit");
            m_SerialId = serializedObject.FindProperty("m_SerialId");
            m_OriginalLayer = serializedObject.FindProperty("m_OriginalLayer");
            m_UIFormAssetName = serializedObject.FindProperty("m_UIFormAssetName");
            m_AssetPath = serializedObject.FindProperty("m_AssetPath");
            m_DepthInUIGroup = serializedObject.FindProperty("m_DepthInUIGroup");
            m_PauseCoveredUIForm = serializedObject.FindProperty("m_PauseCoveredUIForm");
            m_FullName = serializedObject.FindProperty("m_FullName");
        }
    }
}