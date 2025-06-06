namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组定义：名称和深度
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIGroupDefine
    {
        /// <summary>
        /// 界面组名称
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// 界面组深度
        /// </summary>
        public readonly int Depth;

        public UIGroupDefine(string name, int depth)
        {
            Name = name;
            Depth = depth;
        }
    }
    
    /// <summary>
    /// 所有界面组定义
    /// </summary>
    public static class UIGroupConstants
    {
        /// <summary>
        /// 隐藏
        /// </summary>
        public static readonly UIGroupDefine Hidden = new("Hidden", 20);

        /// <summary>
        /// 底板
        /// </summary>
        public static readonly UIGroupDefine Floor = new("Floor", 15);

        /// <summary>
        /// 正常
        /// </summary>
        public static readonly UIGroupDefine Normal = new("Normal", 10);

        /// <summary>
        /// 固定
        /// </summary>
        public static readonly UIGroupDefine Fixed = new("Fixed", 0);

        /// <summary>
        /// 窗口
        /// </summary>
        public static readonly UIGroupDefine Window = new("Window", -10);

        /// <summary>
        /// 提示
        /// </summary>
        public static readonly UIGroupDefine Tip = new("Tip", -15);

        /// <summary>
        /// 引导
        /// </summary>
        public static readonly UIGroupDefine Guide = new("Guide", -20);

        /// <summary>
        /// 黑板
        /// </summary>
        public static readonly UIGroupDefine BlackBoard = new("BlackBoard", -22);

        /// <summary>
        /// 对话
        /// </summary>
        public static readonly UIGroupDefine Dialogue = new("Dialogue", -23);

        /// <summary>
        /// Loading 
        /// </summary>
        public static readonly UIGroupDefine Loading = new("Loading", -25);

        /// <summary>
        /// 通知
        /// </summary>
        public static readonly UIGroupDefine Notify = new("Notify", -30);

        /// <summary>
        /// 系统顶级
        /// </summary>
        public static readonly UIGroupDefine System = new("System", -35);
    }
}