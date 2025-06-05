using GameFrameX.UI.Runtime;

namespace Unity.Startup
{
    public partial class UILauncher
    {
        public override void OnAwake()
        {
            UIGroup = GameApp.UI.GetUIGroup(UIGroupConstants.Normal.Name);
            base.OnAwake();
        }
    }
}