//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面辅助器接口。
    /// </summary>
    public interface IUIHelper
    {
        /// <summary>
        /// 实例化界面。
        /// </summary>
        /// <param name="uiAsset">要实例化的界面资源。</param>
        /// <returns>实例化后的界面。</returns>
        object InstantiateUI(object uiAsset);

        /// <summary>
        /// 创建界面。
        /// </summary>
        /// <param name="uiInstance">界面实例。</param>
        /// <param name="uiLogicType">界面逻辑类型</param>
        /// <returns>界面。</returns>
        IUIBase CreateUI(object uiInstance, Type uiLogicType);

        /// <summary>
        /// 释放界面。
        /// </summary>
        /// <param name="uiInstance">要释放的界面实例。</param>
        void ReleaseUI(object uiInstance);
    }
}