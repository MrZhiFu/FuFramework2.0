//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using GameFrameX.Setting.Runtime;
using UnityEditor;
using UnityEngine;

namespace GameFrameX.Setting.Editor
{
    [CustomEditor(typeof(SettingComponent))]
    internal sealed class SettingComponentInspector : ComponentTypeComponentInspector
    {
        private HelperInfo<SettingHelperBase> m_SettingHelperInfo = new HelperInfo<SettingHelperBase>("Setting");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SettingComponent t = (SettingComponent)target;

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_SettingHelperInfo.Draw();
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Setting Count", t.Count >= 0 ? t.Count.ToString() : "<Unknown>");
                if (t.Count > 0)
                {
                    string[] settingNames = t.GetAllSettingNames();
                    foreach (string settingName in settingNames)
                    {
                        EditorGUILayout.LabelField(settingName, t.GetString(settingName));
                    }
                }
            }

            if (EditorApplication.isPlaying)
            {
                if (GUILayout.Button("Save Settings"))
                {
                    t.Save();
                }

                if (GUILayout.Button("Remove All Settings"))
                {
                    t.RemoveAllSettings();
                }
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