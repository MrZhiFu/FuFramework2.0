/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIDialogMessageBox : ViewBase
    {
        public const string UIPackageName = "UICommon";
        public const string UIResName = "UIDialogMessageBox";
        public const string URL = "ui://ats3vms3iopl2l";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GButton m_enter_button { get; private set; }
		public GButton m_cancel_button { get; private set; }
		public GRichTextField m_content { get; private set; }

        protected override void InitView()
        {
            if(GObject == null)
            {
                return;
            }

            self = (GComponent)GObject;
            self.Add(this);
            
            var com = GObject.asCom;
            if (com != null)
            {
				m_enter_button = (GButton)com.GetChild("enter_button");
				m_cancel_button = (GButton)com.GetChild("cancel_button");
				m_content = (GRichTextField)com.GetChild("content");
            }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.Dispose();
            self.Remove();
			m_enter_button = null;
			m_cancel_button = null;
			m_content = null;
            self = null;            
        }
    }
}
#endif