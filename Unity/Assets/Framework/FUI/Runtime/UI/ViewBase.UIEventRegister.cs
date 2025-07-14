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
        /// 负责该界面的响应UI管理
        /// </summary>
        private FuiEventRegister UIEventRegister{ get; set; }
        
        /// <summary>
        /// 添加FUI某个可响应UI上的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        protected void AddUIListener(EventListener listener, EventCallback1 callback)
        {
            UIEventRegister.AddUIListener(listener, callback);
        }

        /// <summary>
        /// 移除FUI某个可响应UI上的监听事件
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        protected void RemoveUIListener(EventListener listener, EventCallback1 callback)
        {
            UIEventRegister.RemoveUIListener(listener, callback);
        }

        /// <summary>
        /// 设置FUI某个可响应UI上的监听事件(会删除以前添加的事件)
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="callback"></param>
        protected void SetUIListener(EventListener listener, EventCallback1 callback)
        {
            UIEventRegister.SetUIListener(listener, callback);
        }

        /// <summary>
        /// 清理FUI某个可响应UI上的所有监听事件
        /// </summary>
        /// <param name="listener"></param>
        protected void ClearUIListener(EventListener listener)
        {
            UIEventRegister.ClearUIListener(listener);
        }
    }
}