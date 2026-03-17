using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Config.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Editor
{
    /// <summary>
    /// 自定义配置表Inspector
    /// </summary>
    [CustomEditor(typeof(ConfigModule))]
    internal sealed class ConfigModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (target is not ConfigModule module) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("配置表个数：", module.Count.ToString());
                foreach (var configName in module.ConfigNames)
                {
                    EditorGUILayout.LabelField(configName);
                }
            }

            Repaint();
        }
    }
}