using UnityEditor;
using FuFramework.Core.Editor;
using FuFramework.Timer.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Timer.Editor
{
    /// <summary>
    /// 计时器管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(TimerManager))]
    internal sealed class TimerManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var timerManager = target as TimerManager;
            if (timerManager == null) return;

            if (!EditorApplication.isPlaying) return;

            EditorGUILayout.LabelField("当前计时器数量：", timerManager.Count.ToString());
            EditorGUILayout.Space(10);
            foreach (var timerName in timerManager.GetAllTimerNames())
            {
                EditorGUILayout.LabelField(timerName);
            }
        }
    }
}
