using System.Collections.Generic;
using FuFramework.Core.Runtime;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    public sealed partial class UIManager
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
        /// 获取最顶部界面。
        /// </summary>
        /// <returns></returns>
        public T GetTopUI<T>(UILayer? uiLayer = null) where T : ViewBase
        {
            foreach (var (layer, group) in m_UIGroupDict)
            {
                if (uiLayer == null)
                    return group.CurrentViewBase as T;
                
                if (layer != uiLayer.Value) continue;
                return group.CurrentViewBase as T;
            }

            return null;
        }

        /// <summary>
        /// 获取所有界面组下的该名称的界面。
        /// </summary>
        /// <param name="uiName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public T[] GetUIs<T>(string uiName) where T : ViewBase
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));

            var results = new List<T>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetUIs<T>(uiName));
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有界面组下的该类型界面。
        /// </summary>
        /// <param name="uiName">界面名称。</param>
        /// <param name="results">结果列表。</param>
        public void GetUIs(string uiName, List<ViewBase> results)
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));
            FuGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetAllUIs());
            }
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
        public bool IsLoadingUI(string uiName)
        {
            FuGuard.NotNullOrEmpty(uiName, nameof(uiName));
            return m_LoadingDict.ContainsValue(uiName);
        }

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="iuiBase">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUI(ViewBase iuiBase)
        {
            return iuiBase != null && HasUI(iuiBase.SerialId);
        }
    }
}