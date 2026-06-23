using UnityEditor;
using System.Globalization;
using FuFramework.Core.Editor;
using FuFramework.Web.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Editor
{
    /// <summary>
    /// Web模块的Inspector
    /// </summary>
    [CustomEditor(typeof(WebModule))]
    internal sealed class WebModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (target is not WebModule module) return;
            
            EditorGUILayout.LabelField("超时时间(秒)：", module.Timeout.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("每个服务器的最大连接数：", module.MaxConnectionPerServer.ToString());
        }
    }
}