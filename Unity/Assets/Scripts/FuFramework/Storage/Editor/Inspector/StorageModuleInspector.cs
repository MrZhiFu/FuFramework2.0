using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.SaveData.Editor
{
    /// <summary>
    /// 自定义本地数据存储管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(StorageModule))]
    internal sealed class StorageModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            if (target is not StorageModule module) return;
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
            */
        }
    }
}
