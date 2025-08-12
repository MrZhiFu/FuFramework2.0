using FuFramework.Core.Editor;
using FuFramework.Coroutine.Runtime;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace GameFrameX.Coroutine.Editor
{
    [CustomEditor(typeof(CoroutineComponent))]
    internal sealed class CoroutineComponentInspector : FuFrameworkInspector
    {
    }
}