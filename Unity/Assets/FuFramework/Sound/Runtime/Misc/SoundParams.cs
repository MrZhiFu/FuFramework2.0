using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放时的声音参数。
    /// 包括：播放位置(以秒为单位)、是否静音、是否循环播放、声音优先级、声音大小、声音淡入时间、声音音调、
    ///      声音立体声声相、声音空间混合量、声音最大距离、声音多普勒等级。
    /// </summary>
    public sealed class SoundParams : IReference
    {
        /// <summary>
        /// 获取或设置播放位置(以秒为单位)。
        /// </summary>
        public float Time { get; set; }

        /// <summary>
        /// 获取或设置在声音组内是否静音。
        /// </summary>
        public bool IsMute { get; set; }

        /// <summary>
        /// 获取或设置是否循环播放。
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// 获取或设置声音优先级。
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 获取或设置在声音组内音量大小。
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// 获取或设置声音淡入时间，以秒为单位。
        /// </summary>
        public float FadeInSeconds { get; set; }

        /// <summary>
        /// 获取或设置声音音调。
        /// </summary>
        public float Pitch { get; set; }

        /// <summary>
        /// 获取或设置声音立体声声相。
        /// </summary>
        public float PanStereo { get; set; }

        /// <summary>
        /// 获取或设置声音空间混合量。
        /// </summary>
        public float SpatialBlend { get; set; }

        /// <summary>
        /// 获取或设置声音最大距离。
        /// </summary>
        public float MaxDistance { get; set; }

        /// <summary>
        /// 获取或设置声音多普勒等级。
        /// </summary>
        public float DopplerLevel { get; set; }

        
        /// <summary>
        /// 初始化播放声音参数的新实例。
        /// </summary>
        public SoundParams()
        {
            Time          = 0;
            IsMute        = false;
            Loop          = false;
            Priority      = 0;
            Volume        = 1;
            FadeInSeconds = 0;
            Pitch         = 1;
            PanStereo     = 0;
            SpatialBlend  = 0;
            MaxDistance   = 100;
            DopplerLevel  = 1;
        }

        /// <summary>
        /// 创建播放声音参数。
        /// </summary>
        /// <returns>创建的播放声音参数。</returns>
        public static SoundParams Create() => ReferencePool.Acquire<SoundParams>();

        /// <summary>
        /// 清理播放声音参数。
        /// </summary>
        public void Clear()
        {
            Time          = 0;
            IsMute        = false;
            Loop          = false;
            Priority      = 0;
            Volume        = 1;
            FadeInSeconds = 0;
            Pitch         = 1;
            PanStereo     = 0;
            SpatialBlend  = 0;
            MaxDistance   = 100;
            DopplerLevel  = 1;
        }
    }
}