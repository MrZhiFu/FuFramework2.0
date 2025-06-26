using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器.打开界面(打开一个界面只支持异步方式)
    /// </summary>
    public sealed partial class UIManager
    {
        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData"></param>
        /// <param name="isFromResources"></param>
        /// <param name="isMultiple"></param>
        /// <typeparam name="T"></typeparam>
        public void OpenUI<T>(object userData = null, bool isFromResources = false, bool isMultiple = false) where T : ViewBase
        {
            OpenUIAsync<T>(userData, isFromResources, isMultiple).Forget();
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFromResources">是否从Resources中加载。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async UniTask<T> OpenUIAsync<T>(object userData = null, bool isFromResources = false, bool isMultiple = false) where T : ViewBase
        {
            // 通过反射获取界面的包名
            string packageName;
            var    packageNameField = typeof(T).GetField("UIPackageName", BindingFlags.Public | BindingFlags.Static);

            if (packageNameField != null)
                packageName = (string)packageNameField.GetValue(null);
            else
                throw new GameFrameworkException($"界面类型 {typeof(T).Name} 中没有包含 UIPackageName 常量字段.");
            
            return await InnerOpenUIAsync(packageName, typeof(T), userData, isFromResources, isMultiple) as T;
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="packageName">包名。</param>
        /// <param name="uiType">界面逻辑类型。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFromResources">是否从Resources中加载。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        public async UniTask<ViewBase> OpenUIAsync(string packageName, Type uiType, object userData = null, bool isFromResources = false, bool isMultiple = false)
        {
            return await InnerOpenUIAsync(packageName, uiType, userData, isFromResources, isMultiple);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="packageName">包名。</param>
        /// <param name="uiType">界面逻辑类型。如：Login</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isFromResources">是否从Resources中加载。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        private async UniTask<ViewBase> InnerOpenUIAsync(string packageName, Type uiType, object userData, bool isFromResources = false, bool isMultiple = false)
        {
            var uiName = uiType.Name;

            OpenUIInfo openUIInfo;

            // 获取界面实例对象，如果对象池中存在并且不允许使用多个实例，则直接使用对象池中的对象
            UIInstanceObject uiInstanceObject = m_InstancePool.Spawn(uiName);
            if (uiInstanceObject != null && isMultiple == false)
            {
                openUIInfo = OpenUIInfo.Create(m_SerialId, uiType, userData, packageName);
                return InternalOpenUI(openUIInfo, uiInstanceObject.Target as GComponent, false, 0);
            }

            m_LoadingDict.Add(m_SerialId, uiName);

            // 创建一个打开界面界面时的信息对象，用于记录打开界面的信息
            openUIInfo = OpenUIInfo.Create(m_SerialId, uiType, userData, packageName);

            // UI包已经加载过，则直接通过回调创建界面
            if (FUIPackageMgr.Instance.HasPackage(packageName))
                return LoadAssetSuccessCallback(openUIInfo, 0);

            await FUIPackageMgr.Instance.AddPackageAsync(packageName, isFromResources);
            return LoadAssetSuccessCallback(openUIInfo, 0);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="openUIInfo"></param>
        /// <param name="uiView"></param>
        /// <param name="isNewInstance"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private ViewBase InternalOpenUI(OpenUIInfo openUIInfo, GComponent uiView, bool isNewInstance, float duration)
        {
            try
            {
                // 使用界面辅助器创建界面实例
                // 1.将传入的UI界面实例uiView加上UI界面逻辑组件uiType，
                // 2.将uiView作为一个子节点添加到UI界面组的显示对象下。
                ViewBase viewBase = FuiHelper.CreateUI(uiView, openUIInfo.UIType);
                if (viewBase == null) throw new GameFrameworkException("不能从界面辅助器中创建界面实例.");

                // 创建界面实例对象并注册到对象池中
                var uiInstanceObject = UIInstanceObject.Create(openUIInfo.UIType.Name, uiView, viewBase);
                m_InstancePool.Register(uiInstanceObject, true);

                // 初始化界面
                var uiGroup = viewBase.UIGroup;
                viewBase.Init(openUIInfo.SerialId, openUIInfo.UIType.Name, uiView, isNewInstance, openUIInfo.UserData);

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

                m_SerialId++;
                return viewBase;
            }
            catch (Exception exception)
            {
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, openUIInfo.UIType.Name, openUIInfo.UserData);
                m_EventComponent.Fire(this, openUIFailureEventArgs);
                Log.Error($"打开UI界面失败, 资源名称 '{openUIInfo.UIType.Name}', 错误信息 '{exception}'.");
                return GetUI(openUIFailureEventArgs.SerialId);
            }
        }

        /// <summary>
        /// 设置界面实例是否被加锁。
        /// </summary>
        /// <param name="uiView">要设置是否被加锁的界面实例。</param>
        /// <param name="locked">界面实例是否被加锁。</param>
        public void SetUIInstanceLocked(object uiView, bool locked) => m_InstancePool.SetLocked(uiView, locked);

        /// <summary>
        /// 设置界面实例的优先级。
        /// </summary>
        /// <param name="uiView">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIInstancePriority(object uiView, int priority) => m_InstancePool.SetPriority(uiView, priority);

        /// <summary>
        /// 加载界面资源成功回调。
        /// </summary>
        /// <param name="openUIInfo">打开时的界面参数封装</param>
        /// <param name="duration">打开的持续进度</param>
        /// <returns></returns>
        private ViewBase LoadAssetSuccessCallback(OpenUIInfo openUIInfo, float duration)
        {
            if (openUIInfo == null) throw new GameFrameworkException("打开的界面信息为空.");

            var serialId    = openUIInfo.SerialId;
            var uiName      = openUIInfo.UIType.Name;
            var packageName = openUIInfo.PackageName;

            // 检查是否是等待释放的界面，如果是，说明还没有被真正释放，则直接返回界面
            if (m_LoadingInCloseSet.Contains(serialId))
            {
                m_LoadingInCloseSet.Remove(serialId);
                ReferencePool.Release(openUIInfo);
                return GetUI(serialId);
            }

            // 从正在加载的字典中移除
            m_LoadingDict.Remove(serialId);

            // 实例化界面，此时只是使用FUI创建了一个界面，并没有将其加入到UI界面组的显示对象下。
            var uiView = UIPackage.CreateObject(packageName, uiName) as GComponent;

            // 打开界面
            var ui = InternalOpenUI(openUIInfo, uiView, true, duration);

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

            if (m_LoadingInCloseSet.Contains(openUIInfo.SerialId))
            {
                m_LoadingInCloseSet.Remove(openUIInfo.SerialId);
                return GetUI(openUIInfo.SerialId);
            }

            m_LoadingDict.Remove(openUIInfo.SerialId);
            var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, openUIInfo.UIType.Name, openUIInfo.UserData);
            m_EventComponent.Fire(this, openUIFailureEventArgs);
            Log.Error("加载界面资源失败, 界面资源名 '{0}', 错误信息 '{1}'.", openUIInfo.UIType.Name, errorMessage);
            return GetUI(openUIInfo.SerialId);
        }
    }
}