using GameFrameX.Editor;
using FuFramework.Core.Editor;
using GameFrameX.Timer.Runtime;
using UnityEditor;

namespace GameFrameX.Timer.Editor
{
    [CustomEditor(typeof(TimerComponent))]
    internal sealed class TimerGameComponentInspector : GameComponentInspector
    {
        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(ITimerManager));
        }
    }
}
