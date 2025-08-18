using FuFramework.Core.Editor;
using FuFramework.Localization.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Editor
{
    /// <summary>
    /// 自定义本地化组件的Inspector
    /// </summary>
    [CustomEditor(typeof(LocalizationComponent))]
    internal sealed class LocalizationGameComponentInspector : GameComponentInspector
    {
        // private SerializedProperty m_EnableLoadDictionaryUpdateEvent = null;
        private SerializedProperty m_EditorLanguage;
        private SerializedProperty m_DefaultLanguage;
        private SerializedProperty m_IsEnableEditorMode;

        private readonly HelperInfo<LocalizationHelperBase> m_LocalizationHelperInfo = new("Localization");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var localizationComp = (LocalizationComponent)target;

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

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(localizationComp.gameObject))
            {
                EditorGUILayout.LabelField("Language", localizationComp.Language.ToString());
                EditorGUILayout.LabelField("System Language", localizationComp.SystemLanguage.ToString());
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