using System;
using GameFrameX.UI.Runtime;
using UnityEngine;

namespace GameFrameX.UI.FairyGUI.Runtime
{
   
    public class GameFrameXuiToFairyGUICroppingHelper : MonoBehaviour
    {
        private Type[] m_Types;

       
        private void Start()
        {
            m_Types = new Type[]
            {
                typeof(GObjectHelper),
                typeof(ViewBase),
                typeof(UIManager),
                // typeof(FuiLoadAsyncResourceHelper),
                typeof(FuiPackageManager),
                typeof(FuiPathFinderHelper),
            };
        }
    }
}