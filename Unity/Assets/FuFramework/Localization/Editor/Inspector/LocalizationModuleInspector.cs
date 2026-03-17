using FuFramework.Core.Editor;
using FuFramework.Localization.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Editor
{
    /// <summary>
    /// 自定义本地化管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(LocalizationModule))]
    internal sealed class LocalizationModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            if (target is not LocalizationModule module) return;
            EditorGUILayout.LabelField("当前语言：", module.Language.ToString());
        }
    }
}