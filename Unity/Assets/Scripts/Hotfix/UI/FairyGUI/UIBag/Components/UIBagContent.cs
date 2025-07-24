/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FuFramework.UI.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIBagContent : ViewBase
    {
        public const string UIPackageName = "UIBag";
        public const string UIResName = "UIBagContent";
        public const string URL = "ui://a3awyna7l50q2";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public Controller m_IsSelectedItem { get; private set; }
		public GList m_list { get; private set; }
		public UIBagItemInfo m_info { get; private set; }
		public GList m_type_list { get; private set; }


        public static UIBagContent Create(GComponent go)
        {
            var fui = new UIBagContent();
            fui?.SetUIView(go);
            fui?.OnInitUI();
            return fui;
        }

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
				m_IsSelectedItem = com.GetController("IsSelectedItem");
				m_list = (GList)com.GetChild("list");
				m_info = UIBagItemInfo.Create(com.GetChild("info") as GComponent);
				m_type_list = (GList)com.GetChild("type_list");
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            self.Remove();
			m_IsSelectedItem = null;
			m_list = null;
			m_info = null;
			m_type_list = null;
            self = null;            
        }
    }
}
#endif