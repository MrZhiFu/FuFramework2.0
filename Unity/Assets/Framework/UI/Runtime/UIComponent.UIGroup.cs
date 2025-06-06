//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.UI.Runtime
{
    public partial class UIComponent : GameFrameworkComponent
    {
        /// <summary>
        /// UI组。
        /// </summary>
        [Serializable]
        private sealed class UIGroup
        {
            [Header("组名")]
            [SerializeField] private string m_Name  = null;
            
            [Header("组深度")]
            [SerializeField] private int    m_Depth = 0;

            public string Name => m_Name;

            public int Depth => m_Depth;

            public UIGroup(int depth, string name)
            {
                m_Depth = depth;
                m_Name  = name;
            }
        }
    }
}