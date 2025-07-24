using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组中的界面信息。
    /// </summary>
    public sealed class UIInfo : IReference
    {
        /// <summary>
        /// 界面。
        /// </summary>
        public ViewBase View { get; private set; }

        /// <summary>
        /// 界面是否暂停(初始化时默认为false,即界面没有暂停)
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// 界面是否被覆盖(初始化时默认为false,即界面没有被覆盖)
        /// </summary>
        public bool Covered { get; set; }

        /// <summary>
        /// 创建界面组界面信息。
        /// </summary>
        /// <param name="view">界面。</param>
        /// <returns>创建的界面组界面信息。</returns>
        /// <exception cref="GameFrameworkException">界面为空时抛出。</exception>
        public static UIInfo Create(ViewBase view)
        {
            if (view == null) throw new GameFrameworkException("ui界面逻辑实例为空.");
            var uiInfo = ReferencePool.Acquire<UIInfo>();
            uiInfo.View    = view;
            uiInfo.Paused  = false;
            uiInfo.Covered = false;
            return uiInfo;
        }

        /// <summary>
        /// 清理界面组界面信息。
        /// </summary>
        public void Clear()
        {
            View    = null;
            Paused  = false;
            Covered = false;
        }
    }
}