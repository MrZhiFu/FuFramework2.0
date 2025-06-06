using System.Collections.Generic;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="groupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(string groupName) => m_UIManager.HasUIGroup(groupName);

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="groupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(string groupName) => m_UIManager.GetUIGroup(groupName);

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups() => m_UIManager.GetAllUIGroups();

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        public void GetAllUIGroups(List<IUIGroup> results) => m_UIManager.GetAllUIGroups(results);

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="depth">界面组深度。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName, int depth = 0)
        {
            if (m_UIManager.HasUIGroup(uiGroupName)) return false;

            var uiGroupHelper = (UIGroupHelperBase)m_CustomUIGroupHelper.CreateGroup(transform, uiGroupName, m_UIGroupHelperTypeName, m_CustomUIGroupHelper);
            if (uiGroupHelper == null)
            {
                Log.Error($"创建界面组'{uiGroupName}'失败.");
                return false;
            }
            
            return m_UIManager.AddUIGroup(uiGroupName, depth, uiGroupHelper);
        }
    }
}