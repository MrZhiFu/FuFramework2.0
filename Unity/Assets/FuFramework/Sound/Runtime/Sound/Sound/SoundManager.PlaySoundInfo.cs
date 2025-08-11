//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

namespace GameFrameX.Sound.Runtime
{
    public sealed partial class SoundManager : FuModule, ISoundManager
    {
        /// <summary>
        /// 播放声音信息。
        /// </summary>
        private sealed class PlaySoundInfo : IReference
        {
            /// <summary>
            /// 序列编号。
            /// </summary>
            private int m_SerialId;

            /// <summary>
            /// 声音组。
            /// </summary>
            private SoundGroup m_SoundGroup;

            /// <summary>
            /// 播放声音参数。
            /// </summary>
            private PlaySoundParams m_PlaySoundParams;

            /// <summary>
            /// 用户自定义数据。
            /// </summary>
            private object m_UserData;

            /// <summary>
            /// 初始化播放声音信息的新实例。
            /// </summary>
            public PlaySoundInfo()
            {
                m_SerialId = 0;
                m_SoundGroup = null;
                m_PlaySoundParams = null;
                m_UserData = null;
            }

            /// <summary>
            /// 获取序列编号。
            /// </summary>
            public int SerialId
            {
                get
                {
                    return m_SerialId;
                }
            }

            /// <summary>
            /// 获取声音组。
            /// </summary>
            public SoundGroup SoundGroup
            {
                get
                {
                    return m_SoundGroup;
                }
            }

            /// <summary>
            /// 获取播放声音参数。
            /// </summary>
            public PlaySoundParams PlaySoundParams
            {
                get
                {
                    return m_PlaySoundParams;
                }
            }

            /// <summary>
            /// 获取用户自定义数据。
            /// </summary>
            public object UserData
            {
                get
                {
                    return m_UserData;
                }
            }

            /// <summary>
            /// 创建播放声音信息。
            /// </summary>
            /// <param name="serialId">序列编号。</param>
            /// <param name="soundGroup">声音组。</param>
            /// <param name="playSoundParams">播放声音参数。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的播放声音信息。</returns>
            public static PlaySoundInfo Create(int serialId, SoundGroup soundGroup, PlaySoundParams playSoundParams, object userData)
            {
                PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo.m_SerialId = serialId;
                playSoundInfo.m_SoundGroup = soundGroup;
                playSoundInfo.m_PlaySoundParams = playSoundParams;
                playSoundInfo.m_UserData = userData;
                return playSoundInfo;
            }

            /// <summary>
            /// 清理播放声音信息。
            /// </summary>
            public void Clear()
            {
                m_SerialId = 0;
                m_SoundGroup = null;
                m_PlaySoundParams = null;
                m_UserData = null;
            }
        }
    }
}
