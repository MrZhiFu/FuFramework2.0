//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;

namespace GameFrameX.UI.Runtime
{
    /// <summary>
    /// 界面组辅助器接口。
    /// </summary>
    public interface IUIGroupHelper
    {
        /// <summary>
        /// 设置界面组深度。
        /// </summary>
        /// <param name="depth">界面组深度。</param>
        void SetDepth(int depth);

        /// <summary>
        /// 创建界面组。
        /// </summary>
        /// <param name="root">根节点对象</param>
        /// <param name="groupName">界面组名称。</param>
        /// <param name="uiGroupHelperTypeName">界面组辅助器类型名。</param>
        /// <param name="customUIGroupHelper">自定义的界面组辅助器.</param>
        IUIGroupHelper Handler(Transform root, string groupName, string uiGroupHelperTypeName, IUIGroupHelper customUIGroupHelper);
    }
}