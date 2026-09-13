using System;
using FairyGUI;
using Hotfix.Framework.Core;
using System.Collections.Generic;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace 禁用命名空间检查
namespace Hotfix.Framework.UI
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
        private readonly FuLinkedList<WinInfo> m_UIInfoList = new();


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
        public WinBase CurrentWinBase => m_UIInfoList.First?.Value.Win;

        /// <summary>
        /// 初始化界面组的新实例。
        /// </summary>
        /// <param name="layer">界面组层级。</param>
        public void Init(EUILayer layer)
        {
            Layer   = layer;
            m_Pause = false;

            // WinInfo 经引用池 Acquire，唯一回收点是 UIGroup.Remove：若对非空列表直接 Clear 会丢弃这些实例，
            // 造成引用池泄漏。当前唯一调用方传入的是新建空组，这里仍按通用契约逐个回收后再清空。
            if (m_UIInfoList.Count > 0)
            {
                foreach (var uiInfo in m_UIInfoList)
                {
                    if (uiInfo != null) ReferencePool.Recycle(uiInfo);
                }

                m_UIInfoList.Clear();
            }

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
                // 先缓存下一个节点：win._OnUpdate 是用户代码，可能在其中关闭自身
                //（UIModule.Close → UIGroup.Remove → FuLinkedList.Remove 会 detach 本节点，
                // 使 current.Next 变为 null，并把节点回收进节点缓存队列供复用）。
                // 若在回调之后才读 current.Next，本帧该组后续窗口会被全部跳过，节点被复用后还可能跳进「新」节点。
                // 与 EntityGroup.Update 的 m_CachedNode 预取写法保持一致。
                var next = current.Next;

                // 当前节点可能在本帧更早的某个窗口回调里已被关闭（例如前一个窗口关闭了「下一个」窗口）：
                // FuLinkedList.Remove 会 detach 该节点并把它回收进节点缓存队列（_ReleaseNode 把 Value 置为
                // default=null），此时节点引用仍非 null 但 Value 已为空，直接解引用 uiInfo.Win 会抛
                // NullReferenceException。故先从 Value 取出一份再判空，本帧被关闭的节点一律跳过、不予驱动。
                var uiInfo = current.Value;

                // 先推进到回调前缓存的节点，保证回调中关闭自身/关闭下一个节点都不会漏掉本帧后续窗口
                current = next;

                if (uiInfo?.Win == null) continue;

                // 只更新未暂停且可见的界面
                if (!uiInfo.Paused && uiInfo.Win.Visible)
                {
                    uiInfo.Win._OnUpdate(deltaTime, unscaledDeltaTime);
                }
            }
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool Has(int serialId)
        {
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.Win.SerialId == serialId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <typeparam name="T">界面类型。</typeparam>
        /// <returns></returns>
        public bool Has<T>() where T : WinBase => Has(typeof(T).Name);

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="winName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool Has(string winName)
        {
            winName.NotNullOrEmpty(nameof(winName));
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.Win.WinName == winName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public WinBase Get(int serialId)
        {
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.Win.SerialId == serialId)
                {
                    return uiInfo.Win;
                }
            }

            return null;
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <returns>要获取的界面。</returns>
        public T Get<T>() where T : WinBase => (T)Get(typeof(T).Name);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="winName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public WinBase Get(string winName)
        {
            winName.NotNullOrEmpty(nameof(winName));
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.Win.WinName == winName)
                {
                    return uiInfo.Win;
                }
            }

            return null;
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <returns>界面组中的所有界面。</returns>
        public WinBase[] GetAll()
        {
            var result = new WinBase[UICount];

            var i = 0;
            foreach (var uiInfo in m_UIInfoList)
            {
                result[i++] = uiInfo.Win;
            }

            return result;
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <param name="results">界面组中的所有界面。</param>
        public void GetAll(List<WinBase> results)
        {
            results.NotNull(nameof(results));
            results.Clear();
            foreach (var uiInfo in m_UIInfoList)
            {
                results.Add(uiInfo.Win);
            }
        }

        /// <summary>
        /// 往界面组增加界面。
        /// </summary>
        /// <param name="win">要增加的界面。</param>
        public void Add(WinBase win)
        {
            if (Has(win.SerialId))
                throw new InvalidOperationException($"[UIGroup] UI组 '{Layer.ToString()}' 中已经存在UI界面 '[{win.SerialId}]{win.WinName}'.");

            var uiInfo = WinInfo.Create(win);
            m_UIInfoList.AddFirst(uiInfo);
        }

        /// <summary>
        /// 从界面组移除界面。
        /// </summary>
        /// <param name="win">要移除的界面。</param>
        public void Remove(WinBase win)
        {
            var uiInfo = GetInfo(win);
            if (uiInfo == null)
                throw new InvalidOperationException($"[UIGroup] 无法找到界面id为 '{win.SerialId}' ，资源名称为 '{win.WinName}' 的UI界面信息.");

            if (!m_UIInfoList.Remove(uiInfo))
                throw new InvalidOperationException($"[UIGroup] UI组 '{Layer.ToString()}' 中不存在UI界面 '[{win.SerialId}]{win.WinName}'.");

            // 释放界面信息实例
            ReferencePool.Recycle(uiInfo);
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

            while (current != null)
            {
                // 先缓存下一个节点：HandlePauseState/HandleCoverState 会触发 _OnPause/_OnResume/_OnBeCover/_OnReveal
                // 等界面回调，回调内可能关闭当前界面或后续界面（UIModule.Close → UIGroup.Remove → FuLinkedList.Remove
                // 会 detach 本节点并把节点回收进节点缓存队列供复用），使 current.Next 变为 null。
                // 若在回调之后才读 current.Next，本帧该组后续界面会被全部跳过，节点被复用后还可能跳进「新」节点。
                // 与 OnUpdate 的 m_CachedNode 预取写法保持一致。
                var next = current.Next;

                // 当前节点可能已被本帧更早的某个界面回调关闭：_ReleaseNode 会把 Value 置为 default=null，
                // 此时节点引用仍非 null 但 Value 已为空，直接解引用 uiInfo.Win 会抛 NullReferenceException。
                // 故先从 Value 取出一份再判空——Value 为空只跳过本节点，不能终止整轮刷新（否则本帧剩余界面漏刷）。
                var uiInfo = current.Value;

                // 先推进到回调前缓存的节点，保证回调里关闭自身/关闭下一个节点都不会漏掉本帧后续界面
                current = next;

                if (uiInfo?.Win == null) continue;

                // 处理被暂停的界面状态
                HandlePauseState(uiInfo, ref isPause);

                // 处理被覆盖的界面状态
                HandleCoverState(uiInfo, ref isCover);
            }
        }

        /// <summary>
        /// 处理被暂停的界面状态。
        /// 顶部的第一个界面不会走到暂停逻辑，只有第二个及以后的界面才会被暂停。
        /// </summary>
        /// <param name="viewInfo">界面信息。</param>
        /// <param name="isPause">是否暂停的标志。</param>
        private void HandlePauseState(WinInfo viewInfo, ref bool isPause)
        {
            // 先根据当前暂停状态执行暂停/恢复（第一个界面 isPause=false，不会触发暂停）
            if (isPause && !viewInfo.Paused)
            {
                viewInfo.Paused = true;
                viewInfo.Win._OnPause(); // 触发暂停回调
            }
            else if (!isPause && viewInfo.Paused)
            {
                viewInfo.Paused = false;
                viewInfo.Win._OnResume(); // 触发恢复回调
            }

            // 如果当前界面要求暂停被覆盖的界面，则后续界面进入暂停状态
            if (!isPause && viewInfo.Win.PauseCoveredUI)
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
        private void HandleCoverState(WinInfo viewInfo, ref bool isCover)
        {
            if (isCover && !viewInfo.Covered)
            {
                viewInfo.Covered = true;
                viewInfo.Win._OnBeCover(); // 触发被覆盖回调
            }
            else if (!isCover && viewInfo.Covered)
            {
                viewInfo.Covered = false;
                viewInfo.Win._OnReveal(); // 触发重新显示回调
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
        /// <param name="win">界面实例。</param>
        /// <returns>界面信息。</returns>
        private WinInfo GetInfo(WinBase win)
        {
            win.NotNull(nameof(win));
            foreach (var uiInfo in m_UIInfoList)
            {
                if (uiInfo.Win == win)
                    return uiInfo;
            }

            return null;
        }
    }
}