/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIBag : ViewBase
    {
        public const string UIPackageName = "UIBag";
        public const string UIResName = "UIBag";
        public const string URL = "ui://a3awyna7l50q1";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GGraph m_bg { get; private set; }
		public UIBagContent m_content { get; private set; }

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
				m_bg = (GGraph)com.GetChild("bg");
				m_content = UIBagContent.Create(com.GetChild("content") as GComponent);
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
			m_bg = null;
			m_content = null;
            self = null;            
        }
    }
}
#endif