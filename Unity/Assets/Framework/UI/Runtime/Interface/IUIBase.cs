﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面接口。
    /// </summary>
    public interface IUIBase
    {
        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        int SerialId { get; }

        /// <summary>
        /// 获取界面完整名称。
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        string UIAssetName { get; }

        /// <summary>
        /// 获取界面实例。
        /// </summary>
        object Handle { get; }

        /// <summary>
        /// 获取界面是否可见。
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        IUIGroup UIGroup { get; }

        /// <summary>
        /// 获取界面在界面组中的深度。
        /// </summary>
        int DepthInUIGroup { get; }

        /// <summary>
        /// 获取是否暂停被覆盖的界面。
        /// </summary>
        bool PauseCoveredUI { get; }

        /// <summary>
        /// 获取是否唤醒过
        /// </summary>
        bool IsAwake { get; }

        /// <summary>
        /// 界面初始化前执行
        /// </summary>
        void OnAwake();

        /// <summary>
        /// 初始化界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="uiGroup">界面所属的界面组。</param>
        /// <param name="onInitAction">初始化界面前的委托。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        void Init(int serialId, string uiAssetName, IUIGroup uiGroup, Action<IUIBase> onInitAction, bool pauseCoveredUI, bool isNewInstance, object userData, bool isFullScreen = false);

        /// <summary>
        /// 界面初始化。
        /// </summary>
        void OnInit();

        /// <summary>
        /// 界面回收。
        /// </summary>
        void OnRecycle();

        /// <summary>
        /// 界面打开。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        void OnOpen(object userData);

        /// <summary>
        /// 界面更新本地化。
        /// </summary>
        void UpdateLocalization();

        /// <summary>
        /// 界面关闭。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        void OnClose(bool isShutdown, object userData);

        /// <summary>
        /// 界面暂停。
        /// </summary>
        void OnPause();

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        void OnResume();

        /// <summary>
        /// 界面被遮挡。
        /// </summary>
        void OnBeCover();

        /// <summary>
        /// 界面遮挡恢复。
        /// </summary>
        void OnReveal();

        /// <summary>
        /// 界面激活。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        void OnRefocus(object userData);

        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        void OnUpdate(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 界面深度改变。
        /// </summary>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="depthInUIGroup">界面在界面组中的深度。</param>
        void OnDepthChanged(int uiGroupDepth, int depthInUIGroup);
    }
}