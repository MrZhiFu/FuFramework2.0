using System.Collections.Generic;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public IUIBase GetUI(int serialId) => m_UIManager.GetUI(serialId);

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIBase GetUI(string uiAssetName) => m_UIManager.GetUI(uiAssetName);

        /// <summary>
        /// 获取界面名称对应的界面。
        /// (注意：一般为一个，如果有多个同名界面，则会返回所有同名界面)
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIBase[] GetUIs(string uiAssetName)
        {
            var uis     = m_UIManager.GetUIs(uiAssetName);
            var uiImpls = new IUIBase[uis.Length];
            for (var i = 0; i < uis.Length; i++)
            {
                uiImpls[i] = uis[i];
            }

            return uiImpls;
        }

        /// <summary>
        /// 获取界面名称对应的界面。
        /// (注意：一般为一个，如果有多个同名界面，则会返回所有同名界面)
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIs(string uiAssetName, List<IUIBase> results)
        {
            if (results == null)
            {
                Log.Error("传入的结果列表为空.");
                return;
            }

            results.Clear();
            m_UIManager.GetUIs(uiAssetName, m_InternalUIResults);
            results.AddRange(m_InternalUIResults);
        }

        /// <summary>
        /// 根据界面逻辑类型获取界面列表,会返回所有符合条件的集合
        /// </summary>
        /// <typeparam name="T">界面逻辑类型</typeparam>
        /// <returns></returns>
        public List<T> GetLoadedUIList<T>() where T : class, IUIBase
        {
            var fullName = typeof(T).FullName;
            var uis      = m_UIManager.GetAllLoadedUIs();
            var results  = new List<T>();
            foreach (var ui in uis)
            {
                if (ui.FullName != fullName) continue;
                results.Add(ui as T);
            }

            return results;
        }

        /// <summary>
        /// 获取已加载且正在显示的UI。
        /// </summary>
        /// <typeparam name="T">UI的具体类型。</typeparam>
        /// <returns>返回已加载且正在显示的UI实例，如果未找到则返回null。</returns>
        public T GetLoadedAndShowingUI<T>() where T : class, IUIBase
        {
            var fullName = typeof(T).FullName;
            var uis      = m_UIManager.GetAllLoadedUIs();
            foreach (var ui in uis)
            {
                if (ui.FullName != fullName || !ui.Visible) continue;
                return ui as T;
            }

            return null;
        }

        /// <summary>
        /// 根据界面逻辑类型获取界面。只要找到任意的一个即返回
        /// </summary>
        /// <typeparam name="T">逻辑界面类型</typeparam>
        /// <returns></returns>
        public T GetLoadedUI<T>() where T : class, IUIBase
        {
            var fullName = typeof(T).FullName;
            var uis      = m_UIManager.GetAllLoadedUIs();
            foreach (var ui in uis)
            {
                if (ui.FullName != fullName) continue;
                return ui as T;
            }

            return null;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <returns>所有已加载的界面。</returns>
        public IUIBase[] GetAllLoadedUIs()
        {
            var uis     = m_UIManager.GetAllLoadedUIs();
            var uiImpls = new IUIBase[uis.Length];
            for (var i = 0; i < uis.Length; i++)
            {
                uiImpls[i] = uis[i];
            }

            return uiImpls;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        /// <param name="results">所有已加载的界面。</param>
        public void GetAllLoadedUIs(List<IUIBase> results)
        {
            if (results == null)
            {
                Log.Error("传入的结果列表为空.");
                return;
            }

            results.Clear();
            m_UIManager.GetAllLoadedUIs(m_InternalUIResults);
            results.AddRange(m_InternalUIResults);
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUISerialIds() => m_UIManager.GetAllLoadingUISerialIds();

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <param name="results">所有正在加载界面的序列编号。</param>
        public void GetAllLoadingUISerialIds(List<int> results) => m_UIManager.GetAllLoadingUISerialIds(results);
    }
}