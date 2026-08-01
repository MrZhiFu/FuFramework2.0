using System;
using YooAsset;
using Hotfix.Framework.ReferencePool;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Sound
{
    public sealed partial class SoundModule
    {
        /// <summary>
        /// 播放的声音信息。
        /// 功能：
        ///    1. 用于在加载声音资源时保存相关信息。
        /// </summary>
        public class PlaySoundInfo : IReference
        {
            /// <summary>
            /// 声音序列编号。
            /// </summary>
            public int SerialId { get; private set; }

            /// <summary>
            /// 声音资源全路径。
            /// </summary>
            public string SoundAssetPath { get; private set; }

            /// <summary>
            /// 声音资源。
            /// </summary>
            public object SoundAsset { get; private set; }

            /// <summary>
            /// 声音资源句柄。随播放实例流转到 SoundAgent，播放结束时释放；
            /// 未上代理（播放失败/加载中被丢弃）时由调用方释放。
            /// </summary>
            public AssetHandle SoundAssetHandle { get; private set; }

            /// <summary>
            /// 所在声音组。
            /// </summary>
            public SoundGroup SoundGroup { get; private set; }

            /// <summary>
            /// 播放时的声音参数。
            /// </summary>
            public SoundParams SoundParams { get; private set; }

            /// <summary>
            /// 播放时的3D声音参数。
            /// </summary>
            public SoundParams3D SoundParams3D { get; private set; }

            /// <summary>
            /// 播放结束时的回调。
            /// </summary>
            public Action OnPlayEnd { get; private set; }

            /// <summary>
            /// 用户自定义数据。
            /// </summary>
            public object UserData { get; private set; }

            /// <summary>
            /// 创建播放声音信息。
            /// </summary>
            /// <param name="serialId">序列编号。</param>
            /// <param name="soundName">声音资源全路径。</param>
            /// <param name="soundAsset">声音资源对象。</param>
            /// <param name="soundAssetHandle">声音资源句柄。</param>
            /// <param name="soundGroup">所在声音组。</param>
            /// <param name="soundParams">播放声音时的参数。</param>
            /// <param name="soundParams3D">播放3D声音时的参数。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <param name="onPlayEnd">播放结束时的回调。</param>
            /// <returns>创建的播放声音信息。</returns>
            public static PlaySoundInfo Create(int serialId, string soundName, object soundAsset, AssetHandle soundAssetHandle, SoundGroup soundGroup, SoundParams soundParams, SoundParams3D soundParams3D,
                                               object userData, Action onPlayEnd)
            {
                var playSoundInfo = GlobalModule.ReferencePoolModule.Acquire<PlaySoundInfo>();
                playSoundInfo.SerialId          = serialId;
                playSoundInfo.SoundAssetPath    = soundName;
                playSoundInfo.SoundAsset        = soundAsset;
                playSoundInfo.SoundAssetHandle  = soundAssetHandle;
                playSoundInfo.SoundGroup        = soundGroup;
                playSoundInfo.SoundParams       = soundParams;
                playSoundInfo.SoundParams3D     = soundParams3D;
                playSoundInfo.OnPlayEnd         = onPlayEnd;
                playSoundInfo.UserData          = userData;
                return playSoundInfo;
            }

            /// <summary>
            /// 清理播放声音信息。
            /// </summary>
            public void Clear()
            {
                SerialId          = 0;
                SoundAssetPath    = null;
                SoundAsset        = null;
                SoundAssetHandle  = null;
                SoundGroup        = null;
                SoundParams       = null;
                SoundParams3D     = null;
                OnPlayEnd         = null;
                UserData          = null;
            }
        }
    }
}
