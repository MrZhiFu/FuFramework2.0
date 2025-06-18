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
    public sealed partial class UIManager
    {
        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public Task<ViewBase> OpenUIAsync<T>(string uiAssetPath, bool pauseCoveredUI, object userData, bool isFullScreen = false,
            bool isMultiple = false) where T : ViewBase
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
        public async Task<ViewBase> OpenUIAsync(string uiAssetPath, Type uiType, bool pauseCoveredUI, object userData,
            bool isFullScreen = false, bool isMultiple = false)
        {
            return await InnerOpenUIAsync(uiAssetPath, uiType, pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="packagePath">界面所在包路径，如：Assets/Bundles/UI/Login</param>
        /// <param name="uiType">界面逻辑类型。如：Login</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        private async Task<ViewBase> InnerOpenUIAsync(string packagePath, Type uiType, bool pauseCoveredUI, object userData,
            bool isFullScreen = false, bool isMultiple = false)
        {
            var uiName = uiType.Name;

            // 获取界面实例对象，如果对象池中存在并且不允许使用多个实例，则直接使用对象池中的对象
            UIInstanceObject uiInstanceObject = m_InstancePool.Spawn(uiName);
            if (uiInstanceObject != null && isMultiple == false)
            {
                return InternalOpenUI(-1, uiType, uiInstanceObject.Target, pauseCoveredUI, false, 0f, userData, isFullScreen);
            }

            var serialId = ++m_Serial;
            m_LoadingDict.Add(serialId, uiName);

            // 如：packagePath = "Assets/Bundles/UI/Login"，则 packageName = "Login"
            var lastIndexOfStart = packagePath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            var packageName = packagePath.Substring(lastIndexOfStart + 1);

            var openUIInfo = OpenUIInfo.Create(serialId, uiType, pauseCoveredUI, userData, isFullScreen, packageName);

            // 如："Assets/Bundles/UI/Login/Login"，第一个Login是包名，第二个Login是界面描述文件资源名
            var descPath = PathHelper.Combine(packagePath, packageName);

            // UI包是否已经加载过
            var hasUIPackage = FuiPackage.HasPackage(packageName);

            // 检查路径中是否包含Bundle目录，如果不包含则从Resources中同步加载
            if (descPath.IndexOf(Utility.Asset.Path.BundlesDirectoryName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                // 从Resources中加载
                if (!hasUIPackage) FuiPackage.AddPackageSync(descPath);
                return LoadAssetSuccessCallback(openUIInfo, 0 );
            }

            // 检查UI包是否已经加载过，则直接通过回调创建界面
            if (hasUIPackage)
                return LoadAssetSuccessCallback(openUIInfo, 0 );

            await FuiPackage.AddPackageAsync(descPath);

            // 加载描述文件资源
            var assetHandle = await m_AssetManager.LoadAssetAsync<UnityEngine.Object>($"{descPath}_fui");

            // 加载成功
            if (assetHandle.IsSucceed)
                return LoadAssetSuccessCallback(openUIInfo, assetHandle.Progress);

            // 加载失败
            return LoadAssetFailureCallback(openUIInfo, assetHandle.LastError);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="serialId"></param>
        /// <param name="uiType"></param>
        /// <param name="uiInstance"></param>
        /// <param name="pauseCoveredUI"></param>
        /// <param name="isNewInstance"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        /// <param name="isFullScreen"></param>
        /// <returns></returns>
        /// <exception cref="GameFrameworkException"></exception>
        private ViewBase InternalOpenUI(int serialId, Type uiType, object uiInstance, bool pauseCoveredUI,
            bool isNewInstance, float duration, object userData, bool isFullScreen)
        {
            try
            {
                // 使用界面辅助器创建界面实例
                // 1.将传入的UI界面实例uiInstance加上UI界面逻辑组件uiType，
                // 2.将uiInstance作为一个子节点添加到UI界面组的显示对象下。
                ViewBase viewBase = FuiHelper.CreateUI(uiInstance, uiType);
                if (viewBase == null) throw new GameFrameworkException("不能从界面辅助器中创建界面实例.");

                // 界面初始化回调，设置UI的GObject
                void OnInitAction(ViewBase obj)
                {
                    if (obj is not FUI fui) return;
                    fui.SetGObject(uiInstance as GObject);
                }

                // 初始化界面
                var uiGroup = viewBase.UIGroup;
                viewBase.Init(serialId, uiType.Name, uiGroup, OnInitAction, pauseCoveredUI, isNewInstance, userData, isFullScreen);

                // 界面组中是否存在该界面，不存在则添加
                if (!uiGroup.InternalHasInstanceUI(uiType.Name, viewBase))
                {
                    uiGroup.AddUI(viewBase);
                }

                viewBase.OnOpen(userData); // 界面打开回调
                viewBase.UpdateLocalization(); // 更新本地化文本
                uiGroup.Refresh(); // 刷新界面组

                var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(viewBase, duration, userData);
                m_EventComponent.Fire(this, openUISuccessEventArgs);

                return viewBase;
            }
            catch (Exception exception)
            {
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(serialId, uiType.Name, pauseCoveredUI, exception.ToString(), userData);
                Log.Warning(
                    $"打开UI界面失败, 资源名称 '{openUIFailureEventArgs.UIAssetName}',  是否暂停被覆盖的界面 '{openUIFailureEventArgs.PauseCoveredUI}', error message '{openUIFailureEventArgs.ErrorMessage}'.");
                m_EventComponent.Fire(this, openUIFailureEventArgs);
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
        /// <param name="duration"></param>
        /// <param name="openUIInfo"></param>
        /// <returns></returns>
        private ViewBase LoadAssetSuccessCallback(OpenUIInfo openUIInfo, float duration)
        {
            if (openUIInfo == null) throw new GameFrameworkException("打开的界面信息为空.");

            // 检查是否是等待释放的界面，如果是，说明还没有被真正释放，则直接返回界面
            if (m_WaitReleaseSet.Contains(openUIInfo.SerialId))
            {
                m_WaitReleaseSet.Remove(openUIInfo.SerialId);
                ReferencePool.Release(openUIInfo);
                FuiHelper.ReleaseUI(null);
                return GetUI(openUIInfo.SerialId);
            }

            // 从正在加载的字典中移除
            m_LoadingDict.Remove(openUIInfo.SerialId);

            // 实例化界面，此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
            var uiInstance = FuiHelper.InstantiateUI(openUIInfo.PackageName, openUIInfo.UIType.Name);
            var uiInstanceObject = UIInstanceObject.Create(openUIInfo.UIType.Name, uiInstance);
            m_InstancePool.Register(uiInstanceObject, true);

            // 打开界面
            var ui = InternalOpenUI(openUIInfo.SerialId, openUIInfo.UIType,
                uiInstanceObject.Target, openUIInfo.IsPauseBeCoveredUI, true,
                duration, openUIInfo.UserData, openUIInfo.IsFullScreen);

            // 释放资源
            ReferencePool.Release(openUIInfo);
            return ui;
        }

        /// <summary>
        /// 加载界面资源失败回调。
        /// </summary>
        /// <param name="openUIInfo"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private ViewBase LoadAssetFailureCallback(OpenUIInfo openUIInfo, string errorMessage)
        {
            if (openUIInfo == null) throw new GameFrameworkException("打开的界面信息为空.");

            if (m_WaitReleaseSet.Contains(openUIInfo.SerialId))
            {
                m_WaitReleaseSet.Remove(openUIInfo.SerialId);
                return GetUI(openUIInfo.SerialId);
            }

            m_LoadingDict.Remove(openUIInfo.SerialId);
            var appendErrorMessage = Utility.Text.Format("加载界面资源失败, 界面资源名 '{0}', 错误信息 '{1}'.", openUIInfo.UIType.Name, errorMessage);
            var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, openUIInfo.UIType.Name, openUIInfo.IsPauseBeCoveredUI, appendErrorMessage, openUIInfo.UserData);
            m_EventComponent.Fire(this, openUIFailureEventArgs);
            return GetUI(openUIInfo.SerialId);
        }
    }
}