using System;
using Hotfix.Framework.ReferencePools;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// 界面组中的界面信息。
    /// 目标：存储界面组中的界面信息，包括界面、是否暂停、是否被覆盖等。
    /// </summary>
    public sealed class WinInfo : IReference
    {
        /// <summary>
        /// 界面。
        /// </summary>
        public WinBase Win { get; private set; }

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
        /// <param name="win">界面。</param>
        /// <returns>创建的界面组界面信息。</returns>
        /// <exception cref="InvalidOperationException">界面为空时抛出。</exception>
        public static WinInfo Create(WinBase win)
        {
            if (win == null) throw new InvalidOperationException("[UIInfo] ui界面逻辑实例为空.");
            var uiInfo = GlobalModule.ReferencePoolModule.Acquire<WinInfo>();
            uiInfo.Win     = win;
            uiInfo.Paused  = false;
            uiInfo.Covered = false;
            return uiInfo;
        }

        /// <summary>
        /// 清理界面组界面信息。
        /// </summary>
        public void Clear()
        {
            Win     = null;
            Paused  = false;
            Covered = false;
        }
    }
}