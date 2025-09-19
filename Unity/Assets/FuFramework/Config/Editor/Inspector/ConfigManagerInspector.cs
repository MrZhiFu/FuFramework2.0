using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Config.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
    /// <summary>
    /// 自定义配置表Inspector
    /// </summary>
    [CustomEditor(typeof(ConfigManager))]
    internal sealed class ConfigManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (target is not ConfigManager t) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("配置表个数：", t.Count.ToString());
                foreach (var configName in t.ConfigNames)
                {
                    EditorGUILayout.LabelField(configName);
                }
            }

            Repaint();
        }
    }
}