using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
    /// <summary>
    /// 红点节点状态
    /// </summary>
    public struct RedDotState
    {
        /// <summary>
        /// 红点数量（TotalCount）
        /// </summary>
        public int Count;

        /// <summary>
        /// 节点是否激活
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// 显示模式
        /// </summary>
        public ERedDotDisplayMode DisplayMode;

        /// <summary>
        /// 静态空状态
        /// </summary>
        public static readonly RedDotState Empty = new()
        {
            Count       = 0,
            IsActive    = false,
            DisplayMode = ERedDotDisplayMode.DotOnly
        };
    }
}
