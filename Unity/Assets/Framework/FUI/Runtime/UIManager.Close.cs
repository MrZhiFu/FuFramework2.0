//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FairyGUI;
using GameFrameX.Asset.Runtime;
using GameFrameX.ObjectPool;
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
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUI(int serialId)
        {
            CloseUI(serialId, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(int serialId, object userData)
        {
            if (IsLoadingUI(serialId))
            {
                m_WaitReleaseSet.Add(serialId);
                m_LoadingDict.Remove(serialId);
                return;
            }

            ViewBase view = GetUI(serialId);
            if (view == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));
            CloseUI(view, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        public void CloseUI(ViewBase view)
        {
            CloseUI(view, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUI<T>(object userData = null) where T : ViewBase
        {
            var fullName = typeof(T).FullName;
            ViewBase[] uis = GetAllLoadedUIs();
            foreach (var ui in uis)
            {
                if (ui.FullName != fullName) continue;
                if (!HasUIFullName(ui.FullName)) continue;
                CloseUI(ui, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(ViewBase view, object userData)
        {
            GameFrameworkGuard.NotNull(view, nameof(view));
            UIGroup uiGroup = view.UIGroup;

            uiGroup.RemoveUI(view);
            view.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(view.SerialId, view.UIName, uiGroup, userData);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            m_WaitRecycleQueue.Enqueue(view);
        }


        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUINow(int serialId)
        {
            CloseUINow(serialId, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(int serialId, object userData)
        {
            if (IsLoadingUI(serialId))
            {
                m_WaitReleaseSet.Add(serialId);
                m_LoadingDict.Remove(serialId);
                return;
            }

            ViewBase view = GetUI(serialId);
            if (view == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));

            CloseUINow(view, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        public void CloseUINow(ViewBase view)
        {
            CloseUINow(view, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUINow<T>(object userData) where T : ViewBase
        {
            var fullName = typeof(T).FullName;
            ViewBase[] uis = GetAllLoadedUIs();
            foreach (ViewBase ui in uis)
            {
                if (ui.FullName != fullName) continue;
                if (!HasUIFullName(ui.FullName)) continue;
                CloseUINow(ui, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="view">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(ViewBase view, object userData)
        {
            if (view == null) Log.Error("ui is null");
            if (view.UIGroup == null) Log.Error("UIGroup is null");

            var uiGroup = (UIGroup)view.UIGroup;
            uiGroup.RemoveUI(view);
            view.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(view.SerialId, view.UIName, uiGroup, userData);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            // 回收界面实例对象
            RecycleUINow(view);
        }


        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIs()
        {
            CloseAllLoadedUIs(null);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIs(object userData)
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
                m_WaitReleaseSet.Add(serialId);
            }

            m_LoadingDict.Clear();
        }


        /// <summary>
        /// 回收界面实例对象。
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

        /// <summary>
        /// 回收界面实例对象。
        /// </summary>
        /// <param name="view"></param>
        private void RecycleUINow(ViewBase view)
        {
            view.OnRecycle();
            var uiHandle = view.Handle as GameObject;
            if (!uiHandle) return;
            var displayObjectInfo = uiHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            component.Dispose();
        }
    }
}