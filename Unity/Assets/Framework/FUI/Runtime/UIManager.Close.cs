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
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(int serialId, object userData = null)
        {
            if (IsLoadingUI(serialId))
            {
                m_WaitReleaseSet.Add(serialId);
                m_LoadingDict.Remove(serialId);
                return;
            }

            var ui = GetUI(serialId);
            if (ui == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));
            CloseUI(ui, userData);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUI<T>(object userData = null) where T : UIBase
        {
            var fullName = typeof(T).FullName;
            UIBase[] uis = GetAllLoadedUIs();
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
        /// <param name="ui">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(UIBase ui, object userData = null)
        {
            GameFrameworkGuard.NotNull(ui, nameof(ui));
            UIGroup uiGroup = ui.UIGroup;

            uiGroup.RemoveUI(ui);
            ui.OnClose(m_IsShutdown, userData);
            uiGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(ui.SerialId, ui.UIAssetName, uiGroup, userData);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);
            m_WaitRecycleQueue.Enqueue(ui);
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(int serialId, object userData = null)
        {
            if (IsLoadingUI(serialId))
            {
                m_WaitReleaseSet.Add(serialId);
                m_LoadingDict.Remove(serialId);
                return;
            }

            UIBase ui = GetUI(serialId);
            if (ui == null) throw new GameFrameworkException(Utility.Text.Format("找不到界面 '{0}'.", serialId));

            CloseUINow(ui, userData);
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        /// <typeparam name="T"></typeparam>
        public void CloseUINow<T>(object userData = null) where T : UIBase
        {
            var fullName = typeof(T).FullName;
            var uis = GetAllLoadedUIs();
            
            foreach (UIBase ui in uis)
            {
                if (ui.FullName != fullName) continue;
                if (!HasUIFullName(ui.FullName)) continue;
                CloseUINow(ui, userData);
                break;
            }
        }

        /// <summary>
        /// 立即关闭界面。
        /// </summary>
        /// <param name="ui">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUINow(UIBase ui, object userData = null)
        {
            if (ui == null) throw new GameFrameworkException($"需要立即关闭的界面{ui.Name}不存在");
            if (ui.UIGroup == null) throw new GameFrameworkException($"需要立即关闭的界面{ui.Name}的界面组不存在");

            ui.UIGroup.RemoveUI(ui);
            ui.OnClose(m_IsShutdown, userData);
            ui.UIGroup.Refresh();

            var closeUICompleteEventArgs = CloseUICompleteEventArgs.Create(ui.SerialId, ui.UIAssetName, ui.UIGroup, userData);
            m_EventComponent.Fire(this, closeUICompleteEventArgs);

            // 立即回收界面实例对象
            RecycleUINow(ui);
        }


        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIs(object userData = null)
        {
            UIBase[] uis = GetAllLoadedUIs();
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
        /// <param name="ui"></param>
        private void RecycleUI(UIBase ui)
        {
            ui.OnRecycle();
            
            var uiHandle = ui.Handle as GameObject;
            if (!uiHandle) return;
            
            var displayObjectInfo = uiHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            m_InstancePool.Unspawn(component);
        }

        /// <summary>
        /// 立即回收界面实例对象。
        /// </summary>
        /// <param name="ui"></param>
        private void RecycleUINow(UIBase ui)
        {
            ui.OnRecycle();
            
            var uiHandle = ui.Handle as GameObject;
            if (!uiHandle) return;
            
            var displayObjectInfo = uiHandle.GetComponent<DisplayObjectInfo>();
            if (!displayObjectInfo) return;
            
            if (displayObjectInfo.displayObject.gOwner is not GComponent component) return;
            component.Dispose();
        }
    }
}