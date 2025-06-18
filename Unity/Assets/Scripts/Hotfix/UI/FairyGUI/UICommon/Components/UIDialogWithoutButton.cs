/** This is an automatically generated class by FairyGUI. Please do not modify it. **/

#if ENABLE_UI_FAIRYGUI
using FairyGUI;
using FairyGUI.Utils;
using GameFrameX.UI.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.Runtime;

namespace Hotfix.UI
{
    public sealed partial class UIDialogWithoutButton : FUI
    {
        public const string UIPackageName = "UICommon";
        public const string UIResName = "UIDialogWithoutButton";
        public const string URL = "ui://ats3vms3srah1v";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public GButton m_close_icon { get; private set; }

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
				m_close_icon = (GButton)com.GetChild("close_icon");
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
			m_close_icon = null;
            self = null;            
        }
    }
}
#endif