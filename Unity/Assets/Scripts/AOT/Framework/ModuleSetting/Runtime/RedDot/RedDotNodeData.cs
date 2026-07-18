using System;
using UnityEngine;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Runtime.RedDot
{
    /// <summary>
    /// 红点节点数据
    /// </summary>
    [Serializable]
    public class RedDotNodeData
    {
        /// <summary>
        /// 红点key
        /// </summary>
        public string m_Key;

        /// <summary>
        /// 该红点子下的子节点数据
        /// </summary>
        [SerializeReference] public List<RedDotNodeData> m_Children = new();
    }
}