using System;
using FairyGUI;
using FuFramework.Core.Runtime;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面组。
    /// 目标：管理同一层级下的多个界面，继承自 FairyGUI 的 GComponent。
    /// 功能：
    ///     1. 界面列表管理 (FuLinkedList[UIInfo])。
    ///     2. 暂停/恢复整个组。
    ///     3. 界面深度排序。
    ///     4. 被覆盖/恢复处理。
    /// </summary>
    public sealed class UIGroup : GComponent
    {
        /// 界面组是否暂停
        private bool m_Pause;

        /// 获取或设置界面组所在的层级。
        public EUILayer Layer { get; private set; }

        /// 界面组内的界面列表
        private readonly FuLinkedList<ViewInfo> m_UIInfoList = new();


        /// <summary>
        /// 获取或设置界面组是否暂停。
        /// </summary>
        public bool Pause
        {
            get => m_Pause;
            set
            {
                if (m_Pause == value) return;
                m_Pause = value;
                Refresh();
            }
        }

        /// <summary>
        /// 获取界面组中界面数量。
        /// </summary>
        public int UICount => m_UIInfoList.Count;

        /// <summary>
        /// 获取当前界面。
        /// </summary>
        public ViewBase CurrentViewBase => m_UIInfoList.First?.Value.View;

        /// <summary>
        /// 初始化界面组的新实例。
        /// </summary>
        /// <param name="layer">界面组层级。</param>
        public void Init(EUILayer layer)
        {
            Layer   = layer;
            m_Pause = false;
            m_UIInfoList.Clear();
            sortingOrder = (int)layer;
        }

        /// <summary>
        /// 界面组轮询。
        /// 遍历界面组中所有界面，驱动每个界面Update。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        public void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            if (m_Pause) return;
            var current = m_UIInfoList.First;
            while (current != null)
            {
                var uiInfo = current.Value;
                var view   = uiInfo.View;

                // 只更新未暂停且可见的界面
                if (!uiInfo.Paused && view.Visible)
                {
                    view._OnUpdate(deltaTime, unscaledDeltaTime);
                }

                // 继续处理下一个界面
                current = current.Next;
            }
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUI(int serialId)
        {
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.View.SerialId == serialId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        /// <returns></returns>
        public bool HasUI<T>() where T : ViewBase => HasUI(typeof(T).Name);

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUI(string uiName)
        {
            uiName.NotNullOrEmpty(nameof(uiName));
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.View.UIName == uiName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(int serialId)
        {
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.View.SerialId == serialId)
                {
                    return uiInfo.View;
                }
            }

            return null;
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <returns>要获取的界面。</returns>
        public T GetUI<T>() where T : ViewBase => (T)GetUI(typeof(T).Name);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(string uiName)
        {
            uiName.NotNullOrEmpty(nameof(uiName));
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.View.UIName == uiName)
                {
                    return uiInfo.View;
                }
            }

            return null;
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <returns>界面组中的所有界面。</returns>
        public ViewBase[] GetAllUIs()
        {
            var result = new ViewBase[UICount];

            var i = 0;
            foreach (var uiInfo in m_UIInfoList)
            {
                result[i++] = uiInfo.View;
            }

            return result;
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <param name="results">界面组中的所有界面。</param>
        public void GetAllUIs(List<ViewBase> results)
        {
            results.NotNull(nameof(results));
            results.Clear();
            foreach (var uiInfo in m_UIInfoList)
            {
                results.Add(uiInfo.View);
            }
        }

        /// <summary>
        /// 往界面组增加界面。
        /// </summary>
        /// <param name="view">要增加的界面。</param>
        public void AddUI(ViewBase view)
        {
            if (HasUI(view.SerialId))
                throw new InvalidOperationException($"[UIGroup] UI组 '{Layer.ToString()}' 中已经存在UI界面 '[{view.SerialId}]{view.UIName}'.");

            var uiInfo = ViewInfo.Create(view);
            m_UIInfoList.AddFirst(uiInfo);
        }

        /// <summary>
        /// 从界面组移除界面。
        /// </summary>
        /// <param name="view">要移除的界面。</param>
        public void RemoveUI(ViewBase view)
        {
            var uiInfo = GetUIInfo(view);
            if (uiInfo == null)
                throw new InvalidOperationException($"[UIGroup] 无法找到界面id为 '{view.SerialId}' ，资源名称为 '{view.UIName}' 的UI界面信息.");

            if (!m_UIInfoList.Remove(uiInfo))
                throw new InvalidOperationException($"[UIGroup] UI组 '{Layer.ToString()}' 中不存在UI界面 '[{view.SerialId}]{view.UIName}'.");

            // 释放界面信息实例
            ReferencePool.Runtime.ReferencePool.Release(uiInfo);
        }

        /// <summary>
        /// 刷新界面组。
        /// </summary>
        public void Refresh()
        {
            // 从链表头部开始遍历
            var current = m_UIInfoList.First;

            var isCover = false;   // 是否覆盖后面的界面，初始为false，表示第一个界面需要显示完整，后续界面需要被覆盖
            var isPause = m_Pause; // 是否暂停的标志，初始值由组暂停状态决定，后续根据界面暂停状态更新

            while (current is { Value: not null })
            {
                // 先缓存下一个节点，因为回调可能修改链表结构（如关闭当前界面）
                var next   = current.Next;
                var uiInfo = current.Value;

                // 节点可能已被销毁，跳过继续处理下一个节点
                if (uiInfo?.View == null)
                {
                    current = next;
                    continue;
                }

                // 处理被暂停的界面状态
                HandlePauseState(uiInfo, ref isPause);

                // 处理被覆盖的界面状态
                HandleCoverState(uiInfo, ref isCover);

                // 移动到下一个节点
                current = next;
            }
        }

        /// <summary>
        /// 处理被暂停的界面状态。
        /// 顶部的第一个界面不会走到暂停逻辑，只有第二个及以后的界面才会被暂停。
        /// </summary>
        /// <param name="viewInfo">界面信息。</param>
        /// <param name="isPause">是否暂停的标志。</param>
        private void HandlePauseState(ViewInfo viewInfo, ref bool isPause)
        {
            // 先根据当前暂停状态执行暂停/恢复（第一个界面 isPause=false，不会触发暂停）
            if (isPause && !viewInfo.Paused)
            {
                viewInfo.Paused = true;
                viewInfo.View._OnPause(); // 触发暂停回调
            }
            else if (!isPause && viewInfo.Paused)
            {
                viewInfo.Paused = false;
                viewInfo.View._OnResume(); // 触发恢复回调
            }

            // 如果当前界面要求暂停被覆盖的界面，则后续界面进入暂停状态
            if (!isPause && viewInfo.View.PauseCoveredUI)
            {
                isPause = true;
            }
        }

        /// <summary>
        /// 处理被覆盖的界面状态。
        /// 顶部的第一个界面不会走到这里，只有第二个及以后的界面才会被覆盖。
        /// </summary>
        /// <param name="viewInfo">界面信息。</param>
        /// <param name="isCover">是否覆盖的标志。</param>
        private void HandleCoverState(ViewInfo viewInfo, ref bool isCover)
        {
            if (isCover && !viewInfo.Covered)
            {
                viewInfo.Covered = true;
                viewInfo.View._OnBeCover(); // 触发被覆盖回调
            }
            else if (!isCover && viewInfo.Covered)
            {
                viewInfo.Covered = false;
                viewInfo.View._OnReveal(); // 触发重新显示回调
            }

            // 后续界面需要被覆盖
            if (!isCover)
            {
                isCover = true;
            }
        }

        /// <summary>
        /// 获取UI界面的界面信息。
        /// </summary>
        /// <param name="view">界面实例。</param>
        /// <returns>界面信息。</returns>
        private ViewInfo GetUIInfo(ViewBase view)
        {
            view.NotNull(nameof(view));
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.View == view)
                    return uiInfo;
            }

            return null;
        }
    }
}