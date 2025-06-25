/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIAnnouncement : ViewBase
    {
        public const string UIPackageName = "UILogin";
        public const string UIResName = "UIAnnouncement";
        public const string URL = "ui://f011l0h9aneks9g";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GGraph m_MaskLayer { get; private set; }
		public UIAnnouncementContent m_TextContent { get; private set; }
		public GTextField m_TextTitle { get; private set; }

        protected override void InitView()
        {
            if(UIComp == null)
            {
                return;
            }

            self = (GComponent)UIComp;
            self.Add(this);
            
            var com = UIComp.asCom;
            if (com != null)
            {
				m_MaskLayer = (GGraph)com.GetChild("MaskLayer");
				m_TextContent = UIAnnouncementContent.Create(com.GetChild("TextContent") as GComponent);
				m_TextTitle = (GTextField)com.GetChild("TextTitle");
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
			m_MaskLayer = null;
			m_TextContent = null;
			m_TextTitle = null;
            self = null;            
        }
    }
}
#endif