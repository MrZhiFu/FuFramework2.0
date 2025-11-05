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
    [CustomEditor(typeof(DataSaveHelper))]
    internal sealed class SaveHelperInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var saveHelper = target as DataSaveHelper;
            if (!saveHelper) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("当前数据数量", saveHelper.Count >= 0 ? saveHelper.Count.ToString() : "<Unknown>");
            EditorGUILayout.LabelField("是否有未保存的数据", saveHelper.IsDirty? "Yes" : "No");
            
            if (saveHelper.Count > 0)
            {
                var dataNames = saveHelper.GetAllDataNames();
                foreach (var dataName in dataNames)
                {
                    EditorGUILayout.LabelField(dataName, saveHelper.GetString(dataName));
                }
            }

            if (GUILayout.Button("清除数据"))
            {
                saveHelper.RemoveAllData();
            }
        }
    }
}