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
    internal sealed class CoroutineComponentInspector : FuFrameworkInspector
    {
    }
}