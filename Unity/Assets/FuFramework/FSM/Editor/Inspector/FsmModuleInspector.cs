using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Fsm.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Fsm.Editor
{
    /// <summary>
    /// 自定义游戏状态机组件的Inspector
    /// </summary>
    [CustomEditor(typeof(FsmModule))]
    internal sealed class FsmModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is not FsmModule module) return;

            EditorGUILayout.LabelField("有限状态机数量：", module.Count.ToString());

            EditorGUILayout.Space(10);
            
            var fsms = module.GetAllFsms();
            foreach (var fsm in fsms)
            {
                DrawFsm(fsm);
            }
        }

        /// <summary>
        /// 绘制一个有限状态机
        /// </summary>
        /// <param name="fsm"></param>
        private static void DrawFsm(Runtime.Fsm fsm)
        {
            string label;
            if (fsm.IsRunning)
                label = $"当前状态：{fsm.CurrentStateName}，运行中：{fsm.CurrentStateTime:F1} s";
            else if (fsm.IsDestroyed)
                label = "被销毁";
            else
                label = "未运行";

            EditorGUILayout.LabelField($"名称：{fsm.FullName}", label);
        }
    }
}