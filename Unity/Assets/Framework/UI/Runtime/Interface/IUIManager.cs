//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFrameX.Asset.Runtime;
using GameFrameX.ObjectPool;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面管理器接口。
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        int UIGroupCount { get; }

        /// <summary>
        /// 获取或设置界面实例对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        float InstanceAutoReleaseInterval { get; set; }

        /// <summary>
        /// 获取或设置界面实例对象池的容量。
        /// </summary>
        int InstanceCapacity { get; set; }

        /// <summary>
        /// 获取或设置界面实例对象池对象过期秒数。
        /// </summary>
        float InstanceExpireTime { get; set; }

        /// <summary>
        /// 打开界面成功事件。
        /// </summary>
        event EventHandler<OpenUISuccessEventArgs> OpenUISuccess;

        /// <summary>
        /// 打开界面失败事件。
        /// </summary>
        event EventHandler<OpenUIFailureEventArgs> OpenUIFailure;

        /// <summary>
        /// 关闭界面完成事件。
        /// </summary>
        event EventHandler<CloseUICompleteEventArgs> CloseUIComplete;

        /// <summary>
        /// 设置对象池管理器。
        /// </summary>
        /// <param name="objectPoolManager">对象池管理器。</param>
        void SetObjectPoolManager(IObjectPoolManager objectPoolManager);

        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="assetManager">资源管理器。</param>
        void SetResourceManager(IAssetManager assetManager);

        /// <summary>
        /// 设置界面辅助器。
        /// </summary>
        /// <param name="iUiHelper">界面辅助器。</param>
        void SetUIHelper(IUIHelper iUiHelper);

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        bool HasUIGroup(string uiGroupName);

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        IUIGroup GetUIGroup(string uiGroupName);

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        IUIGroup[] GetAllUIGroups();

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        void GetAllUIGroups(List<IUIGroup> results);

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        /// <returns>是否增加界面组成功。</returns>
        bool AddUIGroup(string uiGroupName, IUIGroupHelper uiGroupHelper);

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        /// <returns>是否增加界面组成功。</returns>
        bool AddUIGroup(string uiGroupName, int uiGroupDepth, IUIGroupHelper uiGroupHelper);

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        bool HasUI(int serialId);

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        bool HasUI(string uiAssetName);

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        IUIBase GetUI(int serialId);

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        IUIBase GetUI(string uiAssetName);

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        IUIBase[] GetUIs(string uiAssetName);

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        void GetUIs(string uiAssetName, List<IUIBase> results);

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        IUIBase[] GetAllLoadedUIs();

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        void GetAllLoadedUIs(List<IUIBase> results);

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        int[] GetAllLoadingUISerialIds();

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载界面的序列编号。</param>
        void GetAllLoadingUISerialIds(List<int> results);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        bool IsLoadingUI(int serialId);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        bool IsLoadingUI(string uiAssetName);

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="iuiBase">界面。</param>
        /// <returns>界面是否合法。</returns>
        bool IsValidUI(IUIBase iuiBase);

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        Task<IUIBase> OpenUIAsync<T>(string uiAssetPath, bool pauseCoveredUI, object userData, bool isFullScreen = false, bool isMultiple = false) where T : class, IUIBase;

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="uiType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        Task<IUIBase> OpenUIAsync(string uiAssetPath, Type uiType, bool pauseCoveredUI, object userData, bool isFullScreen = false, bool isMultiple = false);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        void CloseUI(int serialId);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        void CloseUINow(int serialId);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        void CloseUI(int serialId, object userData);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        void CloseUINow(int serialId, object userData);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="iuiBase">要关闭的界面。</param>
        void CloseUI(IUIBase iuiBase);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="iuiBase">要关闭的界面。</param>
        void CloseUINow(IUIBase iuiBase);

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        void CloseUI<T>(object userData) where T : IUIBase;

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        void CloseUINow<T>(object userData) where T : IUIBase;

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="iuiBase">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        void CloseUI(IUIBase iuiBase, object userData);

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="iuiBase">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        void CloseUINow(IUIBase iuiBase, object userData);

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        void CloseAllLoadedUIs();

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        void CloseAllLoadedUIs(object userData);

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        void CloseAllLoadingUIs();

        /// <summary>
        /// 设置界面实例是否被加锁。
        /// </summary>
        /// <param name="uiInstance">要设置是否被加锁的界面实例。</param>
        /// <param name="locked">界面实例是否被加锁。</param>
        void SetUIInstanceLocked(object uiInstance, bool locked);
    }
}