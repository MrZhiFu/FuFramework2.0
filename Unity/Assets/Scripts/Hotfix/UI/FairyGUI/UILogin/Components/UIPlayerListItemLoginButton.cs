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
    public sealed partial class UIPlayerListItemLoginButton : ViewBase
    {
        public const string UIPackageName = "UILogin";
        public const string UIResName = "UIPlayerListItemLoginButton";
        public const string URL = "ui://f011l0h9i3dbs9o";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GRichTextField m_title { get; private set; }


        public static UIPlayerListItemLoginButton Create(GComponent go)
        {
            var fui = new UIPlayerListItemLoginButton();
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
				m_title = (GRichTextField)com.GetChild("title");
            }
        }

        public override void OnDispose()
        {
            base.OnDispose();
            self.Remove();
			m_title = null;
            self = null;            
        }
    }
}
#endif