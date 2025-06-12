using GameFrameX.UI.FairyGUI.Runtime;
using GameFrameX.UI.Runtime;

namespace Unity.Startup
{
    public partial class UILauncher
    {
        public override void OnAwake()
        {
            UIGroup = UIManager.Instance.GetUIGroup(UILayer.Normal);
            base.OnAwake();
        }
    }
}