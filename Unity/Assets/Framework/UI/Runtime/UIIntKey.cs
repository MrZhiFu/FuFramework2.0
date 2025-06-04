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
    /// 界面整型主键。
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public sealed class UIIntKey : MonoBehaviour
    {
        [SerializeField] private int m_Key = 0;

        /// <summary>
        /// 获取或设置主键。
        /// </summary>
        public int Key
        {
            get { return m_Key; }
            set { m_Key = value; }
        }
    }
}