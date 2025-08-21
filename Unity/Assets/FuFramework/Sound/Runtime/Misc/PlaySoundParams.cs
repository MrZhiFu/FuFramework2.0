using FuFramework.Core.Runtime;
using UnityEngine;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放声音参数。
    /// 包括：播放位置(以秒为单位)、是否静音、是否循环播放、声音优先级、声音大小、声音淡入时间、声音音调、
    ///      声音立体声声相、声音空间混合量、声音最大距离、声音多普勒等级。
    /// </summary>
    public sealed class PlaySoundParams : IReference
    {
        /// <summary>
        /// 获取或设置播放位置(以秒为单位)。
        /// </summary>
        public float Time { get; private set; }

        /// <summary>
        /// 获取或设置在声音组内是否静音。
        /// </summary>
        public bool IsMute { get; private set; }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public bool Loop { get; private set; }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// 获取或设置在声音组内音量大小。
        /// </summary>
        public float Volume { get; private set; }

        /// <summary>
        /// 获取或设置声音淡入时间，以秒为单位。
        /// </summary>
        public float FadeInSeconds { get; private set; }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public float Pitch { get; private set; }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public float PanStereo { get; private set; }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public float SpatialBlend { get; private set; }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public float MaxDistance { get; private set; }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public float DopplerLevel { get; private set; }

        
        /// <summary>
        /// 初始化播放声音参数的新实例。
        /// </summary>
        public PlaySoundParams()
        {
            Time          = Constant.DefaultTime;
            IsMute        = Constant.DefaultMute;
            Loop          = Constant.DefaultLoop;
            Priority      = Constant.DefaultPriority;
            Volume        = Constant.DefaultVolume;
            FadeInSeconds = Constant.DefaultFadeInSeconds;
            Pitch         = Constant.DefaultPitch;
            PanStereo     = Constant.DefaultPanStereo;
            SpatialBlend  = Constant.DefaultSpatialBlend;
            MaxDistance   = Constant.DefaultMaxDistance;
            DopplerLevel  = Constant.DefaultDopplerLevel;
        }

        /// <summary>
        /// 创建播放声音参数。
        /// </summary>
        /// <param name="isLoop">是否循环播放。</param>
        /// <param name="priority">声音优先级。</param>
        /// <returns>创建的播放声音参数。</returns>
        public static PlaySoundParams Create(bool isLoop = false, int priority = Constant.DefaultPriority)
        {
            var playSoundParams = ReferencePool.Acquire<PlaySoundParams>();
            playSoundParams.Loop       = isLoop;
            playSoundParams.Priority      = priority;
            return playSoundParams;
        }

        /// <summary>
        /// 清理播放声音参数。
        /// </summary>
        public void Clear()
        {
            Time          = Constant.DefaultTime;
            IsMute        = Constant.DefaultMute;
            Loop          = Constant.DefaultLoop;
            Priority      = Constant.DefaultPriority;
            Volume        = Constant.DefaultVolume;
            FadeInSeconds = Constant.DefaultFadeInSeconds;
            Pitch         = Constant.DefaultPitch;
            PanStereo     = Constant.DefaultPanStereo;
            SpatialBlend  = Constant.DefaultSpatialBlend;
            MaxDistance   = Constant.DefaultMaxDistance;
            DopplerLevel  = Constant.DefaultDopplerLevel;
        }
    }
}