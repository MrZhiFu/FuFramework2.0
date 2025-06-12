//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 界面管理器.UI分组管理器
    /// </summary>
    public sealed partial class UIManager
    {
        /// <summary>
        /// 界面组字典。key为组名称，value为组对象。
        /// </summary>
        private Dictionary<string, UIGroup> m_UIGroupDict;

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount => m_UIGroupDict.Count;

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(string uiGroupName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiGroupName, nameof(uiGroupName));
            return m_UIGroupDict.ContainsKey(uiGroupName);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public UIGroup GetUIGroup(string uiGroupName)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiGroupName, nameof(uiGroupName));
            return m_UIGroupDict.TryGetValue(uiGroupName, out var uiGroup) ? uiGroup : null;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public UIGroup[] GetAllUIGroups()
        {
            var index = 0;
            var results = new UIGroup[m_UIGroupDict.Count];
            foreach (var (_, group) in m_UIGroupDict)
            {
                results[index++] = group;
            }

            return results;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        public void GetAllUIGroups(List<UIGroup> results)
        {
            GameFrameworkGuard.NotNull(results, nameof(results));

            results.Clear();
            foreach (var (_, group) in m_UIGroupDict)
            {
                results.Add(group);
            }
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName, int uiGroupDepth = 0)
        {
            GameFrameworkGuard.NotNullOrEmpty(uiGroupName, nameof(uiGroupName));
            if (HasUIGroup(uiGroupName)) return false;

            var component = new UIGroup();
            GRoot.inst.AddChild(component);
            component.displayObject.name = uiGroupName;
            component.gameObjectName = uiGroupName;
            component.name = uiGroupName;
            component.opaque = false;

            component.AddRelation(GRoot.inst, RelationType.Width);
            component.AddRelation(GRoot.inst, RelationType.Height);
            component.MakeFullScreen();
            component.Init(uiGroupName, uiGroupDepth);
            m_UIGroupDict.Add(uiGroupName, component);
            return true;
        }
    }
}