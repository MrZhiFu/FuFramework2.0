using System.Collections.Generic;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// UI配置参数
    /// </summary>
    public static class FUIConfig
    {
        /// 绑定所有自定义组件Comp的绑定器字典，key：包名，value：绑定器
        public static readonly Dictionary<string, IUICompBinder> CompBinderDic = new();

        /// 设计分辨率X
        public const int DefaultResolutionX = 1920;

        /// 设计分辨率Y
        public const int DefaultResolutionY = 1080;
        
    }
}