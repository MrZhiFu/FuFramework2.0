using UnityEditor;
using UnityEngine;
using FuFramework.Core.Editor;
using FuFramework.SaveData.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.SaveData.Editor
{
    /// <summary>
    /// 自定义本地数据存储管理模块的Inspector
    /// </summary>
    [CustomEditor(typeof(DataSaveModule))]
    internal sealed class SaveModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is not DataSaveModule module) return;
            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("当前所有数据文件数量", module.Count >= 0 ? module.Count.ToString() : "<Unknown>");
            if (module.Count > 0)
            {
                var helperNames = module.GetAllHelperNames();
                foreach (var helperName in helperNames)
                {
                    var helper = module.GetHelper(helperName);
                    if (!helper) continue;
                    EditorGUILayout.LabelField(helperName, helper.Count.ToString());
                }
            }

            if (GUILayout.Button("清除所有数据"))
            {
                module.RemoveAllData();
            }
        }
    }
}