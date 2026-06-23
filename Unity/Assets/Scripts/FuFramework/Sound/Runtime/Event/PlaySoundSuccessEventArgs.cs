using FuFramework.Event.Runtime;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放声音成功事件。
    /// </summary>
    public sealed class PlaySoundSuccessEventArgs : GameEventArgs
    {
        /// <summary>
        /// 获取事件编号。
        /// </summary>
        public override string Id => EventId;

        /// <summary>
        /// 播放声音成功事件编号。
        /// </summary>
        public static readonly string EventId = typeof(PlaySoundSuccessEventArgs).FullName;

        /// <summary>
        /// 声音的序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 声音资源名称。
        /// </summary>
        public string SoundAssetName { get; private set; }

        /// <summary>
        /// 用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 创建播放声音成功事件。
        /// </summary>
        /// <param name="serialId">声音的序列编号。</param>
        /// <param name="soundAssetName">声音资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的播放声音成功事件。</returns>
        public static PlaySoundSuccessEventArgs Create(int serialId, string soundAssetName, object userData)
        {
            var playSoundSuccessEventArgs = ReferencePool.Runtime.ReferencePool.Acquire<PlaySoundSuccessEventArgs>();
            playSoundSuccessEventArgs.SerialId       = serialId;
            playSoundSuccessEventArgs.SoundAssetName = soundAssetName;
            playSoundSuccessEventArgs.UserData       = userData;
            return playSoundSuccessEventArgs;
        }

        /// <summary>
        /// 清理播放声音成功事件。
        /// </summary>
        public override void Clear()
        {
            SerialId       = 0;
            SoundAssetName = null;
            UserData       = null;
        }
    }
}