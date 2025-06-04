using System.Collections.Generic;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent
    {
        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(string uiGroupName)
        {
            return m_UIManager.HasUIGroup(uiGroupName);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(string uiGroupName)
        {
            return m_UIManager.GetUIGroup(uiGroupName);
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups()
        {
            return m_UIManager.GetAllUIGroups();
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        public void GetAllUIGroups(List<IUIGroup> results)
        {
            m_UIManager.GetAllUIGroups(results);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName)
        {
            return AddUIGroup(uiGroupName, 0);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="depth">界面组深度。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(string uiGroupName, int depth)
        {
            if (m_UIManager.HasUIGroup(uiGroupName))
            {
                return false;
            }

#if ENABLE_UI_FAIRYGUI
            UIGroupHelperBase uiGroupHelper = (UIGroupHelperBase)m_CustomUIGroupHelper.Handler(m_InstanceFairyGUIRoot, uiGroupName, m_UIGroupHelperTypeName, m_CustomUIGroupHelper);
#else
            UIGroupHelperBase uiGroupHelper = (UIGroupHelperBase)m_CustomUIGroupHelper.Handler(m_InstanceUGUIRoot, uiGroupName, m_UIGroupHelperTypeName, m_CustomUIGroupHelper);
#endif
            if (uiGroupHelper == null)
            {
                Log.Error("Can not create UI group helper.");
                return false;
            }
            // UIGroupHelperBase uiGroupHelper = Helper.CreateHelper(m_UIGroupHelperTypeName, m_CustomUIGroupHelper);
            // if (uiGroupHelper == null)
            // {
            //     Log.Error("Can not create UI group helper.");
            //     return false;
            // }

            /*
            uiGroupHelper.name = Utility.Text.Format("UI Group - {0}", uiGroupName);
            uiGroupHelper.gameObject.layer = LayerMask.NameToLayer("UI");
            Transform transform = uiGroupHelper.transform;
            transform.SetParent(m_InstanceRoot);
            transform.localScale = Vector3.one;*/

            return m_UIManager.AddUIGroup(uiGroupName, depth, uiGroupHelper);
        }
    }
}