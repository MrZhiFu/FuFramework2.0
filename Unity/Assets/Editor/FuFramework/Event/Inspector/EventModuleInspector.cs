using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Editor
{
    /// <summary>
    /// 自定义事件管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(EventModule))]
    internal sealed class EventModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            if (target is not EventModule module) return;

            EditorGUILayout.LabelField("已注册的事件处理函数的数量：", module.EventHandlerCount.ToString());
            EditorGUILayout.LabelField("触发的事件数量：",       module.EventCount.ToString());

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("已注册的事件处理函数列表：");
            module.ForEachHandler((eventId, handler) => { EditorGUILayout.LabelField($"事件：{eventId}, 处理函数：{handler.Target.GetHashCode()}-{handler.Target.GetType().Name}.{handler.Method.Name}"); });

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("当前帧触发的事件列表：");
            module.ForEachEvent((sender, eventArgs) => { EditorGUILayout.LabelField($"发送者：{sender}，事件：{eventArgs.Id}"); });
            */
        }
    }
}
