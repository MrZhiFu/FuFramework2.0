using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ModuleSetting.Runtime
{
    /// <summary>
    /// 声音组，如背景音乐、音效等
    /// </summary>
    [Serializable]
    public class SoundGroupInfo
    {
        [SerializeField] private string m_GroupID;   // 唯一标识符
        [SerializeField] private string m_GroupName; // 声音组名称
        [SerializeField] private bool   m_IsMute;    // 是否静音

        [SerializeField, Range(0f, 1f)] private float m_Volume; // 音量大小

        [SerializeField] private int  m_AgentHelperCount;                 // 播放代理数量
        [SerializeField] private bool m_AllowBeingReplacedBySamePriority; // 是否允许被同优先级声音替换

        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string GroupID => m_GroupID;

        /// <summary>
        /// 声音组名称
        /// </summary>
        public string Name
        {
            get => m_GroupName;
            set => m_GroupName = value;
        }

        /// <summary>
        /// 是否静音
        /// </summary>
        public bool Mute
        {
            get => m_IsMute;
            set => m_IsMute = value;
        }

        /// <summary>
        /// 音量大小
        /// </summary>
        public float Volume
        {
            get => m_Volume;
            set => m_Volume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 声音代理辅助器数量
        /// </summary>
        public int AgentHelperCount
        {
            get => m_AgentHelperCount;
            set => m_AgentHelperCount = Mathf.Max(1, value);
        }

        /// <summary>
        /// 是否允许被同优先级声音替换
        /// </summary>
        public bool AllowBeingReplacedBySamePriority
        {
            get => m_AllowBeingReplacedBySamePriority;
            set => m_AllowBeingReplacedBySamePriority = value;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SoundGroupInfo(string groupName = "New Sound Group")
        {
            m_GroupID                          = Guid.NewGuid().ToString();
            m_GroupName                        = groupName;
            m_IsMute                           = false;
            m_Volume                           = 1f;
            m_AgentHelperCount                 = 1;
            m_AllowBeingReplacedBySamePriority = true;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void Reset()
        {
            m_GroupName                        = "New Sound Group";
            m_IsMute                           = false;
            m_Volume                           = 1f;
            m_AgentHelperCount                 = 1;
            m_AllowBeingReplacedBySamePriority = true;
        }
    }
}