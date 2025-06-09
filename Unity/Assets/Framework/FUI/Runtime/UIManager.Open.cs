//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Threading.Tasks;
using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器.打开界面
    /// </summary>
    internal sealed partial class UIManager
    {
        private EventHandler<OpenUISuccessEventArgs> m_OpenUISuccessEventHandler;
        private EventHandler<OpenUIFailureEventArgs> m_OpenUIFailureEventHandler;

        /// <summary>
        /// 打开界面成功事件。
        /// </summary>
        public event EventHandler<OpenUISuccessEventArgs> OpenUISuccess
        {
            add => m_OpenUISuccessEventHandler += value;
            remove => m_OpenUISuccessEventHandler -= value;
        }

        /// <summary>
        /// 打开界面失败事件。
        /// </summary>
        public event EventHandler<OpenUIFailureEventArgs> OpenUIFailure
        {
            add => m_OpenUIFailureEventHandler += value;
            remove => m_OpenUIFailureEventHandler -= value;
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public Task<IUIBase> OpenUIAsync<T>(string uiAssetPath, bool pauseCoveredUI, object userData, bool isFullScreen = false,
                                            bool isMultiple = false) where T : class, IUIBase
        {
            return InnerOpenUIAsync(uiAssetPath, typeof(T), pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="uiType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        public async Task<IUIBase> OpenUIAsync(string uiAssetPath, Type uiType, bool pauseCoveredUI, object userData,
                                               bool isFullScreen = false, bool isMultiple = false)
        {
            return await InnerOpenUIAsync(uiAssetPath, uiType, pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="uiType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        private async Task<IUIBase> InnerOpenUIAsync(string uiAssetPath, Type uiType, bool pauseCoveredUI, object userData,
                                                     bool isFullScreen = false, bool isMultiple = false)
        {
            var uiAssetName = uiType.Name;

            UIInstanceObject uiInstanceObject = m_InstancePool.Spawn(uiAssetName);
            if (uiInstanceObject != null && isMultiple == false)
            {
                // 如果对象池存在
                return InternalOpenUI(-1, uiAssetName, uiType, uiInstanceObject.Target, pauseCoveredUI, false, 0f, userData,
                                      isFullScreen);
            }

            var serialId = ++m_Serial;
            m_LoadingDict.Add(serialId, uiAssetName);
            var assetPath = PathHelper.Combine(uiAssetPath, uiAssetName);

            var lastIndexOfStart = uiAssetPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            var packageName      = uiAssetPath.Substring(lastIndexOfStart + 1);

            // 检查UI包是否已经加载过
            var hasUIPackage = FuiPackage.HasPackage(packageName);

            var packageInfo = OpenUIPackageInfo.Create(packageName, uiAssetName);
            var openUIInfo  = OpenUIInfo.Create(serialId, uiType, pauseCoveredUI, userData, isFullScreen);

            // 检查路径中是否包含Bundle目录，如果不包含则从Resources中加载
            if (assetPath.IndexOf(Utility.Asset.Path.BundlesDirectoryName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                // 从Resources中加载
                if (!hasUIPackage)
                    FuiPackage.AddPackageSync(assetPath);
                return LoadAssetSuccessCallback(uiAssetName, packageInfo, 0, openUIInfo);
            }

            // 检查UI包是否已经加载过
            if (hasUIPackage)
            {
                // 如果UI 包存在则创建界面
                return LoadAssetSuccessCallback(uiAssetName, packageInfo, 1, openUIInfo);
            }

            if (packageName == uiAssetName)
            {
                // 如果UI资源名字和包名一致则直接加载
                await FuiPackage.AddPackageAsync(assetPath);
            }
            else
            {
                // 不一致则重新拼接路径
                string newPackagePath = PathHelper.Combine(uiAssetPath, packageName);
                await FuiPackage.AddPackageAsync(newPackagePath);
            }

            string newAssetPackagePath = assetPath;
            if (packageName != uiAssetName)
            {
                newAssetPackagePath = PathHelper.Combine(uiAssetPath, packageName);
            }

            // 从包中加载
            newAssetPackagePath += "_fui";
            var assetHandle = await m_AssetManager.LoadAssetAsync<UnityEngine.Object>(newAssetPackagePath);

            if (assetHandle.IsSucceed)
            {
                // 加载成功
                return LoadAssetSuccessCallback(uiAssetName, packageInfo, assetHandle.Progress, openUIInfo);
            }

            // UI包不存在
            return LoadAssetFailureCallback(uiAssetName, assetHandle.LastError, openUIInfo);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="serialId"></param>
        /// <param name="uiAssetName"></param>
        /// <param name="uiType"></param>
        /// <param name="uiInstance"></param>
        /// <param name="pauseCoveredUI"></param>
        /// <param name="isNewInstance"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        /// <param name="isFullScreen"></param>
        /// <returns></returns>
        /// <exception cref="GameFrameworkException"></exception>
        private IUIBase InternalOpenUI(int serialId, string uiAssetName, Type uiType, object uiInstance, bool pauseCoveredUI,
                                       bool isNewInstance, float duration, object userData, bool isFullScreen)
        {
            try
            {
                // 使用界面辅助器创建界面实例
                // 1.将传入的UI界面实例uiInstance加上UI界面逻辑组件uiType，
                // 2.将uiInstance作为一个子节点添加到UI界面组的显示对象下。
                IUIBase iuiBase = _iuiHelper.CreateUI(uiInstance, uiType);
                if (iuiBase == null) throw new GameFrameworkException("不能从界面辅助器中创建界面实例.");

                // 界面初始化回调，设置UI的GObject
                void OnInitAction(IUIBase obj)
                {
                    if (obj is not FUI fui) return;
                    fui.SetGObject(uiInstance as GObject);
                }

                // 初始化界面
                var uiGroup = iuiBase.UIGroup;
                iuiBase.Init(serialId, uiAssetName, uiGroup, OnInitAction, pauseCoveredUI, isNewInstance, userData, isFullScreen);

                // 界面组中是否存在该界面，不存在则添加
                if (!uiGroup.InternalHasInstanceUI(uiAssetName, iuiBase))
                {
                    uiGroup.AddUI(iuiBase);
                }

                iuiBase.OnOpen(userData);     // 界面打开回调
                iuiBase.UpdateLocalization(); // 更新本地化文本
                uiGroup.Refresh();            // 刷新界面组

                if (m_OpenUISuccessEventHandler != null)
                {
                    var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(iuiBase, duration, userData);
                    m_OpenUISuccessEventHandler(this, openUISuccessEventArgs);
                    // ReferencePool.Release(openUISuccessEventArgs);
                }

                return iuiBase;
            }
            catch (Exception exception)
            {
                if (m_OpenUIFailureEventHandler == null) throw;
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(serialId, uiAssetName, pauseCoveredUI, exception.ToString(), userData);
                m_OpenUIFailureEventHandler(this, openUIFailureEventArgs);
                return GetUI(openUIFailureEventArgs.SerialId);
            }
        }

        /// <summary>
        /// 设置界面实例是否被加锁。
        /// </summary>
        /// <param name="uiInstance">要设置是否被加锁的界面实例。</param>
        /// <param name="locked">界面实例是否被加锁。</param>
        public void SetUIInstanceLocked(object uiInstance, bool locked)
        {
            GameFrameworkGuard.NotNull(uiInstance, nameof(uiInstance));
            m_InstancePool.SetLocked(uiInstance, locked);
        }

        /// <summary>
        /// 设置界面实例的优先级。
        /// </summary>
        /// <param name="uiInstance">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIInstancePriority(object uiInstance, int priority)
        {
            GameFrameworkGuard.NotNull(uiInstance, nameof(uiInstance));
            m_InstancePool.SetPriority(uiInstance, priority);
        }

        /// <summary>
        /// 加载界面资源成功回调。
        /// </summary>
        /// <param name="uiAssetName"></param>
        /// <param name="uiPackageInfo"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private IUIBase LoadAssetSuccessCallback(string uiAssetName, object uiPackageInfo, float duration, object userData)
        {
            var openUIInfo = (OpenUIInfo)userData;
            if (openUIInfo == null) throw new GameFrameworkException("打开的界面信息为空.");

            var packageInfo = (OpenUIPackageInfo)uiPackageInfo;
            if (packageInfo == null) throw new GameFrameworkException("打开的界面信息数据为空.");


            // 检查是否是等待释放的界面，如果是，说明还没有被真正释放，则直接返回界面
            if (m_WaitReleaseSet.Contains(openUIInfo.SerialId))
            {
                m_WaitReleaseSet.Remove(openUIInfo.SerialId);
                ReferencePool.Release(openUIInfo);
                _iuiHelper.ReleaseUI(null);
                return GetUI(openUIInfo.SerialId);
            }

            // 从正在加载的字典中移除
            m_LoadingDict.Remove(openUIInfo.SerialId);

            // 实例化界面，此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
            var uiInstance       = _iuiHelper.InstantiateUI(uiPackageInfo);
            var uiInstanceObject = UIInstanceObject.Create(uiAssetName, uiInstance, _iuiHelper);
            m_InstancePool.Register(uiInstanceObject, true);

            // 打开界面
            var ui = InternalOpenUI(openUIInfo.SerialId,     uiAssetName,               openUIInfo.UIType,
                                    uiInstanceObject.Target, openUIInfo.PauseCoveredUI, true,
                                    duration,                openUIInfo.UserData,       openUIInfo.IsFullScreen);

            // 释放资源
            ReferencePool.Release(openUIInfo);
            ReferencePool.Release(packageInfo);
            return ui;
        }

        /// <summary>
        /// 加载界面资源失败回调。
        /// </summary>
        /// <param name="uiAssetName"></param>
        /// <param name="errorMessage"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        private IUIBase LoadAssetFailureCallback(string uiAssetName, string errorMessage, object userData)
        {
            var openUIInfo = (OpenUIInfo)userData;
            if (openUIInfo == null) throw new GameFrameworkException("打开的界面信息为空.");

            if (m_WaitReleaseSet.Contains(openUIInfo.SerialId))
            {
                m_WaitReleaseSet.Remove(openUIInfo.SerialId);
                return GetUI(openUIInfo.SerialId);
            }

            m_LoadingDict.Remove(openUIInfo.SerialId);
            var appendErrorMessage = Utility.Text.Format("加载界面资源失败, 界面资源名 '{0}', 错误信息 '{1}'.", uiAssetName, errorMessage);
            if (m_OpenUIFailureEventHandler == null) throw new GameFrameworkException(appendErrorMessage);

            var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, uiAssetName, openUIInfo.PauseCoveredUI, appendErrorMessage, openUIInfo.UserData);
            m_OpenUIFailureEventHandler(this, openUIFailureEventArgs);
            return GetUI(openUIInfo.SerialId);
        }
    }
}