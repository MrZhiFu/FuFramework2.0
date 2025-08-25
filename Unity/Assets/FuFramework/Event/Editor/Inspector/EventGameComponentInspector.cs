using FuFramework.Core.Editor;
using FuFramework.Event.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Editor
{
    /// <summary>
    /// 自定义事件组件的Inspector
    /// </summary>
    [CustomEditor(typeof(EventComponent))]
    internal sealed class EventGameComponentInspector : GameComponentInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available during runtime only.", MessageType.Info);
                return;
            }

            var eventComp = target as EventComponent;
            if (!eventComp) return;

            if (IsPrefabInHierarchy(eventComp.gameObject))
            {
                EditorGUILayout.LabelField("Event Handler Count", eventComp.EventHandlerCount.ToString());
                EditorGUILayout.LabelField("Event Count", eventComp.EventCount.ToString());
            }

            Repaint();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IEventManager));
        }
    }
}