using System.Collections.Generic;
using FairyGUI;
using GameFrameX.Runtime;

namespace FuFramework.UI.Runtime
{
    /// <summary>
    /// 界面管理器.UI分组管理器
    /// </summary>
    public sealed partial class UIManager
    {
        /// <summary>
        /// 界面组字典。key为组名称，value为组对象。
        /// </summary>
        private Dictionary<UILayer, UIGroup> m_UIGroupDict;

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount => m_UIGroupDict.Count;

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="layer">界面组层级。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(UILayer layer) => m_UIGroupDict.ContainsKey(layer);

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="layer">界面组层级。</param>
        /// <returns>要获取的界面组。</returns>
        public UIGroup GetUIGroup(UILayer layer)
        {
            return m_UIGroupDict.TryGetValue(layer, out var uiGroup) ? uiGroup : null;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public UIGroup[] GetAllUIGroups()
        {
            var index   = 0;
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
        /// <param name="layer">界面组层级。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(UILayer layer)
        {
            if (HasUIGroup(layer))
            {
                Log.Warning($"[UIManager]UI界面组{layer}已存在!");
                return false;
            }

            var component = new UIGroup();
            GRoot.inst.AddChild(component);

            component.displayObject.name = layer.ToString();
            component.gameObjectName     = layer.ToString();
            component.name               = layer.ToString();
            component.opaque             = false;

            component.AddRelation(GRoot.inst, RelationType.Width);
            component.AddRelation(GRoot.inst, RelationType.Height);

            component.MakeFullScreen();
            component.Init(layer);

            m_UIGroupDict.Add(layer, component);
            return true;
        }
    }
}