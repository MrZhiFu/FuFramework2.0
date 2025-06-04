using GameFrameX.Editor;
using GameFrameX.GlobalConfig.Runtime;
using UnityEditor;
using UnityEngine;

namespace GameFrameX.GlobalConfig.Editor
{
    [CustomEditor(typeof(GlobalConfigComponent))]
    internal sealed class GlobalConfigComponentInspector : GameFrameworkInspector
    {
        private SerializedProperty m_HostServerUrl = null;
        private SerializedProperty m_Content = null;
        private SerializedProperty m_AOTCodeList = null;
        private SerializedProperty m_AOTCodeLists = null;
        private SerializedProperty m_CheckAppVersionUrl = null;
        private SerializedProperty m_CheckResourceVersionUrl = null;
        private GUIContent m_HostServerUrlGUIContent = new GUIContent("主机服务地址");
        private GUIContent m_ContentGUIContent = new GUIContent("附加内容");
        private GUIContent m_ContentGUIAOTCodeList = new GUIContent("补充程序集列表");
        private GUIContent m_AOTCodeListsContentGUI = new GUIContent("补充元数据列表");
        private GUIContent m_CheckAppVersionUrlGUIContent = new GUIContent("检测App版本地址接口");
        private GUIContent m_CheckResourceVersionUrlGUIContent = new GUIContent("检测资源版本地址接口");

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