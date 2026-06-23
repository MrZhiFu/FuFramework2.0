using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 实体组信息
    /// </summary>
    [Serializable]
    public class EntityGroupInfo
    {
        [SerializeField] private string m_GroupID; // 唯一标识符
        [SerializeField] private string m_Name; // 实体组名称

        [SerializeField] private float m_InstanceAutoReleaseInterval = 60f; // 实体组内实体对象池自动释放间隔

        [SerializeField] private int m_InstanceCapacity = 16; // 实体组内实体对象池容量

        [SerializeField] private float m_InstanceExpireTime = 60f; // 实体组内实体对象过期时间

        [SerializeField] private int m_InstancePriority; // 实体组内实体对象优先级

        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string GroupID => m_GroupID;

        /// <summary>
        /// 实体组名称
        /// </summary>
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }

        /// <summary>
        /// 实体组内实体对象池自动释放间隔
        /// </summary>
        public float InstanceAutoReleaseInterval
        {
            get => m_InstanceAutoReleaseInterval;
            set => m_InstanceAutoReleaseInterval = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 实体组内实体对象池容量
        /// </summary>
        public int InstanceCapacity
        {
            get => m_InstanceCapacity;
            set => m_InstanceCapacity = Mathf.Max(0, value);
        }

        /// <summary>
        /// 实体组内实体对象过期时间
        /// </summary>
        public float InstanceExpireTime
        {
            get => m_InstanceExpireTime;
            set => m_InstanceExpireTime = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 实体组内实体对象优先级
        /// </summary>
        public int InstancePriority
        {
            get => m_InstancePriority;
            set => m_InstancePriority = Mathf.Max(0, value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public EntityGroupInfo(string groupName = "New Entity Group")
        {
            m_GroupID = Guid.NewGuid().ToString();
            m_Name = groupName;
            m_InstanceAutoReleaseInterval = 60f;
            m_InstanceCapacity = 16;
            m_InstanceExpireTime = 60f;
            m_InstancePriority = 0;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            m_Name = "New Entity Group";
            m_InstanceAutoReleaseInterval = 60f;
            m_InstanceCapacity = 16;
            m_InstanceExpireTime = 60f;
            m_InstancePriority = 0;
        }
    }
}