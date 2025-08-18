using FuFramework.Core.Editor;
using FuFramework.Setting.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Setting.Editor
{
    /// <summary>
    /// 自定义Setting组件的Inspector
    /// </summary>
    [CustomEditor(typeof(SettingComponent))]
    internal sealed class SettingGameComponentInspector : GameComponentInspector
    {
        private readonly HelperInfo<SettingHelperBase> m_SettingHelperInfo = new("Setting");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var settingComp = (SettingComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_SettingHelperInfo.Draw();
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(settingComp.gameObject))
            {
                EditorGUILayout.LabelField("Setting Count", settingComp.Count >= 0 ? settingComp.Count.ToString() : "<Unknown>");
                if (settingComp.Count > 0)
                {
                    var settingNames = settingComp.GetAllSettingNames();
                    foreach (var settingName in settingNames)
                    {
                        EditorGUILayout.LabelField(settingName, settingComp.GetString(settingName));
                    }
                }
            }

            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button("Save Settings"))
                    settingComp.Save();

                if (GUILayout.Button("Remove All Settings"))
                    settingComp.RemoveAllSettings();
            }

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            _RefreshTypeNames();
        }

        protected override void Enable()
        {
            m_SettingHelperInfo.Init(serializedObject);
            _RefreshTypeNames();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(ISettingManager));
        }

        private void _RefreshTypeNames()
        {
            m_SettingHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}