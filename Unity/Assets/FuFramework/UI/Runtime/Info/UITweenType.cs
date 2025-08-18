// ReSharper disable once CheckNamespace

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面打开/关闭时的动画类型
    /// </summary>
    public enum UITweenType
    {
        /// <summary>
        /// 无动画效果
        /// </summary>
        None,

        /// <summary>
        /// 淡入淡出效果
        /// </summary>
        Fade,

        /// <summary>
        /// 自定义动画效果，需要重写DoCustomTweenClose()和DoCustomTweenOpen()方法，以实现自定义动画效果
        /// </summary>
        Custom,
    }
}