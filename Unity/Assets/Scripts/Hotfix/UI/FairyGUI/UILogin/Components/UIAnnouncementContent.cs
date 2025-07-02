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
    public sealed partial class UIAnnouncementContent : ViewBase
    {
        public const string UIPackageName = "UILogin";
        public const string UIResName = "UIAnnouncementContent";
        public const string URL = "ui://f011l0h9aneks9i";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GRichTextField m_LabelContent { get; private set; }


        public static UIAnnouncementContent Create(GComponent go)
        {
            var fui = new UIAnnouncementContent();
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
				m_LabelContent = (GRichTextField)com.GetChild("LabelContent");
            }
        }

        public override void OnDispose()
        {
            base.OnDispose();
            self.Remove();
			m_LabelContent = null;
            self = null;            
        }
    }
}
#endif