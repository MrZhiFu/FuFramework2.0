using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    public class GameFrameXuiToFairyGUICroppingHelper : MonoBehaviour
    {
        private Type[] m_Types;
       
        private void Start()
        {
            m_Types = new[]
            {
                typeof(ViewBase),
                typeof(UIManager),
                typeof(FuiPackageManager),
                typeof(FuiPathFinderHelper),
            };
        }
    }
}