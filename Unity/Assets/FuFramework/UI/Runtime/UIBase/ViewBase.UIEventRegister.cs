using FairyGUI;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// UI事件订阅器-界面的UI事件管理
    /// </summary>
    public abstract partial class ViewBase
    {
        /// <summary>
        /// UI事件订阅器
        /// </summary>
        private FuiEventRegister UIEventRegister { get; set; }

        /// <summary>
        /// 添加UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        /// <param name="callback">回调函数</param>
        public void AddUIListener(EventListener listener, EventCallback1 callback)
        {
            FuGuard.NotNull(listener,        nameof(listener));
            FuGuard.NotNull(callback,        nameof(callback));
            FuGuard.NotNull(UIEventRegister, "UI事件订阅器为空, 请先初始化UIEventRegister.");
            UIEventRegister.AddUIListener(listener, callback);
        }

        /// <summary>
        /// 设置UI上指定组件的监听事件(会删除以前添加的事件)
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        /// <param name="callback">回调函数</param>
        public void SetUIListener(EventListener listener, EventCallback1 callback)
        {
            FuGuard.NotNull(listener,        nameof(listener));
            FuGuard.NotNull(callback,        nameof(callback));
            FuGuard.NotNull(UIEventRegister, "UI事件订阅器为空, 请先初始化UIEventRegister.");
            UIEventRegister.SetUIListener(listener, callback);
        }

        /// <summary>
        /// 移除UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        /// <param name="callback">回调函数</param>
        public void RemoveUIListener(EventListener listener, EventCallback1 callback)
        {
            FuGuard.NotNull(listener,        nameof(listener));
            FuGuard.NotNull(callback,        nameof(callback));
            FuGuard.NotNull(UIEventRegister, "UI事件订阅器为空, 请先初始化UIEventRegister.");
            UIEventRegister.RemoveUIListener(listener, callback);
        }

        /// <summary>
        /// 清理UI上指定组件的所有监听事件
        /// </summary>
        /// <param name="listener">被监听者(一般是交互组件，如Button)</param>
        public void ClearUIListener(EventListener listener)
        {
            FuGuard.NotNull(listener,        nameof(listener));
            FuGuard.NotNull(UIEventRegister, "UI事件订阅器为空, 请先初始化UIEventRegister.");
            UIEventRegister.ClearUIListener(listener);
        }

        /// <summary>
        /// 清理UI上所有组件的所有监听事件
        /// </summary>
        public void ClearAllUIListener()
        {
            FuGuard.NotNull(UIEventRegister, "UI事件订阅器为空, 请先初始化UIEventRegister.");
            UIEventRegister.ClearAllUIListener();
        }

        /// <summary>
        /// 释放UI事件注册器
        /// </summary>
        private void ReleaseUIEventRegister()
        {
            UIEventRegister.Release();
            UIEventRegister = null;
        }
    }
}