using System;
using FairyGUI;
using Cysharp.Threading.Tasks;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI管理模块分部类之一。
    /// 目标：用于打开UI界面。
    /// 功能：
    ///     1. 异步打开UI界面。
    ///     2. 创建FairyUI界面。
    ///     3. 设置界面实例是否加锁，如果加锁，则不会被释放(销毁)。
    ///     4. 设置界面实例对象的优先级。优先级小的实例会优先被释放。
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T">界面类型。</typeparam>
        public void Open<T>(object userData = null) where T : WinBase, new()
        {
            _OpenAsync<T>(userData).Forget();
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面实例。</returns>
        public async UniTask<T> OpenAsync<T>(object userData = null) where T : WinBase, new()
        {
            return await _OpenAsync<T>(userData);
        }

        /// <summary>
        /// 打开界面。(内部使用)
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面实例。</returns>
        private async UniTask<T> _OpenAsync<T>(object userData = null) where T : WinBase, new()
        {
            var uiName = typeof(T).Name;

            // 检查是否已经在加载中
            if (IsLoading(uiName))
            {
                FuLogger.LogWarning($"[UIModule] 界面 {uiName} 已经正在加载.");
                return null;
            }

            // 检查是否已存在该界面
            if (Has(uiName))
            {
                FuLogger.LogWarning($"[UIModule] 界面 {uiName} 已经存在，不能重复打开.");
                return Get<T>();
            }

            // 分配临时序列号，用于管理加载状态
            var tempSerialId = ++m_SerialId;

            // 添加到加载字典
            m_LoadingDict.TryAdd(tempSerialId, uiName);

            try
            {
                T win;

                // 获取界面实例对象，如果对象池中存在，则直接使用对象池中的对象
                var winIns = m_WinInstancePool.Spawn(uiName);
                if (winIns != null)
                {
                    win = winIns.Target as T;

                    // 使用临时序列号创建Fui界面
                    return CreateFuiWin(win, tempSerialId, false, userData);
                }

                // 创建界面实例对象
                win    = new T();
                winIns = WinObject.Create(win.UIName, win);
                m_WinInstancePool.Register(winIns, true);

                // UI包已经加载过，则直接创建Fui界面
                if (PkgManager.IsLoadedPkg(win.PackageName))
                {
                    // 使用临时序列号创建Fui界面
                    return CreateFuiWin(win, tempSerialId, true, userData);
                }

                // UI包没有加载过，则等待加载UI包，加载完成后再创建Fui界面
                await PkgManager.LoadPkgAsync(win.PackageName);

                // 使用临时序列号创建Fui界面
                return CreateFuiWin(win, tempSerialId, true, userData);
            }
            finally
            {
                // 确保从加载字典中移除
                m_LoadingDict.Remove(tempSerialId);
            }
        }

        /// <summary>
        /// 创建FUI界面
        /// </summary>
        /// <param name="win">界面实例。</param>
        /// <param name="serialId">界面序列号。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns></returns>
        private T CreateFuiWin<T>(T win, int serialId, bool isNewInstance, object userData = null) where T : WinBase, new()
        {
            try
            {
                if (win == null) throw new InvalidOperationException($"[UIModule] 创建界面实例{typeof(T).Name}失败.");

                // 创建FUI界面。
                var uiView = UIPackage.CreateObject(win.PackageName, win.UIName) as GComponent;

                // 初始化界面
                win.Init(serialId, uiView, isNewInstance, userData);

                // FUI界面加入界面组
                var uiGroup = win.UIGroup;

                // AddChild会自动sort++ 
                uiGroup.AddChild(win.WinUI);
                uiGroup.Add(win);

                win._OnOpen();     // 界面打开回调
                uiGroup.Refresh(); // 刷新界面组

                // 广播界面打开成功事件
                var openUISuccessEventArgs = OpenUISuccessEventArgs.Create(win, userData);
                m_EventModule.Broadcast(this, openUISuccessEventArgs);

                return win;
            }
            catch (Exception exception)
            {
                var openUIFailureEventArgs = OpenUIFailureEventArgs.Create(serialId, typeof(T).Name, userData);
                m_EventModule.Broadcast(this, openUIFailureEventArgs);
                FuLogger.LogError($"[UIModule] 打开UI界面失败, 资源名称 '{typeof(T).Name}', 错误信息 '{exception}'.");
                return Get(serialId) as T;
            }
        }

        /// <summary>
        /// 设置界面实例是否加锁，如果加锁，则不会被释放(销毁)。
        /// </summary>
        /// <param name="uiView">要设置是否加锁的界面实例。</param>
        /// <param name="locked">界面实例是否加锁。</param>
        public void SetUILocked(object uiView, bool locked) => m_WinInstancePool.SetLocked(uiView, locked);

        /// <summary>
        /// 设置界面实例对象的优先级。优先级小的实例会优先被释放。
        /// </summary>
        /// <param name="uiView">要设置优先级的界面实例。</param>
        /// <param name="priority">界面实例优先级。</param>
        public void SetUIPriority(object uiView, int priority) => m_WinInstancePool.SetPriority(uiView, priority);
    }
}