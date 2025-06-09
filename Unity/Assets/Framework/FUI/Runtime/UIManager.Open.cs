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
        private EventHandler<OpenUISuccessEventArgs> m_OpenUIFormSuccessEventHandler;
        private EventHandler<OpenUIFailureEventArgs> m_OpenUIFormFailureEventHandler;

        /// <summary>
        /// 打开界面成功事件。
        /// </summary>
        public event EventHandler<OpenUISuccessEventArgs> OpenUIFormSuccess
        {
            add => m_OpenUIFormSuccessEventHandler += value;
            remove => m_OpenUIFormSuccessEventHandler -= value;
        }

        /// <summary>
        /// 打开界面失败事件。
        /// </summary>
        public event EventHandler<OpenUIFailureEventArgs> OpenUIFormFailure
        {
            add => m_OpenUIFormFailureEventHandler += value;
            remove => m_OpenUIFormFailureEventHandler -= value;
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="uiFormType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <returns>界面的序列编号。</returns>
        public async Task<IUIForm> OpenUIFormAsync(string uiFormAssetPath, Type uiFormType, bool pauseCoveredUIForm, object userData,
            bool isFullScreen = false)
        {
            GameFrameworkGuard.NotNull(m_AssetManager, nameof(m_AssetManager));
            GameFrameworkGuard.NotNull(m_UIFormHelper, nameof(m_UIFormHelper));
            GameFrameworkGuard.NotNull(uiFormType, nameof(uiFormType));

            return await InnerOpenUIFormAsync(uiFormAssetPath, uiFormType, pauseCoveredUIForm, userData, isFullScreen);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public Task<IUIForm> OpenUIAsync<T>(string uiFormAssetPath, bool pauseCoveredUIForm, object userData, bool isFullScreen = false,
            bool isMultiple = false) where T : class, IUIForm
        {
            return InnerOpenUIFormAsync(uiFormAssetPath, typeof(T), pauseCoveredUIForm, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="uiFormType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        public async Task<IUIForm> OpenUIAsync(string uiFormAssetPath, Type uiFormType, bool pauseCoveredUIForm, object userData,
            bool isFullScreen = false, bool isMultiple = false)
        {
            return await InnerOpenUIFormAsync(uiFormAssetPath, uiFormType, pauseCoveredUIForm, userData, isFullScreen, isMultiple);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetPath">界面所在路径</param>
        /// <param name="uiFormType">界面逻辑类型。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFullScreen">是否全屏</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        private async Task<IUIForm> InnerOpenUIFormAsync(string uiFormAssetPath, Type uiFormType, bool pauseCoveredUIForm, object userData,
            bool isFullScreen = false, bool isMultiple = false)
        {
            var uiFormAssetName = uiFormType.Name;
            UIFormInstanceObject uiFormInstanceObject = m_InstancePool.Spawn(uiFormAssetName);

            if (uiFormInstanceObject != null && isMultiple == false)
            {
                // 如果对象池存在
                return InternalOpenUIForm(-1, uiFormAssetName, uiFormType, uiFormInstanceObject.Target, pauseCoveredUIForm, false, 0f, userData,
                    isFullScreen);
            }

            var serialId = ++m_Serial;
            m_LoadingDict.Add(serialId, uiFormAssetName);
            var assetPath = PathHelper.Combine(uiFormAssetPath, uiFormAssetName);

            var lastIndexOfStart = uiFormAssetPath.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            var packageName = uiFormAssetPath.Substring(lastIndexOfStart + 1);

            // 检查UI包是否已经加载过
            var hasUIPackage = FuiPackage.HasPackage(packageName);

            OpenUIFormInfoData openUIFormInfoData =
                OpenUIFormInfoData.Create(serialId, packageName, uiFormAssetName, uiFormType, pauseCoveredUIForm, userData);
            OpenUIFormInfo openUIFormInfo = OpenUIFormInfo.Create(serialId, uiFormType, pauseCoveredUIForm, userData, isFullScreen);

            // 检查路径中是否包含Bundle目录，如果不包含则从Resources中加载
            if (assetPath.IndexOf(Utility.Asset.Path.BundlesDirectoryName, StringComparison.OrdinalIgnoreCase) < 0)
            {
                // 从Resources中加载
                if (!hasUIPackage)
                    FuiPackage.AddPackageSync(assetPath);
                return LoadAssetSuccessCallback(uiFormAssetName, openUIFormInfoData, 0, openUIFormInfo);
            }

            // 检查UI包是否已经加载过
            if (hasUIPackage)
            {
                // 如果UI 包存在则创建界面
                return LoadAssetSuccessCallback(uiFormAssetName, openUIFormInfoData, 1, openUIFormInfo);
            }

            if (packageName == uiFormAssetName)
            {
                // 如果UI资源名字和包名一致则直接加载
                await FuiPackage.AddPackageAsync(assetPath);
            }
            else
            {
                // 不一致则重新拼接路径
                string newPackagePath = PathHelper.Combine(uiFormAssetPath, packageName);
                await FuiPackage.AddPackageAsync(newPackagePath);
            }

            string newAssetPackagePath = assetPath;
            if (packageName != uiFormAssetName)
            {
                newAssetPackagePath = PathHelper.Combine(uiFormAssetPath, packageName);
            }

            newAssetPackagePath += "_fui";
            // 从包中加载
            var assetHandle = await m_AssetManager.LoadAssetAsync<UnityEngine.Object>(newAssetPackagePath);

            if (assetHandle.IsSucceed)
            {
                // 加载成功
                return LoadAssetSuccessCallback(uiFormAssetName, openUIFormInfoData, assetHandle.Progress, openUIFormInfo);
            }

            // UI包不存在
            return LoadAssetFailureCallback(uiFormAssetName, assetHandle.LastError, openUIFormInfo);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="serialId"></param>
        /// <param name="uiFormAssetName"></param>
        /// <param name="uiFormType"></param>
        /// <param name="uiFormInstance"></param>
        /// <param name="pauseCoveredUIForm"></param>
        /// <param name="isNewInstance"></param>
        /// <param name="duration"></param>
        /// <param name="userData"></param>
        /// <param name="isFullScreen"></param>
        /// <returns></returns>
        /// <exception cref="GameFrameworkException"></exception>
        private IUIForm InternalOpenUIForm(int serialId, string uiFormAssetName, Type uiFormType, object uiFormInstance, bool pauseCoveredUIForm,
            bool isNewInstance, float duration, object userData, bool isFullScreen)
        {
            try
            {
                // 使用界面辅助器创建界面实例
                IUIForm uiForm = m_UIFormHelper.CreateUIForm(uiFormInstance, uiFormType);
                if (uiForm == null)
                    throw new GameFrameworkException("不能从界面辅助器中创建界面实例.");

                // 界面初始化回调，设置UIForm的GObject
                void OnInitAction(IUIForm obj)
                {
                    if (obj is not FUI fui) return;
                    fui.SetGObject(uiFormInstance as GObject);
                }

                // 初始化界面
                var uiGroup = uiForm.UIGroup;
                uiForm.Init(serialId, uiFormAssetName, uiGroup, OnInitAction, pauseCoveredUIForm, isNewInstance, userData, isFullScreen);

                // 界面组中是否存在该界面，不存在则添加
                if (!uiGroup.InternalHasInstanceUI(uiFormAssetName, uiForm))
                {
                    uiGroup.AddUI(uiForm);
                }

                uiForm.OnOpen(userData); // 界面打开回调
                uiForm.UpdateLocalization(); // 更新本地化文本
                uiGroup.Refresh(); // 刷新界面组

                if (m_OpenUIFormSuccessEventHandler != null)
                {
                    OpenUISuccessEventArgs openUISuccessEventArgs = OpenUISuccessEventArgs.Create(uiForm, duration, userData);
                    m_OpenUIFormSuccessEventHandler(this, openUISuccessEventArgs);
                    // ReferencePool.Release(openUIFormSuccessEventArgs);
                }

                return uiForm;
            }
            catch (Exception exception)
            {
                if (m_OpenUIFormFailureEventHandler == null) throw;
                var openUIFormFailureEventArgs = OpenUIFailureEventArgs.Create(serialId, uiFormAssetName, pauseCoveredUIForm, exception.ToString(), userData);
                m_OpenUIFormFailureEventHandler(this, openUIFormFailureEventArgs);
                return GetUI(openUIFormFailureEventArgs.SerialId);
            }
        }
        /// <summary>
        /// 设置界面实例是否被加锁。
        /// </summary>
        /// <param name="uiFormInstance">要设置是否被加锁的界面实例。</param>
        /// <param name="locked">界面实例是否被加锁。</param>
        public void SetUIInstanceLocked(object uiFormInstance, bool locked)
        {
            GameFrameworkGuard.NotNull(uiFormInstance, nameof(uiFormInstance));
            m_InstancePool.SetLocked(uiFormInstance, locked);
        }

        /// <summary>
        /// 设置界面实例的优先级。
        /// </summary>
        /// <param name="uiFormInstance">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIFormInstancePriority(object uiFormInstance, int priority)
        {
            GameFrameworkGuard.NotNull(uiFormInstance, nameof(uiFormInstance));
            m_InstancePool.SetPriority(uiFormInstance, priority);
        }

        private IUIForm LoadAssetSuccessCallback(string uiFormAssetName, object uiFormAsset, float duration, object userData)
        {
            OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }

            var openUIFormInfoData = (OpenUIFormInfoData)uiFormAsset;
            if (openUIFormInfoData == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }


            if (m_WaitReleaseSet.Contains(openUIFormInfo.SerialId))
            {
                m_WaitReleaseSet.Remove(openUIFormInfo.SerialId);
                ReferencePool.Release(openUIFormInfo);
                m_UIFormHelper.ReleaseUIForm(null);
                return GetUI(openUIFormInfo.SerialId);
            }

            m_LoadingDict.Remove(openUIFormInfo.SerialId);
            UIFormInstanceObject uiFormInstanceObject =
                UIFormInstanceObject.Create(uiFormAssetName, uiFormAsset, m_UIFormHelper.InstantiateUIForm(uiFormAsset), m_UIFormHelper);
            m_InstancePool.Register(uiFormInstanceObject, true);

            var uiForm = InternalOpenUIForm(openUIFormInfo.SerialId, uiFormAssetName, openUIFormInfo.FormType, uiFormInstanceObject.Target,
                openUIFormInfo.PauseCoveredUIForm, true, duration, openUIFormInfo.UserData, openUIFormInfo.IsFullScreen);
            ReferencePool.Release(openUIFormInfo);
            ReferencePool.Release(openUIFormInfoData);
            return uiForm;
        }

        private IUIForm LoadAssetFailureCallback(string uiFormAssetName, string errorMessage, object userData)
        {
            OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new GameFrameworkException("Open UI form info is invalid.");
            }

            if (m_WaitReleaseSet.Contains(openUIFormInfo.SerialId))
            {
                m_WaitReleaseSet.Remove(openUIFormInfo.SerialId);
                return GetUI(openUIFormInfo.SerialId);
            }

            m_LoadingDict.Remove(openUIFormInfo.SerialId);
            string appendErrorMessage =
                Utility.Text.Format("Load UI form failure, asset name '{0}', error message '{2}'.", uiFormAssetName, errorMessage);
            if (m_OpenUIFormFailureEventHandler != null)
            {
                OpenUIFailureEventArgs openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIFormInfo.SerialId, uiFormAssetName,
                    openUIFormInfo.PauseCoveredUIForm, appendErrorMessage, openUIFormInfo.UserData);
                m_OpenUIFormFailureEventHandler(this, openUIFailureEventArgs);
                return GetUI(openUIFormInfo.SerialId);
            }

            throw new GameFrameworkException(appendErrorMessage);
        }
    }
}