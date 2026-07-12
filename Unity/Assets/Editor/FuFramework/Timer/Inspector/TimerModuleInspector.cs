using FuFramework.Core.Editor;
using UnityEditor;

// TODO: 后续考虑使用单独的调试界面去显示模块数据
// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Editor
{
    /// <summary>
    /// 计时器管理模块的Inspector
    /// </summary>
    // [CustomEditor(typeof(TimerModule))]
    internal sealed class TimerModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // TODO: 后续考虑使用单独的调试界面去显示这些数据
            /*
            if (target is not TimerModule module) return;
            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("当前计时器数量：", module.Count.ToString());
            EditorGUILayout.Space(10);

            foreach (var timerName in module.GetAllTimerNames())
            {
                EditorGUILayout.LabelField(timerName);
            }
            */
        }
    }
}
