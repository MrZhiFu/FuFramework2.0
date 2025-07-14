using FairyGUI;
using GameFrameX.UI.FairyGUI.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// UI事件订阅器-界面的UI事件管理
    /// </summary>
    public abstract partial class ViewBase
    {
        /// <summary>
        /// UI事件订阅器
        /// </summary>
        private FuiEventRegister UIEventRegister{ get; set; }
        
        /// <summary>
        /// 添加UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        protected void AddUIListener(EventListener listener, EventCallback1 callback)
        {
            UIEventRegister.AddUIListener(listener, callback);
        }

        /// <summary>
        /// 移除UI上指定组件的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        protected void RemoveUIListener(EventListener listener, EventCallback1 callback)
        {
            UIEventRegister.RemoveUIListener(listener, callback);
        }

        /// <summary>
        /// 设置UI上指定组件的监听事件(会删除以前添加的事件)
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        protected void SetUIListener(EventListener listener, EventCallback1 callback)
        {
            UIEventRegister.SetUIListener(listener, callback);
        }

        /// <summary>
        /// 清理UI上的所有组件的监听事件
        /// </summary>
        /// <param name="listener"></param>
        protected void ClearUIListener(EventListener listener)
        {
            UIEventRegister.ClearUIListener(listener);
        }
        
        /// <summary>
        /// 释放UI事件注册器
        /// </summary>
        private void DisposeUIEventRegister()
        {
            UIEventRegister.Release();
            UIEventRegister = null;
        }
    }
}