using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Event.Runtime
{
   
    public class GameFrameXEventCroppingHelper : MonoBehaviour
    {
        private Type[] m_Types;

       
        private void Start()
        {
            m_Types = new[]
            {
                typeof(EventManager),
                typeof(GameEventArgs),
            };
        }
    }
}