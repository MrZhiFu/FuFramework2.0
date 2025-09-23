using FuFramework.Core.Editor;
using FuFramework.Localization.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Editor
{
    /// <summary>
    /// 自定义本地化管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(LocalizationManager))]
    internal sealed class LocalizationManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            var localizationManager = target as LocalizationManager;
            if (localizationManager == null) return;

            EditorGUILayout.LabelField("当前语言：", localizationManager.Language.ToString());
        }
    }
}