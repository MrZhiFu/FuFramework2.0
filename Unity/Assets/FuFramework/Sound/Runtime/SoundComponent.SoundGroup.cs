using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    public sealed partial class SoundComponent
    {
        /// <summary>
        /// 声音组信息。
        /// </summary>
        [Serializable]
        private sealed class SoundGroup
        {
            /// <summary>
            /// 声音组名称。
            /// </summary>
            [SerializeField] private string m_Name;

            /// <summary>
            /// 是否避免被同优先级声音替换。
            /// </summary>
            [SerializeField] private bool m_AvoidBeingReplacedBySamePriority;

            /// <summary>
            /// 是否静音。
            /// </summary>
            [SerializeField] private bool m_Mute;

            /// <summary>
            /// 音量大小。
            /// </summary>
            [SerializeField, Range(0f, 1f)] private float m_Volume = 1f;

            /// <summary>
            /// 声音代理辅助器数量。
            /// </summary>
            [SerializeField] private int m_AgentHelperCount = 1;

            /// <summary>
            /// 获取声音组名称。
            /// </summary>
            public string Name => m_Name;

            /// <summary>
            /// 获取是否避免被同优先级声音替换。
            /// </summary>
            public bool AvoidBeingReplacedBySamePriority => m_AvoidBeingReplacedBySamePriority;

            /// <summary>
            /// 获取是否静音。
            /// </summary>
            public bool Mute => m_Mute;

            /// <summary>
            /// 获取音量大小。
            /// </summary>
            public float Volume => m_Volume;

            /// <summary>
            /// 获取声音代理辅助器数量。
            /// </summary>
            public int AgentHelperCount => m_AgentHelperCount;
        }
    }
}