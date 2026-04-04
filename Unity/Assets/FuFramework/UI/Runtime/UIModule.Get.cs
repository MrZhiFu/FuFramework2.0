using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// UI管理器分部类之一。
    /// 职责：用于获取已加载的UI界面。
    /// 核心功能:
    /// 1. 判断是否存在界面。
    /// 2. 获取界面。
    /// 3. 获取顶部界面。
    /// </summary>
    public sealed partial class UIModule
    {
        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUI(int serialId)
        {
            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUI(serialId)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUI(string uiName)
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUI(uiName)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(int serialId)
        {
            foreach (var (_, group) in m_UIGroupDict)
            {
                var ui = group.GetUI(serialId);
                if (ui == null) continue;
                return ui;
            }

            return null;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <returns>要获取的界面。</returns>
        public T GetUI<T>() where T : ViewBase => (T)GetUI(typeof(T).Name);

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(string uiName)
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                var ui = group.GetUI(uiName);
                if (ui == null) continue;
                return ui;
            }

            return null;
        }

        /// <summary>
        /// 获取指定层级中最顶部的界面。
        /// </summary>
        /// <param name="uiLayer">界面层级，为null时返回所有层级中最顶部的界面。</param>
        /// <returns>最顶部的界面。</returns>
        public T GetTopUI<T>(UILayer? uiLayer = null) where T : ViewBase
        {
            // 获取指定层级的顶部界面
            if (uiLayer.HasValue)
            {
                if (m_UIGroupDict.TryGetValue(uiLayer.Value, out var group))
                {
                    return group.CurrentViewBase as T;
                }

                return null;
            }

            // 获取所有层级中最顶部的界面（层级值最大的）
            ViewBase topView  = null;
            var      maxLayer = int.MinValue;

            foreach (var (layer, group) in m_UIGroupDict)
            {
                var layerValue = (int)layer;
                if (layerValue > maxLayer && group.CurrentViewBase != null)
                {
                    maxLayer = layerValue;
                    topView  = group.CurrentViewBase;
                }
            }

            return topView as T;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        public ViewBase[] GetAllLoadedUIs()
        {
            var results = new List<ViewBase>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIs());
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        public void GetAllLoadedUIs(List<ViewBase> results)
        {
            FuGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIs());
            }
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUISerialIds()
        {
            var index   = 0;
            var results = new int[m_LoadingDict.Count];
            foreach (var (id, _) in m_LoadingDict)
            {
                results[index++] = id;
            }

            return results;
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载界面的序列编号。</param>
        public void GetAllLoadingUISerialIds(List<int> results)
        {
            FuGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (id, _) in m_LoadingDict)
            {
                results.Add(id);
            }
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUI(int serialId) => m_LoadingDict.ContainsKey(serialId);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUI(string uiName) => m_LoadingDict.ContainsValue(uiName);
    }
}