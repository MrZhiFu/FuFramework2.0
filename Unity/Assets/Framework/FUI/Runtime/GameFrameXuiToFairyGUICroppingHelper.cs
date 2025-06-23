using System;
using UnityEngine;
using UnityEngine.Scripting;

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
                typeof(FUI),
                typeof(UIManager),
                typeof(FuiLoadAsyncResourceHelper),
                typeof(FuiPackageManager),
                typeof(FuiPathFinderHelper),
            };
        }
    }
}