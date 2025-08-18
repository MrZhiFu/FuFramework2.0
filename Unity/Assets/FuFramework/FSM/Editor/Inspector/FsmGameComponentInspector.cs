using FuFramework.Core.Editor;
using FuFramework.Fsm.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Editor
{
    /// <summary>
    /// 自定义游戏状态机组件的Inspector
    /// </summary>
    [CustomEditor(typeof(FsmComponent))]
    internal sealed class FsmGameComponentInspector : GameComponentInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available during runtime only.", MessageType.Info);
                return;
            }

            var t = (FsmComponent)target;

            if (IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("FSM Count", t.Count.ToString());

                var fsms = t.GetAllFsmList();
                foreach (var fsm in fsms)
                {
                    DrawFsm(fsm);
                }
            }

            Repaint();
        }

        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IFsmManager));
        }

        private void DrawFsm(FsmBase fsm)
        {
            string label;
            if (fsm.IsRunning)
                label = $"{fsm.CurrentStateName}, {fsm.CurrentStateTime:F1} s";
            else if (fsm.IsDestroyed)
                label = "Destroyed";
            else
                label = "Not Running";
            EditorGUILayout.LabelField(fsm.FullName, label);
        }
    }
}