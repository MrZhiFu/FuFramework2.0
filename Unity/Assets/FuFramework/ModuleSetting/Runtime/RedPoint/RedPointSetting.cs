using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 红点模块配置
    /// </summary>
    public class RedPointSetting : ScriptableObject
    {
        /// <summary>
        /// 根节点列表
        /// </summary>
        [SerializeReference] public List<RedPointNodeData> m_RootNodes = new();
    }
}