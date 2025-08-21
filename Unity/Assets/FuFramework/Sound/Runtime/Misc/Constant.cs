// ReSharper disable once CheckNamespace

namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 声音相关常量。
    /// </summary>
    internal static class Constant
    {
        /// <summary>
        /// 默认播放位置(以秒为单位)。
        /// </summary>
        internal const float DefaultTime = 0f;

        /// <summary>
        /// 默认是否静音。
        /// </summary>
        internal const bool DefaultMute = false;

        /// <summary>
        /// 默认是否循环播放。
        /// </summary>
        internal const bool DefaultLoop = false;

        /// <summary>
        /// 默认优先级。
        /// </summary>
        internal const int DefaultPriority = 0;

        /// <summary>
        /// 默认音量。
        /// </summary>
        internal const float DefaultVolume = 1f;

        /// <summary>
        /// 默认声音淡入时间，以秒为单位。
        /// </summary>
        internal const float DefaultFadeInSeconds = 0f;

        /// <summary>
        /// 默认声音淡出时间，以秒为单位。
        /// </summary>
        internal const float DefaultFadeOutSeconds = 0f;

        /// <summary>
        /// 默认声音音调。
        /// </summary>
        internal const float DefaultPitch = 1f;

        /// <summary>
        /// 默认声音立体声声相。
        /// </summary>
        internal const float DefaultPanStereo = 0f;

        /// <summary>
        /// 默认声音空间混合量。
        /// </summary>
        internal const float DefaultSpatialBlend = 0f;

        /// <summary>
        /// 默认声音最大距离。
        /// </summary>
        internal const float DefaultMaxDistance = 100f;

        /// <summary>
        /// 默认声音多普勒等级。
        /// </summary>
        internal const float DefaultDopplerLevel = 1f;
    }
}