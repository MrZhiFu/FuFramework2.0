using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放声音参数。
    /// </summary>
    public sealed class PlaySoundParams : IReference
    {
        /// <summary>
        /// 获取或设置播放位置。
        /// </summary>
        public float Time { get; private set; }

        /// <summary>
        /// 获取或设置在声音组内是否静音。
        /// </summary>
        public bool MuteInSoundGroup { get; private set; }

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
        public float VolumeInSoundGroup { get; private set; }

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
        /// 获取是否被引用。
        /// </summary>
        internal bool Referenced { get; private set; }

        /// <summary>
        /// 初始化播放声音参数的新实例。
        /// </summary>
        public PlaySoundParams()
        {
            Referenced         = false;
            Time               = Constant.DefaultTime;
            MuteInSoundGroup   = Constant.DefaultMute;
            Loop               = Constant.DefaultLoop;
            Priority           = Constant.DefaultPriority;
            VolumeInSoundGroup = Constant.DefaultVolume;
            FadeInSeconds      = Constant.DefaultFadeInSeconds;
            Pitch              = Constant.DefaultPitch;
            PanStereo          = Constant.DefaultPanStereo;
            SpatialBlend       = Constant.DefaultSpatialBlend;
            MaxDistance        = Constant.DefaultMaxDistance;
            DopplerLevel       = Constant.DefaultDopplerLevel;
        }

        /// <summary>
        /// 创建播放声音参数。
        /// </summary>
        /// <param name="isLoop">是否循环播放。</param>
        /// <returns>创建的播放声音参数。</returns>
        public static PlaySoundParams Create(bool isLoop = false)
        {
            var playSoundParams = ReferencePool.Acquire<PlaySoundParams>();
            playSoundParams.Referenced = true;
            playSoundParams.Loop       = isLoop;
            return playSoundParams;
        }

        /// <summary>
        /// 清理播放声音参数。
        /// </summary>
        public void Clear()
        {
            Time               = Constant.DefaultTime;
            MuteInSoundGroup   = Constant.DefaultMute;
            Loop               = Constant.DefaultLoop;
            Priority           = Constant.DefaultPriority;
            VolumeInSoundGroup = Constant.DefaultVolume;
            FadeInSeconds      = Constant.DefaultFadeInSeconds;
            Pitch              = Constant.DefaultPitch;
            PanStereo          = Constant.DefaultPanStereo;
            SpatialBlend       = Constant.DefaultSpatialBlend;
            MaxDistance        = Constant.DefaultMaxDistance;
            DopplerLevel       = Constant.DefaultDopplerLevel;
        }
    }
}