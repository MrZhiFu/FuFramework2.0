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
        public Task<ViewBase> OpenUIAsync<T>(string uiAssetPath, bool pauseCoveredUI, object userData, bool isFullScreen = false, bool isMultiple = false) where T : ViewBase
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
        public async Task<ViewBase> OpenUIAsync(string uiAssetPath, Type uiType, bool pauseCoveredUI, object userData, bool isFullScreen = false, bool isMultiple = false)
        {
            return await InnerOpenUIAsync(uiAssetPath, uiType, pauseCoveredUI, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="packagePath">界面所在包路径，如：Assets/Bundles/UI/Login</param>
        /// <param name="uiType">界面逻辑类型。如：Login</param>
        /// <param name="pauseCoveredUI">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        private async Task<ViewBase> InnerOpenUIAsync(string packagePath, Type uiType, bool pauseCoveredUI, object userData, bool isFullScreen = false, bool isMultiple = false)
        {
            var uiName = uiType.Name;

            // 如：packagePath = "Assets/Bundles/UI/Login"，则 packageName = "Login"
            var lastIndexOfStart = packagePath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            var packageName      = packagePath.Substring(lastIndexOfStart + 1);

            OpenUIInfo openUIInfo;

            // 获取界面实例对象，如果对象池中存在并且不允许使用多个实例，则直接使用对象池中的对象
            UIInstanceObject uiInstanceObject = m_InstancePool.Spawn(uiName);
            if (uiInstanceObject != null && isMultiple == false)
            {
                openUIInfo = OpenUIInfo.Create(-1, uiType, pauseCoveredUI, userData, isFullScreen, packageName);
                return InternalOpenUI(openUIInfo, (GObject)uiInstanceObject.Target, false, 0);
            }

            var serialId = ++m_Serial;
            m_LoadingDict.Add(serialId, uiName);

            // 创建一个打开界面界面时的信息对象，用于记录打开界面的信息
            openUIInfo = OpenUIInfo.Create(serialId, uiType, pauseCoveredUI, userData, isFullScreen, packageName);

            // 如："Assets/Bundles/UI/Login/Login"，第一个Login是包名，第二个Login是界面描述文件资源名
            var descPath = PathHelper.Combine(packagePath, packageName);

            // UI包是否已经加载过
            var hasUIPackage = FuiPackageManager.Instance.HasPackage(packageName);

            // 检查UI包是否已经加载过，是则直接通过回调创建界面
            if (hasUIPackage)
                return LoadAssetSuccessCallback(openUIInfo, 0);

            // 检查路径中是否包含Bundle目录，否则从Resources中同步加载
            if (descPath.IndexOf(Utility.Asset.Path.BundlesDirectoryName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                // 从Resources中加载
                FuiPackageManager.Instance.AddPackageSync(descPath);
                return LoadAssetSuccessCallback(openUIInfo, 0);
            }

            // 等待异步加载UI资源包完成 
            await FuiPackageManager.Instance.AddPackageAsync(descPath);

            // 等待异步加载完成加载描述文件完成
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
        /// <param name="openUIInfo"></param>
        /// <param name="uiInstance"></param>
        /// <param name="isNewInstance"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private ViewBase InternalOpenUI(OpenUIInfo openUIInfo, GObject uiInstance, bool isNewInstance, float duration)
        {
            try
            {
                // 使用界面辅助器创建界面实例
                // 1.将传入的UI界面实例uiInstance加上UI界面逻辑组件uiType，
                // 2.将uiInstance作为一个子节点添加到UI界面组的显示对象下。
                ViewBase viewBase = FuiHelper.CreateUI(uiInstance, openUIInfo.UIType);
                if (viewBase == null) throw new GameFrameworkException("不能从界面辅助器中创建界面实例.");

                // 初始化界面
                var uiGroup = viewBase.UIGroup;
                viewBase.Init(openUIInfo.SerialId, openUIInfo.UIType.Name, uiGroup, uiInstance, openUIInfo.IsPauseBeCoveredUI, isNewInstance, openUIInfo.UserData, openUIInfo.IsFullScreen);

                // 界面组中是否存在该界面，不存在则添加
                if (!uiGroup.InternalHasInstanceUI(openUIInfo.UIType.Name, viewBase))
                {
                    uiGroup.AddUI(viewBase);
                }

                viewBase.OnOpen(openUIInfo.UserData); // 界面打开回调
                viewBase.UpdateLocalization();        // 更新本地化文本
                uiGroup.Refresh();                    // 刷新界面组

                // 广播界面打开成功事件
                var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(viewBase, duration, openUIInfo.UserData);
                m_EventComponent.Fire(this, openUISuccessEventArgs);

                return viewBase;
            }
            catch (Exception exception)
            {
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, openUIInfo.UIType.Name, openUIInfo.IsPauseBeCoveredUI, exception.ToString(), openUIInfo.UserData);
                Log.Warning($"打开UI界面失败, 资源名称 '{openUIFailureEventArgs.UIAssetName}',  是否暂停被覆盖的界面 '{openUIFailureEventArgs.PauseCoveredUI}', error message '{openUIFailureEventArgs.ErrorMessage}'.");
                m_EventComponent.Fire(this, openUIFailureEventArgs);
                return GetUI(openUIFailureEventArgs.SerialId);
            }
        }

        /// <summary>
        /// 设置界面实例是否被加锁。
        /// </summary>
        /// <param name="uiInstance">要设置是否被加锁的界面实例。</param>
        /// <param name="locked">界面实例是否被加锁。</param>
        public void SetUIInstanceLocked(object uiInstance, bool locked) => m_InstancePool.SetLocked(uiInstance, locked);

        /// <summary>
        /// 设置界面实例的优先级。
        /// </summary>
        /// <param name="uiInstance">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIInstancePriority(object uiInstance, int priority) => m_InstancePool.SetPriority(uiInstance, priority);

        /// <summary>
        /// 加载界面资源成功回调。
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="openUIInfo"></param>
        /// <returns></returns>
        private ViewBase LoadAssetSuccessCallback(OpenUIInfo openUIInfo, float duration)
        {
            if (openUIInfo == null) throw new GameFrameworkException("打开的界面信息为空.");

            int    serialId    = openUIInfo.SerialId;
            string uiName      = openUIInfo.UIType.Name;
            string packageName = openUIInfo.PackageName;

            // 检查是否是等待释放的界面，如果是，说明还没有被真正释放，则直接返回界面
            if (m_WaitReleaseSet.Contains(serialId))
            {
                m_WaitReleaseSet.Remove(serialId);
                ReferencePool.Release(openUIInfo);
                FuiHelper.ReleaseUI(null);
                return GetUI(serialId);
            }

            // 从正在加载的字典中移除
            m_LoadingDict.Remove(serialId);

            // 实例化界面，此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
            var uiInstance       = FuiHelper.InstantiateUI(packageName, uiName);
            var uiInstanceObject = UIInstanceObject.Create(uiName, uiInstance);
            m_InstancePool.Register(uiInstanceObject, true);

            // 打开界面
            var ui = InternalOpenUI(openUIInfo, (GObject)uiInstanceObject.Target, true, duration);

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
            var appendErrorMessage     = Utility.Text.Format("加载界面资源失败, 界面资源名 '{0}', 错误信息 '{1}'.", openUIInfo.UIType.Name, errorMessage);
            var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, openUIInfo.UIType.Name, openUIInfo.IsPauseBeCoveredUI, appendErrorMessage, openUIInfo.UserData);
            m_EventComponent.Fire(this, openUIFailureEventArgs);
            return GetUI(openUIInfo.SerialId);
        }
    }
}