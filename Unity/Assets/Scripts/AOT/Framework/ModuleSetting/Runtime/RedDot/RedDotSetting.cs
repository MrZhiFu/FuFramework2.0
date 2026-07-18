using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace AOT.Framework.ModuleSetting.Runtime.RedDot
{
    /// <summary>
    /// 红点模块配置
    /// </summary>
    public class RedDotSetting : ScriptableObject
    {
        /// <summary>
        /// 根节点列表
        /// </summary>
        [SerializeReference] public List<RedDotNodeData> m_RootNodes = new();
    }
}