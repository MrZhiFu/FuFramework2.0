using FairyGUI;
using System.Linq;
using FuFramework.Core.Runtime;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面组。
    /// 职责：管理同一层级下的多个界面，继承自 FairyGUI 的 GComponent。
    /// 核心功能:
    ///  1. 界面列表管理 (FuLinkedList[UIInfo])。
    ///  2. 暂停/恢复整个组。
    ///  3. 界面深度排序。
    ///  4. 被覆盖/恢复处理。
    /// </summary>
    public sealed class UIGroup : GComponent
    {
        /// 界面组是否暂停
        private bool m_Pause;

        /// 获取或设置界面组所在的层级。
        public UILayer Layer { get; private set; }

        /// 界面组内的界面列表
        private readonly FuLinkedList<UIInfo> m_UIInfoList = new();


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
        public void Init(UILayer layer)
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
                if (!current.Value.Paused && current.Value.View.Visible) // 只更新未暂停且可见的界面
                {
                    current.Value.View._OnUpdate(deltaTime, unscaledDeltaTime);
                }

                current = current.Next; // 继续处理下一个界面
            }
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUI(int serialId) => m_UIInfoList.Any(uiInfo => uiInfo.View.SerialId == serialId);

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUI(string uiName)
        {
            if (string.IsNullOrEmpty(uiName)) throw new FuException("[UIGroup] 传入的UI界面资源名称为空.");
            return m_UIInfoList.Any(uiInfo => uiInfo.View.UIName == uiName);
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(int serialId)
        {
            return (from uiInfo in m_UIInfoList where uiInfo.View.SerialId == serialId select uiInfo.View).FirstOrDefault();
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
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));
            return (from uiInfo in m_UIInfoList where uiInfo.View.UIName == uiName select uiInfo.View).FirstOrDefault();
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public T[] GetUIs<T>(string uiName) where T : ViewBase
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));
            return (from uiInfo in m_UIInfoList where uiInfo.View.UIName == uiName select uiInfo.View as T).ToArray();
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIs(string uiName, List<ViewBase> results)
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));
            FuGuard.NotNull(results, nameof(results));

            results.Clear();
            results.AddRange(from uiInfo in m_UIInfoList where uiInfo.View.UIName == uiName select uiInfo.View);
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <returns>界面组中的所有界面。</returns>
        public ViewBase[] GetAllUIs()
        {
            return m_UIInfoList.Select(uiInfo => uiInfo.View).ToArray();
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <param name="results">界面组中的所有界面。</param>
        public void GetAllUIs(List<ViewBase> results)
        {
            FuGuard.NotNull(results, nameof(results));
            results.Clear();
            results.AddRange(m_UIInfoList.Select(uiInfo => uiInfo.View));
        }

        /// <summary>
        /// 往界面组增加界面。
        /// </summary>
        /// <param name="view">要增加的界面。</param>
        public void AddUI(ViewBase view)
        {
            m_UIInfoList.AddFirst(UIInfo.Create(view));
        }

        /// <summary>
        /// 从界面组移除界面。
        /// </summary>
        /// <param name="view">要移除的界面。</param>
        public void RemoveUI(ViewBase view)
        {
            var uiInfo = GetUIInfo(view);
            if (uiInfo == null)
                throw new FuException($"[UIGroup] 无法找到界面id为 '{view.SerialId}' ，资源名称为 '{view.UIName}' 的UI界面信息.");

            if (!m_UIInfoList.Remove(uiInfo))
                throw new FuException($"[UIGroup] UI组 '{Layer.ToString()}' 中不存在UI界面 '[{view.SerialId}]{view.UIName}'.");

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
            var isPause = m_Pause; // 是否暂停的标志(来自成员变量)
            var depth   = UICount; //初始深度值(从界面数量开始递减)

            while (current is { Value: not null })
            {
                // 预先获取下一个节点（因为当前节点可能在处理过程中被移除）
                var next = current.Next;

                // 通知界面深度变化（使用逆序深度分配，第一个元素深度值最大）
                current.Value.View._OnDepthChanged(depth--);

                if (current.Value == null) return; // 可能在回调中被销毁，所有这里判断下

                // 暂停状态下的处理逻辑，第一个界面不会走到这里，只有第二个及以后的界面才会被暂停
                if (isPause)
                {
                    if (!current.Value.Covered)
                    {
                        current.Value.Covered = true;
                        current.Value.View._OnBeCover(); // 触发被覆盖回调
                        if (current.Value == null) return;
                    }

                    if (!current.Value.Paused)
                    {
                        current.Value.Paused = true;
                        current.Value.View._OnPause(); // 触发暂停回调
                        if (current.Value == null) return;
                    }
                }

                // 正常状态下的处理逻辑
                else
                {
                    if (current.Value.Paused)
                    {
                        current.Value.Paused = false;
                        current.Value.View._OnResume(); // 触发恢复回调
                        if (current.Value == null) return;
                    }

                    // 如果当前界面要求暂停被覆盖的界面，则后续界面进入暂停状态
                    if (current.Value.View.PauseCoveredUI)
                        isPause = true;

                    if (isCover)
                    {
                        // 需要覆盖后续界面，第一个界面不会走到这里，只有第二个及以后的界面才会被暂停
                        if (!current.Value.Covered)
                        {
                            current.Value.Covered = true;
                            current.Value.View._OnBeCover(); // 触发被覆盖回调
                            if (current.Value == null) return;
                        }
                    }
                    else
                    {
                        if (current.Value.Covered)
                        {
                            current.Value.Covered = false;
                            current.Value.View._OnReveal(); // 触发重新显示回调
                            if (current.Value == null) return;
                        }

                        isCover = true; // 后续界面需要被覆盖
                    }
                }

                current = next; // 移动到下一个节点
            }
        }

        /// <summary>
        /// 检查界面组中是否存在指定界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <param name="view">要检查的界面。</param>
        /// <returns>是否存在指定界面。</returns>
        public bool InternalHasUI(string uiName, ViewBase view)
        {
            return m_UIInfoList.Any(uiInfo => uiInfo.View.UIName == uiName && uiInfo.View == view);
        }

        /// <summary>
        /// 获取UI界面的界面信息。
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        private UIInfo GetUIInfo(ViewBase view)
        {
            FuGuard.NotNull(view, nameof(view));
            return m_UIInfoList.FirstOrDefault(uiInfo => uiInfo.View == view);
        }
    }
}