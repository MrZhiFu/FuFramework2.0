using GameFrameX.Editor;
using GameFrameX.Timer.Runtime;
using UnityEditor;

namespace GameFrameX.Timer.Editor
{
    [CustomEditor(typeof(TimerComponent))]
    internal sealed class TimerComponentInspector : ComponentTypeComponentInspector
    {
        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(ITimerManager));
        }
    }
}
