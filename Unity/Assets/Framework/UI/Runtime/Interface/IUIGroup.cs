//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {
        /// <summary>
        /// 获取界面组名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取或设置界面组深度。
        /// </summary>
        int Depth { get; set; }

        /// <summary>
        /// 获取或设置界面组是否暂停。
        /// </summary>
        bool Pause { get; set; }

        /// <summary>
        /// 获取界面组中界面数量。
        /// </summary>
        int UICount { get; }

        /// <summary>
        /// 获取当前界面。
        /// </summary>
        IUIBase CurrentIuiBase { get; }

        /// <summary>
        /// 获取界面组辅助器。
        /// </summary>
        IUIGroupHelper Helper { get; }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>界面组中是否存在界面。</returns>
        bool HasUI(int serialId);

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        bool HasUI(string uiAssetName);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        IUIBase GetUI(int serialId);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        IUIBase GetUI(string uiAssetName);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        IUIBase[] GetUIs(string uiAssetName);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        void GetUIs(string uiAssetName, List<IUIBase> results);

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <returns>界面组中的所有界面。</returns>
        IUIBase[] GetAllUIs();

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <param name="results">界面组中的所有界面。</param>
        void GetAllUIs(List<IUIBase> results);

        /// <summary>
        /// 检查界面组中是否存在指定界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="iuiBase">要检查的界面。</param>
        /// <returns>是否存在指定界面。</returns>
        bool InternalHasInstanceUI(string uiAssetName, IUIBase iuiBase);

        /// <summary>
        /// 往界面组增加界面。
        /// </summary>
        /// <param name="iuiBase">要增加的界面。</param>
        void AddUI(IUIBase iuiBase);

        /// <summary>
        /// 刷新界面组。
        /// </summary>
        void Refresh();
    }
}