using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.UI
{
    /// <summary>
    /// UI管理模块分部类之一。
    /// 目标：用于关闭UI界面。
    /// 功能：
    ///     1. 关闭界面(加入待回收队列，等待update轮询中回收)。
    ///     2. 立即关闭界面(立即回收)。
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>
        /// 关闭界面(加入待回收队列，等待update轮询中回收)。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void Close(int serialId)
        {
            var win = Get(serialId);
            if (win == null)
            {
                FuLogger.LogError($"[UIModule] 需要关闭的UI界面View为空 '{serialId}'.");
                return;
            }

            Close(win);
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update下一帧回收)。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        public void Close<T>() where T : WinBase
        {
            var win = Get<T>();
            if (win != null)
            {
                Close(win);
            }
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update下一帧回收)。
        /// </summary>
        /// <param name="win">要关闭的界面。</param>
        public void Close(WinBase win)
        {
            if (win == null)
            {
                FuLogger.LogError("[UIModule] 需要关闭的UI界面为空");
                return;
            }

            if (win.UIGroup == null)
            {
                FuLogger.LogError("[UIModule] 需要关闭的UI界面组为空");
                return;
            }

            // 防御性守卫：已加载窗口的 SerialId 取自其加载完成时的临时序列号，加载完成时已从
            // m_LoadingDict 移除，故正常流程下该分支不可达（历史上为"加载中被关闭"遗留）。
            // 在途加载的中止统一由 CloseAllLoading → m_CancelLoadingSet 在 _OpenAsync 的取消校验点处理。
            if (IsLoading(win.SerialId))
            {
                m_LoadingDict.Remove(win.SerialId);
                return;
            }

            var uiGroup = win.UIGroup;
            if (uiGroup == null) return;

            uiGroup.Remove(win);
            win._OnClose();
            uiGroup.Refresh();

            // 模糊界面：隐藏/重定位模糊覆盖层
            if (win.UIConfig?.Blur == true)
                OnWinClosed(win);

            // 抛出关闭界面完成事件
            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(win.SerialId, win.WinName, uiGroup);
            m_EventModule.Broadcast(this, closeUICompleteEventArgs);

            m_WaitRecycleQueue.Enqueue(win);
        }


        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseNow(int serialId)
        {
            var win = Get(serialId);
            if (win == null)
            {
                FuLogger.LogError($"[UIModule] 找不到界面 '{serialId}'");
                return;
            }

            CloseNow(win);
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        public void CloseNow<T>() where T : WinBase
        {
            var win = Get<T>();
            if (win != null)
            {
                CloseNow(win);
            }
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="win">要关闭的界面。</param>
        public void CloseNow(WinBase win)
        {
            if (win == null)
            {
                FuLogger.LogError("[UIModule] 需要关闭的UI界面View为空");
                return;
            }

            if (win.UIGroup == null)
            {
                FuLogger.LogError("[UIModule] 需要关闭的UI界面组为空");
                return;
            }

            if (IsLoading(win.SerialId))
            {
                m_LoadingDict.Remove(win.SerialId);
                return;
            }

            var uiGroup = win.UIGroup;
            if (uiGroup == null) return;

            uiGroup.Remove(win);
            win._OnClose();
            uiGroup.Refresh();

            // 模糊界面：隐藏/重定位模糊覆盖层
            if (win.UIConfig?.Blur == true)
                OnWinClosed(win);

            // 抛出关闭界面完成事件
            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(win.SerialId, win.WinName, uiGroup);
            m_EventModule.Broadcast(this, closeUICompleteEventArgs);

            // 立即回收界面实例对象
            Recycle(win);
        }

        /// <summary>
        /// 关闭所有界面(包括已加载和正在加载的界面)。
        /// </summary>
        public void CloseAll()
        {
            CloseAllLoaded();
            CloseAllLoading();
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoaded()
        {
            var uis = GetAllLoaded();
            foreach (var ui in uis)
            {
                if (!Has(ui.SerialId)) continue;
                Close(ui);
            }
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoading()
        {
            // 只清字典不够：在途的 _OpenAsync 仍会在 await 之后继续创建并注册窗口（CloseAll 后界面上屏）。
            // 先把在途序列号登记进取消集合，_OpenAsync 在每个 await 后的取消校验点命中即销毁半成品实例并中止；
            // 再清空加载字典（IsLoading/GetAllLoadingSerialIds 立即反映"无加载中"）。
            foreach (var (serialId, _) in m_LoadingDict)
            {
                m_CancelLoadingSet.Add(serialId);
            }

            m_LoadingDict.Clear();
        }

        /// <summary>
        /// 回收界面实例
        /// </summary>
        /// <param name="win"></param>
        private void Recycle(WinBase win)
        {
            // 先让窗口自身完成回收清理，再还回对象池；避免池超容量同步销毁后操作已销毁对象。
            // _OnRecycle 是用户代码，不能让它抛异常时跳过 Recycle——否则池槽位永久不归还
            //（对象池内部认为该对象仍"使用中"，直到 ObjectPoolModule 强制回收并告警）。
            try
            {
                win._OnRecycle();
            }
            finally
            {
                m_WinObjPool.Recycle(win);
            }
        }
    }
}