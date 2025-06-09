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
        public event EventHandler<CloseUICompleteEventArgs> CloseUIFormComplete
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

            IUIForm uiForm = GetUI(serialId);
            if (uiForm == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));
            CloseUI(uiForm, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUI(IUIForm uiForm)
        {
            CloseUI(uiForm, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUI<T>(object userData) where T : IUIForm
        {
            var fullName = typeof(T).FullName;
            IUIForm[] uiForms = GetAllLoadedUIs();
            foreach (IUIForm uiForm in uiForms)
            {
                if (uiForm.FullName != fullName) continue;
                if (!HasUIFullName(uiForm.FullName)) continue;
                CloseUI(uiForm, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(IUIForm uiForm, object userData)
        {
            GameFrameworkGuard.NotNull(uiForm, nameof(uiForm));
            GameFrameworkGuard.NotNull(uiForm.UIGroup, nameof(uiForm.UIGroup));
            UIGroup uiGroup = (UIGroup)uiForm.UIGroup;

            uiGroup.RemoveUI(uiForm);
            uiForm.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUICompleteEventHandler != null)
            {
                var closeUIFormCompleteEventArgs = CloseUICompleteEventArgs.Create(uiForm.SerialId, uiForm.UIFormAssetName, uiGroup, userData);
                m_CloseUICompleteEventHandler(this, closeUIFormCompleteEventArgs);
            }

            m_WaitRecycleQueue.Enqueue(uiForm);
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

            IUIForm uiForm = GetUI(serialId);
            if (uiForm == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));

            CloseUINow(uiForm, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUINow(IUIForm uiForm)
        {
            CloseUINow(uiForm, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUINow<T>(object userData) where T : IUIForm
        {
            var fullName = typeof(T).FullName;
            IUIForm[] uiForms = GetAllLoadedUIs();
            foreach (IUIForm uiForm in uiForms)
            {
                if (uiForm.FullName != fullName) continue;
                if (!HasUIFullName(uiForm.FullName)) continue;
                CloseUINow(uiForm, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(IUIForm uiForm, object userData)
        {
            GameFrameworkGuard.NotNull(uiForm, nameof(uiForm));
            GameFrameworkGuard.NotNull(uiForm.UIGroup, nameof(uiForm.UIGroup));
            UIGroup uiGroup = (UIGroup)uiForm.UIGroup;

            uiGroup.RemoveUI(uiForm);
            uiForm.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUICompleteEventHandler != null)
            {
                CloseUICompleteEventArgs closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(uiForm.SerialId, uiForm.UIFormAssetName, uiGroup, userData);
                m_CloseUICompleteEventHandler(this, closeUICompleteEventArgs);
                // ReferencePool.Release(closeUIFormCompleteEventArgs);
            }

            // 回收界面实例对象
            RecycleUINow(uiForm);
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
            IUIForm[] uiForms = GetAllLoadedUIs();
            foreach (var uiForm in uiForms)
            {
                if (!HasUI(uiForm.SerialId)) continue;
                CloseUI(uiForm, userData);
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
        /// <param name="uiForm"></param>
        private void RecycleUI(IUIForm uiForm)
        {
            uiForm.OnRecycle();
            var formHandle = uiForm.Handle as GameObject;
            if (!formHandle) return;
            var displayObjectInfo = formHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            m_InstancePool.Unspawn(component);
        }

        /// <summary>
        /// 回收界面实例对象。
        /// </summary>
        /// <param name="uiForm"></param>
        private void RecycleUINow(IUIForm uiForm)
        {
            uiForm.OnRecycle();
            var formHandle = uiForm.Handle as GameObject;
            if (!formHandle) return;
            var displayObjectInfo = formHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            component.Dispose();
        }
    }
}