using UnityEditor;
using System.Globalization;
using FuFramework.Core.Editor;
using FuFramework.Web.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Editor
{
    /// <summary>
    /// 自定义Web组件的Inspector
    /// </summary>
    [CustomEditor(typeof(WebManager))]
    internal sealed class WebGameComponentInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            var webManager = target as WebManager;
            if (webManager == null) return;
            
            EditorGUILayout.LabelField("超时时间(秒)：", webManager.Timeout.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("每个服务器的最大连接数：", webManager.MaxConnectionPerServer.ToString());
        }
    }
}