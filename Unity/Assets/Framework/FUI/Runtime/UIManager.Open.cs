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
        /// <param name="isMultiple"></param>
        /// <typeparam name="T"></typeparam>
        public void OpenUI<T>(object userData = null, bool isMultiple = false) where T : ViewBase
        {
            OpenUIAsync<T>(userData, isMultiple).Forget();
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns>界面的序列编号。</returns>
        public async UniTask<T> OpenUIAsync<T>(object userData = null, bool isMultiple = false) where T : ViewBase
        {
            // 通过反射获取界面的包名
            string packageName;
            var    packageNameField = typeof(T).GetField("UIPackageName", BindingFlags.Public | BindingFlags.Static);

            if (packageNameField != null)
                packageName = (string)packageNameField.GetValue(null);
            else
                throw new GameFrameworkException($"[UIManager]界面类型 {typeof(T).Name} 中没有包含 UIPackageName 常量字段.");

            return await InnerOpenUIAsync(packageName, typeof(T), userData, isMultiple) as T;
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="packageName">包名。</param>
        /// <param name="uiType">界面逻辑类型。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        public async UniTask<ViewBase> OpenUIAsync(string packageName, Type uiType, object userData = null, bool isMultiple = false)
        {
            return await InnerOpenUIAsync(packageName, uiType, userData, isMultiple);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="packageName">包名。</param>
        /// <param name="uiType">界面逻辑类型。如：Login</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否创建新界面</param>
        /// <returns></returns>
        private async UniTask<ViewBase> InnerOpenUIAsync(string packageName, Type uiType, object userData, bool isMultiple = false)
        {
            var uiName = uiType.Name;

            OpenUIInfo openUIInfo;

            // 获取界面实例对象，如果对象池中存在并且不允许使用多个实例，则直接使用对象池中的对象
            UIInstanceObject uiInstanceObject = m_InstancePool.Spawn(uiName);
            if (uiInstanceObject != null && isMultiple == false)
            {
                openUIInfo = OpenUIInfo.Create(m_SerialId, packageName, uiType, userData);
                return InternalOpenUI(openUIInfo, uiInstanceObject.Target as ViewBase);
            }

            if (!m_LoadingDict.TryAdd(m_SerialId, uiName))
            {
                Log.Warning($"[UIManager]已经有序号为 {m_SerialId} 的界面正在加载.");
                return null;
            }

            // 创建一个打开界面界面时的信息对象，用于记录打开界面的信息
            openUIInfo = OpenUIInfo.Create(m_SerialId, packageName, uiType, userData);

            // UI包已经加载过，则直接通过回调创建界面
            if (FuiPackageMgr.Instance.HasPackage(packageName))
                return LoadAssetSuccessCallback(openUIInfo, 0);

            // UI包没有加载过，则加载UI包
            await FuiPackageMgr.Instance.AddPackageAsync(packageName);
            return LoadAssetSuccessCallback(openUIInfo, 0);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="openUIInfo"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        private ViewBase InternalOpenUI(OpenUIInfo openUIInfo, ViewBase view)
        {
            try
            {
                var isNewInstance = view == null;

                // 创建界面逻辑实例
                if (view == null)
                {
                    view = Activator.CreateInstance(openUIInfo.UIType) as ViewBase;

                    // 创建界面实例对象并注册到对象池中
                    var uiInstanceObject = UIInstanceObject.Create(openUIInfo.UIType.Name, view);
                    m_InstancePool.Register(uiInstanceObject, true);
                }

                if (view == null) throw new GameFrameworkException($"[UIManager]创建界面实例{openUIInfo.UIType.Name}失败.");

                // 创建FUI界面。
                var uiView = UIPackage.CreateObject(openUIInfo.PackageName, openUIInfo.UIType.Name) as GComponent;
                
                // 初始化界面
                view.Init(openUIInfo.SerialId, openUIInfo.PackageName, openUIInfo.UIType.Name, uiView, isNewInstance, openUIInfo.UserData);
                
                // FUI界面加入界面组
                var uiGroup = view.UIGroup;
                uiGroup.AddChild(view.UIView);
                if (!uiGroup.InternalHasUI(openUIInfo.UIType.Name, view))
                {
                    uiGroup.AddUI(view);
                }
                
                view._OnOpen(openUIInfo.UserData); // 界面打开回调
                view.UpdateLocalization();        // 更新本地化文本
                uiGroup.Refresh();                // 刷新界面组

                // 广播界面打开成功事件
                var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(view, openUIInfo.UserData);
                m_EventComponent.Fire(this, openUISuccessEventArgs);

                m_SerialId++;
                return view;
            }
            catch (Exception exception)
            {
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(openUIInfo.SerialId, openUIInfo.UIType.Name, openUIInfo.UserData);
                m_EventComponent.Fire(this, openUIFailureEventArgs);
                Log.Error($"[UIManager]打开UI界面失败, 资源名称 '{openUIInfo.UIType.Name}', 错误信息 '{exception}'.");
                return GetUI(openUIFailureEventArgs.SerialId);
            }
        }

        /// <summary>
        /// 加载界面资源成功回调。
        /// </summary>
        /// <param name="openUIInfo">打开时的界面参数封装</param>
        /// <param name="duration">打开的持续进度</param>
        /// <returns></returns>
        private ViewBase LoadAssetSuccessCallback(OpenUIInfo openUIInfo, float duration)
        {
            if (openUIInfo == null) throw new GameFrameworkException("[UIManager]打开的界面信息为空.");

            var serialId    = openUIInfo.SerialId;

            // 检查是否是等待释放的界面，如果是，说明还没有被真正释放，则直接返回界面
            if (m_LoadingInCloseSet.Contains(serialId))
            {
                m_LoadingInCloseSet.Remove(serialId);
                ReferencePool.Release(openUIInfo);
                return GetUI(serialId);
            }

            // 从正在加载的字典中移除
            m_LoadingDict.Remove(serialId);

            // 打开界面
            var ui = InternalOpenUI(openUIInfo, null);

            // 释放资源
            ReferencePool.Release(openUIInfo);
            return ui;
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
    }
}