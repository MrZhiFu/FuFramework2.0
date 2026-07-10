using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Web.Editor
{
    /// <summary>
    /// Web模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(WebModule))]
    internal sealed class WebModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            serializedObject.Update();

            if (target is not WebModule module) return;

            EditorGUILayout.LabelField("超时时间(秒)：", module.Timeout.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("每个服务器的最大连接数：", module.MaxConnectionPerServer.ToString());
            */
        }
    }
}
