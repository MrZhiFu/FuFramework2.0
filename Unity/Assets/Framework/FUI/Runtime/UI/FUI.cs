using FairyGUI;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FUI界面基类
    /// </summary>
    public class FUI : ViewBase
    {
        /// <summary>
        /// 初始化UI
        /// </summary>
        /// <param name="gObject"></param>
        public FUI(GObject gObject)
        {
            GObject = gObject;
            InitView();
        }
    }
}