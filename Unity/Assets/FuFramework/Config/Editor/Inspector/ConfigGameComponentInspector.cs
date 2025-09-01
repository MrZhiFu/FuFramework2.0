﻿using FuFramework.Core.Editor;
using FuFramework.Config.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
    /*
    /// <summary>
    /// 自定义配置表Inspector
    /// </summary>
    [CustomEditor(typeof(ConfigComponent))]
    internal sealed class ConfigGameComponentInspector : GameComponentInspector
    {

        private SerializedProperty m_EnableLoadConfigUpdateEvent = null;
        private SerializedProperty m_CachedBytesSize = null;

        // private HelperInfo<ConfigHelperBase> m_ConfigHelperInfo = new HelperInfo<ConfigHelperBase>("Config");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ConfigComponent t = (ConfigComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_EnableLoadConfigUpdateEvent);
                // m_ConfigHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_CachedBytesSize);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Config Count", t.Count.ToString());
                // EditorGUILayout.LabelField("Cached Bytes Size", t.CachedBytesSize.ToString());
            }

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
            m_EnableLoadConfigUpdateEvent = serializedObject.FindProperty("m_EnableLoadConfigUpdateEvent");
            m_CachedBytesSize = serializedObject.FindProperty("m_CachedBytesSize");

            // m_ConfigHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            // m_ConfigHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IConfigManager));
        }
    }
    */
}