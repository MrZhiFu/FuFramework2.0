﻿//------------------------------------------------------------
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
        int UIFormCount { get; }

        /// <summary>
        /// 获取当前界面。
        /// </summary>
        IUIForm CurrentUIForm { get; }

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
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        bool HasUI(string uiFormAssetName);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        IUIForm GetUI(int serialId);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        IUIForm GetUI(string uiFormAssetName);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        IUIForm[] GetUIs(string uiFormAssetName);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        void GetUIs(string uiFormAssetName, List<IUIForm> results);

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <returns>界面组中的所有界面。</returns>
        IUIForm[] GetAllUIs();

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <param name="results">界面组中的所有界面。</param>
        void GetAllUIs(List<IUIForm> results);

        /// <summary>
        /// 检查界面组中是否存在指定界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiForm">要检查的界面。</param>
        /// <returns>是否存在指定界面。</returns>
        bool InternalHasInstanceUI(string uiFormAssetName, IUIForm uiForm);

        /// <summary>
        /// 往界面组增加界面。
        /// </summary>
        /// <param name="uiForm">要增加的界面。</param>
        void AddUI(IUIForm uiForm);

        /// <summary>
        /// 刷新界面组。
        /// </summary>
        void Refresh();
    }
}