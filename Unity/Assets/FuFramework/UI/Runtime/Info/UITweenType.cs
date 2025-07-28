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
        /// 移动X轴效果
        /// </summary>
        MoveX,
        
        /// <summary>
        /// 移动Y轴效果
        /// </summary>
        MoveY,
        
        /// <summary>
        /// 同时移动X轴和Y轴效果
        /// </summary>
        MoveXY,
        
        /// <summary>
        /// 缩放X轴效果
        /// </summary>
        ScaleX,
        
        /// <summary>
        /// 缩放Y轴效果
        /// </summary>
        ScaleY,
        
        /// <summary>
        /// 同时缩放X轴和Y轴效果
        /// </summary>
        ScaleXY,
        
        /// <summary>
        /// 旋转效果
        /// </summary>
        Rotate,
    }
}