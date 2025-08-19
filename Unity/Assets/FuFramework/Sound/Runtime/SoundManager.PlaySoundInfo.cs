using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    public sealed partial class SoundManager
    {
        /// <summary>
        /// 播放的声音信息。
        /// 功能：用于在加载声音资源时保存相关信息。
        /// </summary>
        private class PlaySoundInfo : IReference
        {
            /// <summary>
            /// 获取序列编号。
            /// </summary>
            public int SerialId { get; private set; }

            /// <summary>
            /// 获取声音组。
            /// </summary>
            public SoundGroup SoundGroup { get; private set; }

            /// <summary>
            /// 获取播放声音参数。
            /// </summary>
            public PlaySoundParams PlaySoundParams { get; private set; }

            /// <summary>
            /// 获取用户自定义数据。
            /// </summary>
            public object UserData { get; private set; }

            /// <summary>
            /// 初始化播放声音信息的新实例。
            /// </summary>
            public PlaySoundInfo()
            {
                SerialId        = 0;
                SoundGroup      = null;
                PlaySoundParams = null;
                UserData        = null;
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
                var playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo.SerialId        = serialId;
                playSoundInfo.SoundGroup      = soundGroup;
                playSoundInfo.PlaySoundParams = playSoundParams;
                playSoundInfo.UserData        = userData;
                return playSoundInfo;
            }

            /// <summary>
            /// 清理播放声音信息。
            /// </summary>
            public void Clear()
            {
                SerialId        = 0;
                SoundGroup      = null;
                PlaySoundParams = null;
                UserData        = null;
            }
        }
    }
}