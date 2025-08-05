//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FuFramework.Core.Runtime;
using GameFrameX.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

namespace GameFrameX.Sound.Runtime
{
    /// <summary>
    /// 播放声音参数。
    /// </summary>
    public sealed class PlaySoundParams : IReference
    {
        /// <summary>
        /// 是否被引用。
        /// </summary>
        private bool m_Referenced;
        
        /// <summary>
        /// 播放位置。
        /// </summary>
        private float m_Time;
        
        /// <summary>
        /// 在声音组内是否静音。
        /// </summary>
        private bool m_MuteInSoundGroup;
        
        /// <summary>
        /// 是否循环播放。
        /// </summary>
        private bool m_Loop;
        
        /// <summary>
        /// 声音优先级。
        /// </summary>
        private int m_Priority;
        
        /// <summary>
        /// 在声音组内音量大小。
        /// </summary>
        private float m_VolumeInSoundGroup;
        
        /// <summary>
        /// 声音淡入时间，以秒为单位。
        /// </summary>
        private float m_FadeInSeconds;
        
        /// <summary>
        /// 声音音调。
        /// </summary>
        private float m_Pitch;
        
        /// <summary>
        /// 声音立体声声相。
        /// </summary>
        private float m_PanStereo;
        
        /// <summary>
        /// 声音空间混合量。
        /// </summary>
        private float m_SpatialBlend;
        
        /// <summary>
        /// 声音最大距离。
        /// </summary>
        private float m_MaxDistance;
        
        /// <summary>
        /// 声音多普勒等级。
        /// </summary>
        private float m_DopplerLevel;

        /// <summary>
        /// 初始化播放声音参数的新实例。
        /// </summary>
        public PlaySoundParams()
        {
            m_Referenced = false;
            m_Time = Constant.DefaultTime;
            m_MuteInSoundGroup = Constant.DefaultMute;
            m_Loop = Constant.DefaultLoop;
            m_Priority = Constant.DefaultPriority;
            m_VolumeInSoundGroup = Constant.DefaultVolume;
            m_FadeInSeconds = Constant.DefaultFadeInSeconds;
            m_Pitch = Constant.DefaultPitch;
            m_PanStereo = Constant.DefaultPanStereo;
            m_SpatialBlend = Constant.DefaultSpatialBlend;
            m_MaxDistance = Constant.DefaultMaxDistance;
            m_DopplerLevel = Constant.DefaultDopplerLevel;
        }

        /// <summary>
        /// 获取或设置播放位置。
        /// </summary>
        public float Time
        {
            get { return m_Time; }
            set { m_Time = value; }
        }

        /// <summary>
        /// 获取或设置在声音组内是否静音。
        /// </summary>
        public bool MuteInSoundGroup
        {
            get { return m_MuteInSoundGroup; }
            set { m_MuteInSoundGroup = value; }
        }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public bool Loop
        {
            get { return m_Loop; }
            set { m_Loop = value; }
        }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public int Priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
        }

        /// <summary>
        /// 获取或设置在声音组内音量大小。
        /// </summary>
        public float VolumeInSoundGroup
        {
            get { return m_VolumeInSoundGroup; }
            set { m_VolumeInSoundGroup = value; }
        }

        /// <summary>
        /// 获取或设置声音淡入时间，以秒为单位。
        /// </summary>
        public float FadeInSeconds
        {
            get { return m_FadeInSeconds; }
            set { m_FadeInSeconds = value; }
        }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public float Pitch
        {
            get { return m_Pitch; }
            set { m_Pitch = value; }
        }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public float PanStereo
        {
            get { return m_PanStereo; }
            set { m_PanStereo = value; }
        }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public float SpatialBlend
        {
            get { return m_SpatialBlend; }
            set { m_SpatialBlend = value; }
        }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public float MaxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; }
        }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public float DopplerLevel
        {
            get { return m_DopplerLevel; }
            set { m_DopplerLevel = value; }
        }

        /// <summary>
        /// 获取是否被引用。
        /// </summary>
        internal bool Referenced
        {
            get { return m_Referenced; }
        }

        /// <summary>
        /// 创建播放声音参数。
        /// </summary>
        /// <param name="isLoop">是否循环播放。</param>
        /// <returns>创建的播放声音参数。</returns>
        public static PlaySoundParams Create(bool isLoop = false)
        {
            PlaySoundParams playSoundParams = ReferencePool.Acquire<PlaySoundParams>();
            playSoundParams.m_Referenced = true;
            playSoundParams.m_Loop = isLoop;
            return playSoundParams;
        }

        /// <summary>
        /// 清理播放声音参数。
        /// </summary>
        public void Clear()
        {
            m_Time = Constant.DefaultTime;
            m_MuteInSoundGroup = Constant.DefaultMute;
            m_Loop = Constant.DefaultLoop;
            m_Priority = Constant.DefaultPriority;
            m_VolumeInSoundGroup = Constant.DefaultVolume;
            m_FadeInSeconds = Constant.DefaultFadeInSeconds;
            m_Pitch = Constant.DefaultPitch;
            m_PanStereo = Constant.DefaultPanStereo;
            m_SpatialBlend = Constant.DefaultSpatialBlend;
            m_MaxDistance = Constant.DefaultMaxDistance;
            m_DopplerLevel = Constant.DefaultDopplerLevel;
        }
    }
}