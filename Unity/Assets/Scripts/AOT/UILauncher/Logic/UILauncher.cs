using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.UI.Runtime;

namespace Unity.Startup
{
    public partial class UILauncher
    {
        protected override void OnInit()
        {
            base.OnInit();
            OnInitUI();
        }
    }
}