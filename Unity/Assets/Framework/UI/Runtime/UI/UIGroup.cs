using System.Collections.Generic;
using System.Linq;
using GameFrameX.Runtime;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIGroup : IUIGroup
    {
        /// 获取界面组名称。
        public string Name { get; }

        /// 界面组深度
        private int m_Depth;

        /// 界面组是否暂停
        private bool m_Pause;

        /// 界面组辅助器。
        private readonly IUIGroupHelper m_UIGroupHelper;

        /// 界面组内的界面列表
        private readonly GameFrameworkLinkedList<UIFormInfo> m_UIFormInfos;

        /// 临时缓存界面节点
        private LinkedListNode<UIFormInfo> m_CachedNode;

        /// <summary>
        /// 初始化界面组的新实例。
        /// </summary>
        /// <param name="name">界面组名称。</param>
        /// <param name="depth">界面组深度。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        public UIGroup(string name, int depth, IUIGroupHelper uiGroupHelper)
        {
            if (string.IsNullOrEmpty(name)) throw new GameFrameworkException("传入的UI组名称为空.");

            Name            = name;
            m_Pause         = false;
            m_UIGroupHelper = uiGroupHelper ?? throw new GameFrameworkException("传入的界面组辅助器为空.");
            m_UIFormInfos   = new GameFrameworkLinkedList<UIFormInfo>();
            m_CachedNode    = null;
            Depth           = depth;
        }

        /// <summary>
        /// 获取或设置界面组深度。
        /// </summary>
        public int Depth
        {
            get => m_Depth;
            set
            {
                if (m_Depth == value) return;
                m_Depth = value;
                m_UIGroupHelper.SetDepth(m_Depth);
                Refresh();
            }
        }

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
        public int UIFormCount => m_UIFormInfos.Count;

        /// <summary>
        /// 获取当前界面。
        /// </summary>
        public IUIForm CurrentUIForm => m_UIFormInfos.First?.Value.UIForm;

        /// <summary>
        /// 获取界面组辅助器。
        /// </summary>
        public IUIGroupHelper Helper => m_UIGroupHelper;

        /// <summary>
        /// 界面组轮询。
        /// 遍历界面组中所有界面，驱动每个界面Update。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            var current = m_UIFormInfos.First;
            while (current != null)
            {
                if (current.Value.Paused) break;

                m_CachedNode = current.Next;
                current.Value.UIForm.OnUpdate(elapseSeconds, realElapseSeconds);
                current      = m_CachedNode;
                m_CachedNode = null;
            }
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUIForm(int serialId)
        {
            return m_UIFormInfos.Any(uiFormInfo => uiFormInfo.UIForm.SerialId == serialId);
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="fullName">界面资源完整名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUIFormFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) throw new GameFrameworkException("传入的UI界面完整名称为空.");
            return m_UIFormInfos.Any(uiFormInfo => uiFormInfo.UIForm.FullName == fullName);
        }

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        public bool HasUIForm(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName)) throw new GameFrameworkException("传入的UI界面资源名称为空.");
            return m_UIFormInfos.Any(uiFormInfo => uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName);
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUIForm(int serialId)
        {
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                if (uiFormInfo.UIForm.SerialId != serialId) continue;
                return uiFormInfo.UIForm;
            }

            return null;
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm GetUIForm(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName))
                throw new GameFrameworkException("传入的UI界面资源名称为空.");

            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                if (uiFormInfo.UIForm.UIFormAssetName != uiFormAssetName) continue;
                return uiFormInfo.UIForm;
            }

            return null;
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        public IUIForm[] GetUIForms(string uiFormAssetName)
        {
            if (string.IsNullOrEmpty(uiFormAssetName)) throw new GameFrameworkException("传入的UI界面资源名称为空.");

            var results = new List<IUIForm>();
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                if (uiFormInfo.UIForm.UIFormAssetName != uiFormAssetName) continue;
                results.Add(uiFormInfo.UIForm);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="results">要获取的界面。</param>
        public void GetUIForms(string uiFormAssetName, List<IUIForm> results)
        {
            if (string.IsNullOrEmpty(uiFormAssetName)) throw new GameFrameworkException("传入的UI界面资源名称为空.");
            if (results == null) throw new GameFrameworkException("传入的结果列表为空.");

            results.Clear();
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                if (uiFormInfo.UIForm.UIFormAssetName != uiFormAssetName) continue;
                results.Add(uiFormInfo.UIForm);
            }
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <returns>界面组中的所有界面。</returns>
        public IUIForm[] GetAllUIForms()
        {
            var results = new List<IUIForm>();
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                results.Add(uiFormInfo.UIForm);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 从界面组中获取所有界面。
        /// </summary>
        /// <param name="results">界面组中的所有界面。</param>
        public void GetAllUIForms(List<IUIForm> results)
        {
            if (results == null) throw new GameFrameworkException("传入的结果列表为空.");

            results.Clear();
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                results.Add(uiFormInfo.UIForm);
            }
        }

        /// <summary>
        /// 往界面组增加界面。
        /// </summary>
        /// <param name="uiForm">要增加的界面。</param>
        public void AddUIForm(IUIForm uiForm)
        {
            m_UIFormInfos.AddFirst(UIFormInfo.Create(uiForm));
        }

        /// <summary>
        /// 从界面组移除界面。
        /// </summary>
        /// <param name="uiForm">要移除的界面。</param>
        public void RemoveUIForm(IUIForm uiForm)
        {
            UIFormInfo uiFormInfo = GetUIFormInfo(uiForm);
            if (uiFormInfo == null)
            {
                throw new GameFrameworkException(Utility.Text.Format("Can not find UI form info for serial id '{0}', UI form asset name is '{1}'.", uiForm.SerialId, uiForm.UIFormAssetName));
            }

            if (!uiFormInfo.Covered)
            {
                uiFormInfo.Covered = true;
                uiForm.OnBeCover();
            }

            if (!uiFormInfo.Paused)
            {
                uiFormInfo.Paused = true;
                uiForm.OnPause();
            }

            if (m_CachedNode != null && m_CachedNode.Value.UIForm == uiForm)
            {
                m_CachedNode = m_CachedNode.Next;
            }

            if (!m_UIFormInfos.Remove(uiFormInfo))
            {
                throw new GameFrameworkException(Utility.Text.Format("UI group '{0}' not exists specified UI form '[{1}]{2}'.", Name, uiForm.SerialId, uiForm.UIFormAssetName));
            }

            ReferencePool.Release(uiFormInfo);
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUIForm(IUIForm uiForm, object userData)
        {
            UIFormInfo uiFormInfo = GetUIFormInfo(uiForm);
            if (uiFormInfo == null)
            {
                throw new GameFrameworkException("Can not find UI form info.");
            }

            m_UIFormInfos.Remove(uiFormInfo);
            m_UIFormInfos.AddFirst(uiFormInfo);
            uiFormInfo.UIForm.OnRefocus(userData);
        }

        /// <summary>
        /// 刷新界面组。
        /// </summary>
        public void Refresh()
        {
            // 从链表头部开始遍历
            var current = m_UIFormInfos.First;
           
            var isCover = false;       // 是否覆盖后面的界面，初始为false，表示第一个界面需要显示完整，后续界面需要被覆盖
            var isPause = m_Pause;     // 是否暂停的标志(来自成员变量)
            var depth   = UIFormCount; //初始深度值(从界面数量开始递减)

            while (current is { Value: not null })
            {
                // 预先获取下一个节点（因为当前节点可能在处理过程中被移除）
                var next = current.Next;
                
                // 通知界面深度变化（使用逆序深度分配，第一个元素深度值最大）
                current.Value.UIForm.OnDepthChanged(Depth, depth--);
                
                if (current.Value == null) return; // 可能在回调中被销毁，所有这里判断下

                // 暂停状态下的处理逻辑，第一个界面不会走到这里，只有第二个及以后的界面才会被暂停
                if (isPause)
                {
                    if (!current.Value.Covered)
                    {
                        current.Value.Covered = true;
                        current.Value.UIForm.OnBeCover();// 触发被覆盖回调
                        if (current.Value == null) return;
                    }

                    if (!current.Value.Paused)
                    {
                        current.Value.Paused = true;
                        current.Value.UIForm.OnPause();// 触发暂停回调
                        if (current.Value == null)
                        {
                            return;
                        }
                    }
                }
                
                // 正常状态下的处理逻辑
                else
                {
                    if (current.Value.Paused)
                    {
                        current.Value.Paused = false;
                        current.Value.UIForm.OnResume();// 触发恢复回调
                        if (current.Value == null) return;
                    }

                    // 如果当前界面要求暂停被覆盖的界面，则后续界面进入暂停状态
                    if (current.Value.UIForm.PauseCoveredUIForm) 
                        isPause = true;

                    if (isCover)
                    {
                        // 需要覆盖后续界面，第一个界面不会走到这里，只有第二个及以后的界面才会被暂停
                        if (!current.Value.Covered)
                        {
                            current.Value.Covered = true;
                            current.Value.UIForm.OnBeCover(); // 触发被覆盖回调
                            if (current.Value == null) return;
                        }
                    }
                    else
                    {
                        if (current.Value.Covered)
                        {
                            current.Value.Covered = false;
                            current.Value.UIForm.OnReveal(); // 触发重新显示回调
                            if (current.Value == null) return;
                        }

                        isCover = true; // 后续界面需要被覆盖
                    }
                }

                current = next; // 移动到下一个节点
            }
        }

        /// <summary>
        /// 检查界面组中是否存在指定界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiForm">要检查的界面。</param>
        /// <returns>是否存在指定界面。</returns>
        public bool InternalHasInstanceUIForm(string uiFormAssetName, IUIForm uiForm)
        {
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName && uiFormInfo.UIForm == uiForm)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取UI界面的界面信息。
        /// </summary>
        /// <param name="uiForm"></param>
        /// <returns></returns>
        /// <exception cref="GameFrameworkException"></exception>
        private UIFormInfo GetUIFormInfo(IUIForm uiForm)
        {
            if (uiForm == null) throw new GameFrameworkException("传入的UI界面为空.");
            foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
            {
                if (uiFormInfo.UIForm != uiForm) continue;
                return uiFormInfo;
            }

            return null;
        }
    }
}