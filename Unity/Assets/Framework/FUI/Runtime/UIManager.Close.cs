//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    public sealed partial class UIManager
    {
        /// <summary>
        /// 关闭界面(加入待回收队列，等待update轮询中回收)。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUI(int serialId)
        {
            var view = GetUI(serialId);
            if (view == null)
            {
                Log.Error($"[UIManager]需要关闭的UI界面View为空 '{serialId}'.");
                return;
            }

            CloseUI(view);
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update轮询中回收)。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CloseUI<T>() where T : ViewBase
        {
            var uis = GetUIs<T>(typeof(T).Name);
            foreach (var ui in uis)
            {
                CloseUI(ui);
                break;
            }
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update轮询中回收)。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        public void CloseUI(ViewBase view)
        {
            if (view == null)
            {
                Log.Error("[UIManager]需要关闭的UI界面为空");
                return;
            }

            if (view.UIGroup == null)
            {
                Log.Error("[UIManager]需要关闭的UI界面组为空");
                return;
            }

            if (IsLoadingUI(view.SerialId))
            {
                m_LoadingDict.Remove(view.SerialId);
                return;
            }

            var uiGroup = view.UIGroup;
            if (uiGroup == null) return;

            uiGroup.RemoveUI(view);
            view._OnClose();
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(view.SerialId, view.UIName, uiGroup);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            m_WaitRecycleQueue.Enqueue(view);
        }


        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUINow(int serialId)
        {
            var view = GetUI(serialId);
            if (view == null)
            {
                Log.Error($"[UIManager]找不到界面 '{serialId}'");
                return;
            }

            CloseUINow(view);
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void CloseUINow<T>() where T : ViewBase
        {
            var uis = GetUIs<T>(typeof(T).Name);
            foreach (var ui in uis)
            {
                CloseUINow(ui);
                break;
            }
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        public void CloseUINow(ViewBase view)
        {
            if (view == null)
            {
                Log.Error("[UIManager]需要关闭的UI界面View为空");
                return;
            }

            if (view.UIGroup == null)
            {
                Log.Error("[UIManager]需要关闭的UI界面组为空");
                return;
            }

            if (IsLoadingUI(view.SerialId))
            {
                m_LoadingDict.Remove(view.SerialId);
                return;
            }

            var uiGroup = view.UIGroup;
            if (uiGroup == null) return;

            uiGroup.RemoveUI(view);
            view._OnClose();
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(view.SerialId, view.UIName, uiGroup);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            // 立即回收界面实例对象
            RecycleUI(view);
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
        /// <param name="view"></param>
        private void RecycleUI(ViewBase view)
        {
            m_InstancePool.Unspawn(view);
            view._OnRecycle();
        }
    }
}