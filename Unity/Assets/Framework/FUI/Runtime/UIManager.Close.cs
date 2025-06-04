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
        private EventHandler<CloseUIFormCompleteEventArgs> m_CloseUIFormCompleteEventHandler;

        /// <summary>
        /// 关闭界面完成事件。
        /// </summary>
        public event EventHandler<CloseUIFormCompleteEventArgs> CloseUIFormComplete
        {
            add { m_CloseUIFormCompleteEventHandler += value; }
            remove { m_CloseUIFormCompleteEventHandler -= value; }
        }

        /// <summary>
        /// 回收界面实例对象。
        /// </summary>
        /// <param name="uiForm"></param>
        private void RecycleUIForm(IUIForm uiForm)
        {
            uiForm.OnRecycle();
            var formHandle = uiForm.Handle as GameObject;
            if (formHandle)
            {
                var displayObjectInfo = formHandle.GetComponent<DisplayObjectInfo>();
                if (displayObjectInfo)
                {
                    if (displayObjectInfo.displayObject.gOwner is GComponent component)
                    {
                        m_InstancePool.Unspawn(component);
                    }
                }
            }
        }

        /// <summary>
        /// 回收界面实例对象。
        /// </summary>
        /// <param name="uiForm"></param>
        private void RecycleUIFormNow(IUIForm uiForm)
        {
            uiForm.OnRecycle();
            var formHandle = uiForm.Handle as GameObject;
            if (formHandle)
            {
                var displayObjectInfo = formHandle.GetComponent<DisplayObjectInfo>();
                if (displayObjectInfo)
                {
                    if (displayObjectInfo.displayObject.gOwner is GComponent component)
                    {
                        component.Dispose();
                        // m_InstancePool.Unspawn(component);
                    }
                }
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUIForm(int serialId)
        {
            CloseUIForm(serialId, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(int serialId, object userData)
        {
            if (IsLoadingUIForm(serialId))
            {
                m_UIFormsToReleaseOnLoad.Add(serialId);
                m_UIFormsBeingLoaded.Remove(serialId);
                return;
            }

            IUIForm uiForm = GetUIForm(serialId);
            if (uiForm == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find UI form '{0}'.", serialId));
            }

            CloseUIForm(uiForm, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUIForm(IUIForm uiForm)
        {
            CloseUIForm(uiForm, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUIForm<T>(object userData) where T : IUIForm
        {
            var fullName = typeof(T).FullName;
            IUIForm[] uiForms = GetAllLoadedUIForms();
            foreach (IUIForm uiForm in uiForms)
            {
                if (uiForm.FullName != fullName)
                {
                    continue;
                }

                if (!HasUIFormFullName(uiForm.FullName))
                {
                    continue;
                }

                CloseUIForm(uiForm, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(IUIForm uiForm, object userData)
        {
            GameFrameworkGuard.NotNull(uiForm, nameof(uiForm));
            GameFrameworkGuard.NotNull(uiForm.UIGroup, nameof(uiForm.UIGroup));
            UIGroup uiGroup = (UIGroup)uiForm.UIGroup;

            uiGroup.RemoveUIForm(uiForm);
            uiForm.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUIFormCompleteEventHandler != null)
            {
                CloseUIFormCompleteEventArgs closeUIFormCompleteEventArgs = CloseUIFormCompleteEventArgs.Create(uiForm.SerialId, uiForm.UIFormAssetName, uiGroup, userData);
                m_CloseUIFormCompleteEventHandler(this, closeUIFormCompleteEventArgs);
                // ReferencePool.Release(closeUIFormCompleteEventArgs);
            }

            m_RecycleQueue.Enqueue(uiForm);
        }


        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUIFormNow(int serialId)
        {
            CloseUIFormNow(serialId, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIFormNow(int serialId, object userData)
        {
            if (IsLoadingUIForm(serialId))
            {
                m_UIFormsToReleaseOnLoad.Add(serialId);
                m_UIFormsBeingLoaded.Remove(serialId);
                return;
            }

            IUIForm uiForm = GetUIForm(serialId);
            if (uiForm == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find UI form '{0}'.", serialId));
            }

            CloseUIFormNow(uiForm, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUIFormNow(IUIForm uiForm)
        {
            CloseUIFormNow(uiForm, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUIFormNow<T>(object userData) where T : IUIForm
        {
            var fullName = typeof(T).FullName;
            IUIForm[] uiForms = GetAllLoadedUIForms();
            foreach (IUIForm uiForm in uiForms)
            {
                if (uiForm.FullName != fullName)
                {
                    continue;
                }

                if (!HasUIFormFullName(uiForm.FullName))
                {
                    continue;
                }

                CloseUIFormNow(uiForm, userData);
                break;
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIFormNow(IUIForm uiForm, object userData)
        {
            GameFrameworkGuard.NotNull(uiForm, nameof(uiForm));
            GameFrameworkGuard.NotNull(uiForm.UIGroup, nameof(uiForm.UIGroup));
            UIGroup uiGroup = (UIGroup)uiForm.UIGroup;

            uiGroup.RemoveUIForm(uiForm);
            uiForm.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            if (m_CloseUIFormCompleteEventHandler != null)
            {
                CloseUIFormCompleteEventArgs closeUIFormCompleteEventArgs = CloseUIFormCompleteEventArgs.Create(uiForm.SerialId, uiForm.UIFormAssetName, uiGroup, userData);
                m_CloseUIFormCompleteEventHandler(this, closeUIFormCompleteEventArgs);
                // ReferencePool.Release(closeUIFormCompleteEventArgs);
            }

            RecycleUIFormNow(uiForm);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIForms()
        {
            CloseAllLoadedUIForms(null);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIForms(object userData)
        {
            IUIForm[] uiForms = GetAllLoadedUIForms();
            foreach (IUIForm uiForm in uiForms)
            {
                if (!HasUIForm(uiForm.SerialId))
                {
                    continue;
                }

                CloseUIForm(uiForm, userData);
            }
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIForms()
        {
            foreach (KeyValuePair<int, string> uiFormBeingLoaded in m_UIFormsBeingLoaded)
            {
                m_UIFormsToReleaseOnLoad.Add(uiFormBeingLoaded.Key);
            }

            m_UIFormsBeingLoaded.Clear();
        }
    }
}