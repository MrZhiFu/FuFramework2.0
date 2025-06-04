namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组常量
    /// </summary>
    public static class UIGroupConstants
    {
        /// <summary>
        /// 隐藏
        /// </summary>
        public static readonly UIGroupDefine Hidden = new UIGroupDefine(20, "Hidden");

        /// <summary>
        /// 底板
        /// </summary>
        public static readonly UIGroupDefine Floor = new UIGroupDefine(15, "Floor");

        /// <summary>
        /// 正常
        /// </summary>
        public static readonly UIGroupDefine Normal = new UIGroupDefine(10, "Normal");

        /// <summary>
        /// 固定
        /// </summary>
        public static readonly UIGroupDefine Fixed = new UIGroupDefine(0, "Fixed");

        /// <summary>
        /// 窗口
        /// </summary>
        public static readonly UIGroupDefine Window = new UIGroupDefine(-10, "Window");

        /// <summary>
        /// 提示
        /// </summary>
        public static readonly UIGroupDefine Tip = new UIGroupDefine(-15, "Tip");

        /// <summary>
        /// 引导
        /// </summary>
        public static readonly UIGroupDefine Guide = new UIGroupDefine(-20, "Guide");

        /// <summary>
        /// 黑板
        /// </summary>
        public static readonly UIGroupDefine BlackBoard = new UIGroupDefine(-22, "BlackBoard");

        /// <summary>
        /// 对话
        /// </summary>
        public static readonly UIGroupDefine Dialogue = new UIGroupDefine(-23, "Dialogue");

        /// <summary>
        /// Loading 
        /// </summary>
        public static readonly UIGroupDefine Loading = new UIGroupDefine(-25, "Loading");

        /// <summary>
        /// 通知
        /// </summary>
        public static readonly UIGroupDefine Notify = new UIGroupDefine(-30, "Notify");

        /// <summary>
        /// 系统顶级
        /// </summary>
        public static readonly UIGroupDefine System = new UIGroupDefine(-35, "System");
    }
}