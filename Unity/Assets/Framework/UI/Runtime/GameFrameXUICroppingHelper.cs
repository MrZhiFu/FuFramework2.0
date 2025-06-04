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
                typeof(UIStringKey),
                typeof(UIComponent),
                typeof(UIIntKey),
                typeof(UIGroupHelperBase),
                typeof(UIFormHelperBase),
                typeof(UIForm),
                typeof(UIGroup),
                typeof(UIFormInfo),
                typeof(OpenUIFormInfo),
                typeof(UIGroupDefine),
                typeof(UIGroupConstants),
                typeof(UIEventSubscriber),
                typeof(CloseUIFormCompleteEventArgs),
                typeof(OpenUIFormFailureEventArgs),
                typeof(OpenUIFormSuccessEventArgs),
            };
        }
    }
}