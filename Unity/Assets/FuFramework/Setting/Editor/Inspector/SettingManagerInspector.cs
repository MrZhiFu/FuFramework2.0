using UnityEditor;
using UnityEngine;
using FuFramework.Core.Editor;
using FuFramework.Setting.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Setting.Editor
{
    /// <summary>
    /// 自定义Setting管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(SettingManager))]
    internal sealed class SettingManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var settingManager = target as SettingManager;
            if (settingManager == null) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("当前设置数量", settingManager.Count >= 0 ? settingManager.Count.ToString() : "<Unknown>");
            if (settingManager.Count > 0)
            {
                var settingNames = settingManager.GetAllSettingNames();
                foreach (var settingName in settingNames)
                {
                    EditorGUILayout.LabelField(settingName, settingManager.GetString(settingName));
                }
            }

            if (GUILayout.Button("保存数据"))
                settingManager.Save();

            if (GUILayout.Button("清除所有数据"))
                settingManager.RemoveAllSettings();
        }
    }
}