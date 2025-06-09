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
    internal sealed partial class UIManager
    {
        /// 关闭界面完成事件。
        private EventHandler<CloseUICompleteEventArgs> m_CloseUICompleteEventHandler;

        /// <summary>
        /// 关闭界面完成事件。
        /// </summary>
        public event EventHandler<CloseUICompleteEventArgs> CloseUIComplete
        {
            add => m_CloseUICompleteEventHandler += value;
            remove => m_CloseUICompleteEventHandler -= value;
        }


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

            IUIBase iuiBase = GetUI(serialId);
            if (iuiBase == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));
            CloseUI(iuiBase, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="iuiBase">要关闭的界面。</param>
        public void CloseUI(IUIBase iuiBase)
        {
            CloseUI(iuiBase, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUI<T>(object userData) where T : IUIBase
        {
            var       fullName = typeof(T).FullName;
            IUIBase[] uis      = GetAllLoadedUIs();
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
        /// <param name="iuiBase">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(IUIBase iuiBase, object userData)
        {
            GameFrameworkGuard.NotNull(iuiBase,         nameof(iuiBase));
            GameFrameworkGuard.NotNull(iuiBase.UIGroup, nameof(iuiBase.UIGroup));
            UIGroup uiGroup = (UIGroup)iuiBase.UIGroup;

            uiGroup.RemoveUI(iuiBase);
            iuiBase.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUICompleteEventHandler != null)
            {
                var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(iuiBase.SerialId, iuiBase.UIAssetName, uiGroup, userData);
                m_CloseUICompleteEventHandler(this, closeUICompleteEventArgs);
            }

            m_WaitRecycleQueue.Enqueue(iuiBase);
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

            IUIBase iuiBase = GetUI(serialId);
            if (iuiBase == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));

            CloseUINow(iuiBase, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="iuiBase">要关闭的界面。</param>
        public void CloseUINow(IUIBase iuiBase)
        {
            CloseUINow(iuiBase, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUINow<T>(object userData) where T : IUIBase
        {
            var       fullName = typeof(T).FullName;
            IUIBase[] uis      = GetAllLoadedUIs();
            foreach (IUIBase ui in uis)
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
        /// <param name="iuiBase">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(IUIBase iuiBase, object userData)
        {
            GameFrameworkGuard.NotNull(iuiBase,         nameof(iuiBase));
            GameFrameworkGuard.NotNull(iuiBase.UIGroup, nameof(iuiBase.UIGroup));

            var uiGroup = (UIGroup)iuiBase.UIGroup;
            uiGroup.RemoveUI(iuiBase);
            iuiBase.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUICompleteEventHandler != null)
            {
                var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(iuiBase.SerialId, iuiBase.UIAssetName, uiGroup, userData);
                m_CloseUICompleteEventHandler(this, closeUICompleteEventArgs);
                // ReferencePool.Release(closeUICompleteEventArgs);
            }

            // 回收界面实例对象
            RecycleUINow(iuiBase);
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
            IUIBase[] uis = GetAllLoadedUIs();
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
        /// <param name="iuiBase"></param>
        private void RecycleUI(IUIBase iuiBase)
        {
            iuiBase.OnRecycle();
            var uiHandle = iuiBase.Handle as GameObject;
            if (!uiHandle) return;
            var displayObjectInfo = uiHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            m_InstancePool.Unspawn(component);
        }

        /// <summary>
        /// 回收界面实例对象。
        /// </summary>
        /// <param name="iuiBase"></param>
        private void RecycleUINow(IUIBase iuiBase)
        {
            iuiBase.OnRecycle();
            var uiHandle = iuiBase.Handle as GameObject;
            if (!uiHandle) return;
            var displayObjectInfo = uiHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            component.Dispose();
        }
    }
}