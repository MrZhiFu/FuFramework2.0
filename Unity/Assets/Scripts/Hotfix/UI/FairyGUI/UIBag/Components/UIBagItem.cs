/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;
using UnityEngine;

namespace Hotfix.UI
{
    public sealed partial class UIBagItem : ViewBase
    {
        public const string UIPackageName = "UIBag";
        public const string UIResName = "UIBagItem";
        public const string URL = "ui://a3awyna7l50q5";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GButton self { get; private set; }

		public GButton m_good_item { get; private set; }

        public static UIBagItem Create(GComponent go)
        {
            var fui = go.displayObject.gameObject.GetOrAddComponent<UIBagItem>();
            fui?.SetUIView(go);
            fui?.InitView();
            return fui;
        }

        /// <summary>
        /// 通过此方法获取的FUI，在Dispose时不会释放GObject，需要自行管理（一般在配合FGUI的Pool机制时使用）。
        /// </summary>
        public static UIBagItem GetFormPool(GComponent go)
        {
            var fui = go.Get<UIBagItem>();
            if (fui == null)
            {
                fui = Create(go);
            }
            return fui;
        }

        protected override void InitView()
        {
            if(View == null)
            {
                return;
            }

            self = (GButton)View;
            self.Add(this);
            
            var com = View.asCom;
            if (com != null)
            {
				m_good_item = (GButton)com.GetChild("good_item");
            }
        }

        public override void OnDispose()
        {
            base.OnDispose();
            self.Remove();
			m_good_item = null;
            self = null;            
        }
    }
}
#endif