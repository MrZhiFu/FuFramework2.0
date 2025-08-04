// ReSharper disable once CheckNamespace 禁用命名空间检查

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面层级
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 世界场景中的UI，如：HUD, 血条等
        /// </summary>
        WorldUI = 0,

        /// <summary>
        /// 主界面
        /// </summary>
        MainUI = 1500,

        /// <summary>
        /// 一般全屏界面
        /// </summary>
        Normal = 2000,

        /// <summary>
        /// 窗口
        /// </summary>
        Window = 2500,

        /// <summary>
        /// 提示
        /// </summary>
        Tip = 3000,

        /// <summary>
        /// 引导
        /// </summary>
        Guide = 3500,

        /// <summary>
        /// Loading 
        /// </summary>
        Loading = 4000,
    }
}