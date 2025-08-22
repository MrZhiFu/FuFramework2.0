using FuFramework.Event.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放声音失败事件。
    /// </summary>
    public sealed class PlaySoundFailureEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 播放声音失败事件编号。
        /// </summary>
        public static readonly string EventId = typeof(PlaySoundFailureEventArgs).FullName;

        /// <summary>
        /// 声音的序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 声音资源名称。
        /// </summary>
        public string SoundAssetName { get; private set; }

        /// <summary>
        /// 声音组名称。
        /// </summary>
        public string SoundGroupName { get; private set; }

        /// <summary>
        /// 错误码。
        /// </summary>
        public EPlaySoundErrorCode ErrorCode { get; private set; }


        /// <summary>
        /// 初始化播放声音失败事件的新实例。
        /// </summary>
        public PlaySoundFailureEventArgs()
        {
            SerialId       = 0;
            SoundAssetName = null;
            SoundGroupName = null;
            ErrorCode      = EPlaySoundErrorCode.Unknown;
        }

        /// <summary>
        /// 创建播放声音失败事件。
        /// </summary>
        /// <param name="serialId">声音的序列编号。</param>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="errorCode">错误码。</param>
        /// <returns>创建的播放声音失败事件。</returns>
        public static PlaySoundFailureEventArgs Create(int serialId, string soundAssetName, string soundGroupName, EPlaySoundErrorCode errorCode)
        {
            var playSoundFailureEventArgs = ReferencePool.Acquire<PlaySoundFailureEventArgs>();
            playSoundFailureEventArgs.SerialId       = serialId;
            playSoundFailureEventArgs.SoundAssetName = soundAssetName;
            playSoundFailureEventArgs.SoundGroupName = soundGroupName;
            playSoundFailureEventArgs.ErrorCode      = errorCode;
            return playSoundFailureEventArgs;
        }

        /// <summary>
        /// 清理播放声音失败事件。
        /// </summary>
        public override void Clear()
        {
            SerialId       = 0;
            SoundAssetName = null;
            SoundGroupName = null;
            ErrorCode      = EPlaySoundErrorCode.Unknown;
        }
    }
}