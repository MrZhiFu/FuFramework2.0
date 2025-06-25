/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using Cysharp.Threading.Tasks;
using FairyGUI.Utils;
using GameFrameX.Entity.Runtime;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;
using UnityEngine;

namespace Hotfix.UI
{
    public sealed partial class UIBagItemInfo : ViewBase
    {
        public const string UIPackageName = "UIBag";
        public const string UIResName = "UIBagItemInfo";
        public const string URL = "ui://a3awyna7l50q3";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public Controller m_IsCanUse { get; private set; }
		public GTextField m_name_text { get; private set; }
		public GRichTextField m_desc_text { get; private set; }
		public GButton m_good_item { get; private set; }
		public GButton m_use_button { get; private set; }
		public GButton m_get_source_button { get; private set; }


        public static UIBagItemInfo Create(GComponent go)
        {
            var fui = go.displayObject.gameObject.GetOrAddComponent<UIBagItemInfo>();
            fui?.SetUIView(go);
            fui?.InitView();
            return fui;
        }

        protected override void InitView()
        {
            if(View == null)
            {
                return;
            }

            self = (GComponent)View;
            self.Add(this);
            
            var com = View.asCom;
            if (com != null)
            {
				m_IsCanUse = com.GetController("IsCanUse");
				m_name_text = (GTextField)com.GetChild("name_text");
				m_desc_text = (GRichTextField)com.GetChild("desc_text");
				m_good_item = (GButton)com.GetChild("good_item");
				m_use_button = (GButton)com.GetChild("use_button");
				m_get_source_button = (GButton)com.GetChild("get_source_button");
            }
        }

        public override void OnDispose()
        {
            if (IsDisposed)
            {
                return;
            }

            base.OnDispose();
            self.Remove();
			m_IsCanUse = null;
			m_name_text = null;
			m_desc_text = null;
			m_good_item = null;
			m_use_button = null;
			m_get_source_button = null;
            self = null;            
        }
    }
}
#endif