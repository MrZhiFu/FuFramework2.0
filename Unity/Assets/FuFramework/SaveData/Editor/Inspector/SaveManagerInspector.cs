using UnityEditor;
using UnityEngine;
using FuFramework.Core.Editor;
using FuFramework.SaveData.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.SaveData.Editor
{
    /// <summary>
    /// 自定义本地数据存储管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(SaveManager))]
    internal sealed class SaveManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var saveManager = target as SaveManager;
            if (!saveManager) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("当前所有数据文件数量", saveManager.Count >= 0 ? saveManager.Count.ToString() : "<Unknown>");
            if (saveManager.Count > 0)
            {
                var helperNames = saveManager.GetAllHelperNames();
                foreach (var helperName in helperNames)
                {
                    var helper = saveManager.GetHelper(helperName);
                    if (!helper) continue;
                   EditorGUILayout.LabelField(helperName, helper.Count.ToString());
                }
            }

            if (GUILayout.Button("清除所有数据"))
            {
                saveManager.RemoveAllData();
            }
        }
    }
}