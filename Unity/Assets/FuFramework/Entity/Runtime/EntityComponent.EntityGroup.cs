using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public sealed partial class EntityComponent
    {
        /// <summary>
        /// 实体组基本信息。
        /// 用于在Inspector中添加/编辑实体组信息。
        /// </summary>
        [Serializable]
        private sealed class EntityGroup
        {
            [Header("实体组名称")]
            [SerializeField] private string m_Name;

            [Header("实体组内实体对象池自动释放间隔")]
            [SerializeField] private float m_InstanceAutoReleaseInterval = 60f;

            [Header("实体组内实体对象池容量")]
            [SerializeField] private int m_InstanceCapacity = 16;

            [Header("实体组内实体对象过期时间")]
            [SerializeField] private float m_InstanceExpireTime = 60f;

            [Header("实体组内实体对象优先级")]
            [SerializeField] private int m_InstancePriority;

            public string Name => m_Name;

            public float InstanceAutoReleaseInterval => m_InstanceAutoReleaseInterval;

            public int InstanceCapacity => m_InstanceCapacity;

            public float InstanceExpireTime => m_InstanceExpireTime;

            public int InstancePriority => m_InstancePriority;
        }
    }
}