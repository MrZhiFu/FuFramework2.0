//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using GameFrameX.Localization.Runtime;
using UnityEditor;

namespace GameFrameX.Localization.Editor
{
    [CustomEditor(typeof(LocalizationComponent))]
    internal sealed class LocalizationComponentInspector : ComponentTypeComponentInspector
    {
        // private SerializedProperty m_EnableLoadDictionaryUpdateEvent = null;
        private SerializedProperty m_EditorLanguage = null;
        private SerializedProperty m_DefaultLanguage = null;
        private SerializedProperty m_IsEnableEditorMode = null;

        private HelperInfo<LocalizationHelperBase> m_LocalizationHelperInfo = new HelperInfo<LocalizationHelperBase>("Localization");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            LocalizationComponent localizationComponent = (LocalizationComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_LocalizationHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_DefaultLanguage);
                EditorGUILayout.PropertyField(m_IsEnableEditorMode);
                EditorGUILayout.PropertyField(m_EditorLanguage);
                EditorGUILayout.HelpBox("Editor language option is only use for localization test in editor mode.", MessageType.Info);
                // EditorGUILayout.PropertyField(m_EnableLoadDictionaryUpdateEvent);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(localizationComponent.gameObject))
            {
                EditorGUILayout.LabelField("Language", localizationComponent.Language.ToString());
                EditorGUILayout.LabelField("System Language", localizationComponent.SystemLanguage.ToString());
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        protected override void Enable()
        {
            // m_EnableLoadDictionaryUpdateEvent = serializedObject.FindProperty("m_EnableLoadDictionaryUpdateEvent");
            m_EditorLanguage = serializedObject.FindProperty("m_EditorLanguage");
            m_DefaultLanguage = serializedObject.FindProperty("m_DefaultLanguage");
            m_IsEnableEditorMode = serializedObject.FindProperty("m_IsEnableEditorMode");
            m_LocalizationHelperInfo.Init(serializedObject);
            m_LocalizationHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(ILocalizationManager));
        }
    }
}