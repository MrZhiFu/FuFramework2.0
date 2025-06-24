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

namespace Unity.Startup
{
    public sealed partial class UILauncher : ViewBase
    {
        public const string UIPackageName = "UILauncher";
        public const string UIResName = "UILauncher";
        public const string URL = "ui://u7deosq0mw8e0";

        /// <summary>
        /// {uiResName}的组件类型(GComponent、GButton、GProcessBar等)，它们都是GObject的子类。
        /// </summary>
        public GComponent self { get; private set; }

		public Controller m_IsUpgrade { get; private set; }
		public Controller m_IsDownload { get; private set; }
		public GLoader m_bg { get; private set; }
		public GTextField m_TipText { get; private set; }
		public GProgressBar m_ProgressBar { get; private set; }
		public UILauncherUpgrade m_upgrade { get; private set; }

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
				m_IsUpgrade = com.GetController("IsUpgrade");
				m_IsDownload = com.GetController("IsDownload");
				m_bg = (GLoader)com.GetChild("bg");
				m_TipText = (GTextField)com.GetChild("TipText");
				m_ProgressBar = (GProgressBar)com.GetChild("ProgressBar");
				m_upgrade = UILauncherUpgrade.Create(com.GetChild("upgrade"));
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
			m_IsUpgrade = null;
			m_IsDownload = null;
			m_bg = null;
			m_TipText = null;
			m_ProgressBar = null;
			m_upgrade = null;
            self = null;            
        }
    }
}
#endif