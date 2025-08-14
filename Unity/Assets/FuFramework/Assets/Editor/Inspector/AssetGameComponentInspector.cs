using FuFramework.Asset.Runtime;
using FuFramework.Core.Editor;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Asset.Editor
{
    /// <summary>
    /// 自定义资源管理器组件的Inspector
    /// </summary>
    [CustomEditor(typeof(AssetComponent))]
    internal sealed class AssetGameComponentInspector : GameComponentInspector
    {
        private SerializedProperty m_GamePlayMode;

        private readonly GUIContent m_GamePlayModeGUIContent = new("资源运行模式");
        private readonly GUIContent m_AssetResourcePackagesGUIContent = new("包列表");
        private SerializedProperty m_AssetResourcePackages;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_GamePlayMode, m_GamePlayModeGUIContent);
                GUI.enabled = false;
                EditorGUILayout.PropertyField(m_AssetResourcePackages, m_AssetResourcePackagesGUIContent);
                GUI.enabled = true;
            }
            EditorGUI.EndDisabledGroup();
            
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IAssetManager));
        }

        protected override void Enable()
        {
            m_GamePlayMode = serializedObject.FindProperty("m_GamePlayMode");
            m_AssetResourcePackages = serializedObject.FindProperty("m_assetResourcePackages");
            // m_fallbackHostServer = serializedObject.FindProperty("m_fallbackHostServer");
        }
    }
}