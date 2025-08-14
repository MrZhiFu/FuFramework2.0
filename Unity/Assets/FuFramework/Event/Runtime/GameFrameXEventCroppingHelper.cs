using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace FuFramework.Event.Runtime
{
   
    public class GameFrameXEventCroppingHelper : MonoBehaviour
    {
        private Type[] m_Types;

       
        private void Start()
        {
            m_Types = new Type[]
            {
                typeof(EventManager),
                typeof(EventComponent),
                typeof(GameEventArgs),
                typeof(IEventManager),
            };
        }
    }
}