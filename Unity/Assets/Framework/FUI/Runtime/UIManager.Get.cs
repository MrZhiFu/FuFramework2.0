//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
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
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUI(string uiAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiAssetName, nameof(uiAssetName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUI(uiAssetName)) continue;
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
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(string uiAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiAssetName, nameof(uiAssetName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                var ui = group.GetUI(uiAssetName);
                if (ui == null) continue;
                return ui;
            }

            return null;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase[] GetUIs(string uiAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiAssetName, nameof(uiAssetName));

            var results = new List<ViewBase>();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.AddRange(group.GetUIs(uiAssetName));
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取所有界面组下的界面。
        /// </summary>
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIs(string uiAssetName, List<ViewBase> results)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiAssetName, nameof(uiAssetName));
            GameFrameworkGuard.NotNull(results, nameof(results));
            
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
            GameFrameworkGuard.NotNull(results, nameof(results));

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
            var index = 0;
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
            GameFrameworkGuard.NotNull(results, nameof(results));

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
        /// <param name="uiAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUI(string uiAssetName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiAssetName, nameof(uiAssetName));
            return m_LoadingDict.ContainsValue(uiAssetName);
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

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="fullName">完整界面名称</param>
        /// <returns></returns>
        public bool HasUIFullName(string fullName)
        {
            GameFrameworkGuard.NotNullOrEmpty(fullName, nameof(fullName));

            foreach (var (_, group) in m_UIGroupDict)
            {
                if (!group.HasUIFullName(fullName)) continue;
                return true;
            }

            return false;
        }
    }
}