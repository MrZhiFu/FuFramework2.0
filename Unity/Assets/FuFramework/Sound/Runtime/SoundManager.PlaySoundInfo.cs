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
        public class PlaySoundInfo : IReference
        {
            /// <summary>
            /// 声音序列编号。
            /// </summary>
            public int SerialId { get; private set; }

            /// <summary>
            /// 声音名称。
            /// </summary>
            public string SoundName { get; private set; }

            /// <summary>
            /// 声音资源。
            /// </summary>
            public object SoundAsset { get; private set; }

            /// <summary>
            /// 所在声音组。
            /// </summary>
            public SoundGroup SoundGroup { get; private set; }

            /// <summary>
            /// 播放时的声音参数。
            /// </summary>
            public PlaySoundParams PlaySoundParams { get; private set; }

            /// <summary>
            /// 播放时的3D声音参数。
            /// </summary>
            public PlaySoundParams3D PlaySoundParams3D { get; private set; }

            /// <summary>
            /// 获取用户自定义数据。
            /// </summary>
            public object UserData { get; private set; }

            /// <summary>
            /// 创建播放声音信息。
            /// </summary>
            /// <param name="serialId">序列编号。</param>
            /// <param name="soundName">声音名称。</param>
            /// <param name="soundAsset">声音资源对象。</param>
            /// <param name="soundGroup">所在声音组。</param>
            /// <param name="playSoundParams">播放声音参数。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>创建的播放声音信息。</returns>
            public static PlaySoundInfo Create(int serialId, string soundName, object soundAsset, SoundGroup soundGroup, PlaySoundParams playSoundParams, PlaySoundParams3D playSoundParams3D,
                                               object userData)
            {
                var playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo.SerialId          = serialId;
                playSoundInfo.SoundName         = soundName;
                playSoundInfo.SoundAsset        = soundAsset;
                playSoundInfo.SoundGroup        = soundGroup;
                playSoundInfo.PlaySoundParams   = playSoundParams;
                playSoundInfo.PlaySoundParams3D = playSoundParams3D;
                playSoundInfo.UserData          = userData;
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
                PlaySoundParams3D = null;
                UserData        = null;
            }
        }
    }
}