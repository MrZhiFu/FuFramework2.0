namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIGroupDefine
    {
        /// <summary>
        /// 界面组深度
        /// </summary>
        public readonly int Depth;

        /// <summary>
        /// 界面组名称
        /// </summary>
        public readonly string Name;

        public UIGroupDefine(int depth, string name)
        {
            Depth = depth;
            Name = name;
        }
    }
}