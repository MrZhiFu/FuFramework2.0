using FuFramework.Core.Editor;
using FuFramework.Coroutine.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace GameFrameX.Coroutine.Editor
{
    /// <summary>
    /// 自定义协程组件的Inspector
    /// </summary>
    [CustomEditor(typeof(CoroutineModule))]
    internal sealed class CoroutineModuleInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (target is not CoroutineModule module) return;

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("当前执行的协程个数：", module.Count.ToString());
                foreach (var coroutine in module.AllCoroutines)
                {
                    EditorGUILayout.LabelField(coroutine.ToString());
                }
            }

            Repaint();
        }
    }
}