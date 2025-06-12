using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    [Preserve]
    public class GameFrameXuiToFairyGUICroppingHelper : MonoBehaviour
    {
        private Type[] m_Types;

        [Preserve]
        private void Start()
        {
            m_Types = new Type[]
            {
                typeof(GObjectHelper),
                typeof(FUI),
                typeof(UIManager),
                typeof(FuiLoadAsyncResourceHelper),
                typeof(FuiPackageComponent),
                typeof(FuiPathFinderHelper),
            };
        }
    }
}