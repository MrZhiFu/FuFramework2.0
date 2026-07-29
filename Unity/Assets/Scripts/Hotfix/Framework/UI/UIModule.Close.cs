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
        public void CloseUI(int serialId)
        {
            var win = GetUI(serialId);
            if (win == null)
            {
                FuLogger.LogError($"[UIModule] 需要关闭的UI界面View为空 '{serialId}'.");
                return;
            }

            CloseUI(win);
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update下一帧回收)。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        public void CloseUI<T>() where T : WinBase
        {
            var win = GetUI<T>();
            if (win != null)
            {
                CloseUI(win);
            }
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update下一帧回收)。
        /// </summary>
        /// <param name="win">要关闭的界面。</param>
        public void CloseUI(WinBase win)
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

            if (IsLoadingUI(win.SerialId))
            {
                m_LoadingDict.Remove(win.SerialId);
                return;
            }

            var uiGroup = win.UIGroup;
            if (uiGroup == null) return;

            uiGroup.RemoveUI(win);
            win._OnClose();
            uiGroup.Refresh();

            // 抛出关闭界面完成事件
            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(win.SerialId, win.UIName, uiGroup);
            m_EventModule.Broadcast(this, closeUICompleteEventArgs);

            m_WaitRecycleQueue.Enqueue(win);
        }


        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUINow(int serialId)
        {
            var win = GetUI(serialId);
            if (win == null)
            {
                FuLogger.LogError($"[UIModule] 找不到界面 '{serialId}'");
                return;
            }

            CloseUINow(win);
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        public void CloseUINow<T>() where T : WinBase
        {
            var win = GetUI<T>();
            if (win != null)
            {
                CloseUINow(win);
            }
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="win">要关闭的界面。</param>
        public void CloseUINow(WinBase win)
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

            if (IsLoadingUI(win.SerialId))
            {
                m_LoadingDict.Remove(win.SerialId);
                return;
            }

            var uiGroup = win.UIGroup;
            if (uiGroup == null) return;

            uiGroup.RemoveUI(win);
            win._OnClose();
            uiGroup.Refresh();

            // 抛出关闭界面完成事件
            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(win.SerialId, win.UIName, uiGroup);
            m_EventModule.Broadcast(this, closeUICompleteEventArgs);

            // 立即回收界面实例对象
            RecycleUI(win);
        }

        /// <summary>
        /// 关闭所有界面(包括已加载和正在加载的界面)。
        /// </summary>
        public void CloseAllUIs()
        {
            CloseAllLoadedUIs();
            CloseAllLoadingUIs();
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIs()
        {
            var uis = GetAllLoadedUIs();
            foreach (var ui in uis)
            {
                if (!HasUI(ui.SerialId)) continue;
                CloseUI(ui);
            }
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIs()
        {
            m_LoadingDict.Clear();
        }

        /// <summary>
        /// 回收界面实例
        /// </summary>
        /// <param name="win"></param>
        private void RecycleUI(WinBase win)
        {
            m_InstancePool.Recycle(win);
            win._OnRecycle();
        }
    }
}
