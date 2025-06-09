using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameFrameX.UI.Runtime
{
    [Preserve]
    public class GameFrameXUICroppingHelper : MonoBehaviour
    {
        private Type[] m_Types;

        [Preserve]
        private void Start()
        {
            m_Types = new[]
            {
                typeof(IUIManager),
                typeof(UIComponent),
                typeof(UIGroupHelperBase),
                typeof(UIHelperBase),
                typeof(UIBase),
                typeof(UIGroup),
                typeof(UIInfo),
                typeof(OpenUIInfo),
                typeof(UIGroupDefine),
                typeof(UIGroupConstants),
                typeof(EventRegister),
                typeof(CloseUICompleteEventArgs),
                typeof(OpenUIFailureEventArgs),
                typeof(OpenUISuccessEventArgs),
            };
        }
    }
}