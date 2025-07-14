/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIMain : ViewBase
    {
        public const string UIPackageName = "UIMain";
        public const string UIResName = "UIMain";
        public const string URL = "ui://q9u97yzfxws70";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GLoader m_bg { get; private set; }
		public GButton m_bag_button { get; private set; }
		public GLoader m_player_icon { get; private set; }
		public GTextField m_player_name { get; private set; }
		public GTextField m_player_level { get; private set; }

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
				m_bg = (GLoader)com.GetChild("bg");
				m_bag_button = (GButton)com.GetChild("bag_button");
				m_player_icon = (GLoader)com.GetChild("player_icon");
				m_player_name = (GTextField)com.GetChild("player_name");
				m_player_level = (GTextField)com.GetChild("player_level");
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            self.Remove();
			m_bg = null;
			m_bag_button = null;
			m_player_icon = null;
			m_player_name = null;
			m_player_level = null;
            self = null;            
        }
    }
}
#endif