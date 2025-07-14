/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIPlayerCreate : ViewBase
    {
        public const string UIPackageName = "UILogin";
        public const string UIResName = "UIPlayerCreate";
        public const string URL = "ui://f011l0h9i3dbs9p";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GTextInput m_UserName { get; private set; }
		public GButton m_enter { get; private set; }
		public GTextField m_ErrorText { get; private set; }

        private void OnInitUI()
        {
            if(UIView == null)
            {
                return;
            }

            self = (GComponent)UIView;
            self.Add(this);
            
            var com = UIView.asCom;
            if (com != null)
            {
				m_UserName = (GTextInput)com.GetChild("UserName");
				m_enter = (GButton)com.GetChild("enter");
				m_ErrorText = (GTextField)com.GetChild("ErrorText");
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            self.Remove();
			m_UserName = null;
			m_enter = null;
			m_ErrorText = null;
            self = null;            
        }
    }
}
#endif