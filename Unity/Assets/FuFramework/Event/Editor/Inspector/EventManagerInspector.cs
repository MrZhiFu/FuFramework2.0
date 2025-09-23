using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Event.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Editor
{
    /// <summary>
    /// 自定义事件管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(EventManager))]
    internal sealed class EventManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var eventManager = target as EventManager;
            if (!eventManager) return;

            EditorGUILayout.LabelField("已注册的事件处理函数的数量：", eventManager.EventHandlerCount.ToString());
            EditorGUILayout.LabelField("触发的事件数量：", eventManager.EventCount.ToString());
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("已注册的事件处理函数列表：");
            eventManager.ForEachHandler((eventId, handler) =>
            {
                EditorGUILayout.LabelField($"事件：{eventId}, 处理函数：{handler.Target.GetHashCode()}-{handler.Target.GetType().Name}.{handler.Method.Name}");
            });
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("当前帧触发的事件列表：");
            eventManager.ForEachEvent((sender, eventArgs) =>
            {
                EditorGUILayout.LabelField($"发送者：{sender}，事件：{eventArgs.Id}");
            });
        }
    }
}