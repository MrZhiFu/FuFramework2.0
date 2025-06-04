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
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        private readonly Dictionary<string, UIGroup> m_UIGroups;

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount
        {
            get { return m_UIGroups.Count; }
        }

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(string uiGroupName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiGroupName, nameof(uiGroupName));
            return m_UIGroups.ContainsKey(uiGroupName);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(string uiGroupName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiGroupName, nameof(uiGroupName));

            if (m_UIGroups.TryGetValue(uiGroupName, out var uiGroup))
            {
                return uiGroup;
            }

            return null;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups()
        {
            int index = 0;
            IUIGroup[] results = new IUIGroup[m_UIGroups.Count];
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                results[index++] = uiGroup.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        public void GetAllUIGroups(List<IUIGroup> results)
        {
            GameFrameworkGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (KeyValuePair<string, UIGroup> uiGroup in m_UIGroups)
            {
                results.Add(uiGroup.Value);
            }
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName, IUIGroupHelper uiGroupHelper)
        {
            return AddUIGroup(uiGroupName, 0, uiGroupHelper);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName, int uiGroupDepth, IUIGroupHelper uiGroupHelper)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiGroupName, nameof(uiGroupName));
            GameFrameworkGuard.NotNull(uiGroupHelper, nameof(uiGroupHelper));
            if (HasUIGroup(uiGroupName))
            {
                return false;
            }

            m_UIGroups.Add(uiGroupName, new UIGroup(uiGroupName, uiGroupDepth, uiGroupHelper));

            return true;
        }
    }
}