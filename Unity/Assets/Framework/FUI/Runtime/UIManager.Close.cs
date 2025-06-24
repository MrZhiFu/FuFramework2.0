//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;
using UnityEngine;

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
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(int serialId, object userData = null)
        {
            var view = GetUI(serialId);
            if (!view) Log.Error($"找不到界面 '{serialId}'.");
            CloseUI(view, userData);
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update轮询中回收)。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUI<T>(object userData = null) where T : ViewBase
        {
            var uis = GetUIs(typeof(T).Name);
            foreach (var ui in uis)
            {
                CloseUI(ui, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面(加入待回收队列，等待update轮询中回收)。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(ViewBase view, object userData = null)
        {
            if (!view) Log.Error("需要关闭的UI界面View为空");
            if (view.UIGroup == null) Log.Error("需要关闭的UI界面组为空");

            if (IsLoadingUI(view.SerialId))
            {
                m_LoadingInCloseSet.Add(view.SerialId);
                m_LoadingDict.Remove(view.SerialId);
                return;
            }

            var uiGroup = view.UIGroup;
            if (uiGroup == null) return;

            uiGroup.RemoveUI(view);
            view.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(view.SerialId, view.UIName, uiGroup, userData);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            m_WaitRecycleQueue.Enqueue(view);
        }


        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(int serialId, object userData = null)
        {
            var view = GetUI(serialId);
            if (!view) Log.Error($"找不到界面 '{serialId}'");

            CloseUINow(view, userData);
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUINow<T>(object userData = null) where T : ViewBase
        {
            var uis = GetUIs(typeof(T).Name);
            foreach (var ui in uis)
            {
                CloseUINow(ui, userData);
                break;
            }
        }

        /// <summary>
        /// 立即关闭界面(立即回收)。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(ViewBase view, object userData = null)
        {
            if (!view) Log.Error("需要关闭的UI界面View为空");
            if (view.UIGroup == null) Log.Error("需要关闭的UI界面组为空");

            if (IsLoadingUI(view.SerialId))
            {
                m_LoadingInCloseSet.Add(view.SerialId);
                m_LoadingDict.Remove(view.SerialId);
                return;
            }
            
            var uiGroup = view.UIGroup;
            if (uiGroup == null) return;
            
            uiGroup.RemoveUI(view);
            view.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(view.SerialId, view.UIName, uiGroup, userData);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            // 立即回收界面实例对象
            RecycleUI(view);
        }

        /// <summary>
        /// 关闭所有界面(包括已加载和正在加载的界面)。
        /// </summary>
        /// <param name="userData"></param>
        public void CloseAllUIs(object userData = null)
        {
            CloseAllLoadedUIs(userData);
            CloseAllLoadingUIs();
        }
        

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIs(object userData = null)
        {
            ViewBase[] uis = GetAllLoadedUIs();
            foreach (var ui in uis)
            {
                if (!HasUI(ui.SerialId)) continue;
                CloseUI(ui, userData);
            }
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIs()
        {
            foreach (var (serialId, _) in m_LoadingDict)
            {
                m_LoadingInCloseSet.Add(serialId);
            }

            m_LoadingDict.Clear();
        }

        /// <summary>
        /// 回收界面实例
        /// </summary>
        /// <param name="view"></param>
        private void RecycleUI(ViewBase view)
        {
            view.OnRecycle();
            var uiHandle = view.Handle as GameObject;
            if (!uiHandle) return;
            var displayObjectInfo = uiHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            m_InstancePool.Unspawn(component);
        }
    }
}