// ReSharper disable once CheckNamespace

namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放声音错误码。
    /// </summary>
    public enum EPlaySoundErrorCode : byte
    {
        /// <summary>
        /// 未知错误。
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 声音组不存在。
        /// </summary>
        SoundGroupNotExist,

        /// <summary>
        /// 声音组没有声音代理。
        /// </summary>
        SoundGroupHasNoAgent,

        /// <summary>
        /// 播放声音因优先级低被忽略。
        /// </summary>
        IgnoredBecauseLowPriority,

        /// <summary>
        /// 设置声音资源到AudioSource组件上失败。
        /// </summary>
        SetSoundAssetFailure
    }
}