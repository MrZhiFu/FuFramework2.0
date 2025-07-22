using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
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
        public void OpenUI<T>(object userData = null, bool isMultiple = false) where T : ViewBase, new()
        {
            OpenUIAsync<T>(userData, isMultiple).Forget();
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否允许多个界面实例</param>
        /// <returns>界面的序列编号。</returns>
        public async UniTask<T> OpenUIAsync<T>(object userData = null, bool isMultiple = false) where T : ViewBase, new()
        {
            return await _OpenUIAsync<T>(userData, isMultiple);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="isMultiple">是否允许多个界面实例</param>
        /// <returns></returns>
        private async UniTask<T> _OpenUIAsync<T>(object userData = null, bool isMultiple = false) where T : ViewBase, new()
        {
            var uiName = typeof(T).Name;
            if (!m_LoadingDict.TryAdd(m_SerialId, uiName))
            {
                Log.Warning($"[UIManager]已经有序号为 {m_SerialId} 的界面正在加载.");
                return null;
            }

            T view;

            // 获取界面实例对象，如果对象池中存在并且不允许使用多个实例，则直接使用对象池中的对象
            UIInstanceObject uiInstanceObject = m_InstancePool.Spawn(uiName);
            if (uiInstanceObject != null && isMultiple == false)
            {
                view = uiInstanceObject.Target as T;
                return CreateUIView(view, false, userData);
            }

            // 创建界面实例对象
            view = new T();
            uiInstanceObject = UIInstanceObject.Create(view.UIName, view);
            m_InstancePool.Register(uiInstanceObject, true);
                
            // UI包已经加载过，则直接通过回调创建界面
            if (FuiPackageManager.Instance.HasPackage(view.PackageName))
            {
                // 从正在加载的字典中移除，并创建FUI界面
                m_LoadingDict.Remove(m_SerialId);
                return CreateUIView(view, true, userData);
            }

            // UI包没有加载过，则等待加载UI包，加载完成后再创建界面
            await FuiPackageManager.Instance.AddPackageAsync(view.PackageName);
            
            // 从正在加载的字典中移除，并创建FUI界面
            m_LoadingDict.Remove(m_SerialId);
            return CreateUIView(view, true, userData);
        }

        /// <summary>
        /// 创建FUI界面
        /// </summary>
        /// <param name="view">界面实例。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns></returns>
        private T CreateUIView<T> (T view, bool isNewInstance, object userData = null) where T : ViewBase, new()
        {
            try
            {
                if (view == null) throw new GameFrameworkException($"[UIManager]创建界面实例{typeof(T).Name}失败.");

                // 创建FUI界面。
                var uiView = UIPackage.CreateObject(view.PackageName, view.UIName) as GComponent;
                
                // 初始化界面
                view.Init(m_SerialId, uiView, isNewInstance, userData);
                
                // FUI界面加入界面组
                var uiGroup = view.UIGroup;
                uiGroup.AddChild(view.UIView);
                if (!uiGroup.InternalHasUI(view.UIName, view))
                {
                    uiGroup.AddUI(view);
                }
                
                view._OnOpen();            // 界面打开回调
                view.UpdateLocalization(); // 更新本地化文本
                uiGroup.Refresh();         // 刷新界面组

                // 广播界面打开成功事件
                var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(view, userData);
                m_EventComponent.Fire(this, openUISuccessEventArgs);

                m_SerialId++;
                return view;
            }
            catch (Exception exception)
            {
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(m_SerialId, typeof(T).Name, userData);
                m_EventComponent.Fire(this, openUIFailureEventArgs);
                Log.Error($"[UIManager]打开UI界面失败, 资源名称 '{typeof(T).Name}', 错误信息 '{exception}'.");
                return GetUI(openUIFailureEventArgs.SerialId) as T;
            }
        }

        /// <summary>
        /// 设置界面实例是否加锁，如果加锁，则不会被释放(销毁)。
        /// </summary>
        /// <param name="uiView">要设置是否加锁的界面实例。</param>
        /// <param name="locked">界面实例是否加锁。</param>
        public void SetUILocked(object uiView, bool locked) => m_InstancePool.SetLocked(uiView, locked);

        /// <summary>
        /// 设置界面实例对象的优先级。优先级小的实例会优先被释放。
        /// </summary>
        /// <param name="uiView">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIPriority(object uiView, int priority) => m_InstancePool.SetPriority(uiView, priority);
    }
}