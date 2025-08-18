using FuFramework.Core.Editor;
using FuFramework.GlobalConfig.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace FuFramework.GlobalConfig.Editor
{
    /// <summary>
    /// 自定义全局配置组件的Inspector
    /// </summary>
    [CustomEditor(typeof(GlobalConfigComponent))]
    internal sealed class GlobalConfigComponentInspector : FuFrameworkInspector
    {
        private SerializedProperty m_HostServerUrl;
        private SerializedProperty m_Content;
        private SerializedProperty m_AOTCodeList;
        private SerializedProperty m_AOTCodeLists;
        private SerializedProperty m_CheckAppVersionUrl;
        private SerializedProperty m_CheckResourceVersionUrl;

        private readonly GUIContent m_HostServerUrlGUIContent = new("主机服务地址");
        private readonly GUIContent m_ContentGUIContent = new("附加内容");
        private readonly GUIContent m_ContentGUIAOTCodeList = new("补充程序集列表");
        private readonly GUIContent m_AOTCodeListsContentGUI = new("补充元数据列表");
        private readonly GUIContent m_CheckAppVersionUrlGUIContent = new("检测App版本地址接口");
        private readonly GUIContent m_CheckResourceVersionUrlGUIContent = new("检测资源版本地址接口");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode & Application.isPlaying);
            {
                EditorGUILayout.PropertyField(m_HostServerUrl, m_HostServerUrlGUIContent);
                EditorGUILayout.PropertyField(m_CheckAppVersionUrl, m_CheckAppVersionUrlGUIContent);
                EditorGUILayout.PropertyField(m_CheckResourceVersionUrl, m_CheckResourceVersionUrlGUIContent);
                EditorGUILayout.PropertyField(m_AOTCodeList, m_ContentGUIAOTCodeList, GUILayout.Height(100));
                EditorGUILayout.PropertyField(m_Content, m_ContentGUIContent, GUILayout.Height(120));
                EditorGUILayout.PropertyField(m_AOTCodeLists, m_AOTCodeListsContentGUI);
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            m_CheckAppVersionUrl = serializedObject.FindProperty("m_CheckAppVersionUrl");
            m_HostServerUrl = serializedObject.FindProperty("m_HostServerUrl");
            m_Content = serializedObject.FindProperty("m_Content");
            m_AOTCodeList = serializedObject.FindProperty("m_AOTCodeList");
            m_AOTCodeLists = serializedObject.FindProperty("m_AOTCodeLists");
            m_CheckResourceVersionUrl = serializedObject.FindProperty("m_CheckResourceVersionUrl");

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}