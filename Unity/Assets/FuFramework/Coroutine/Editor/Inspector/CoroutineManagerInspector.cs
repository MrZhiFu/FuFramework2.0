using FuFramework.Core.Editor;
using FuFramework.Coroutine.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace GameFrameX.Coroutine.Editor
{
    /// <summary>
    /// 自定义协程组件的Inspector
    /// </summary>
    [CustomEditor(typeof(CoroutineManager))]
    internal sealed class CoroutineManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (target is not CoroutineManager t) return;
            
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("当前执行的协程个数：", t.Count.ToString());
                foreach (var coroutine in t.AllCoroutines)
                {
                    EditorGUILayout.LabelField(coroutine.ToString());
                }
            }
            Repaint();
        }
    }
}