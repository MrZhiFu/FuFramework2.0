using System;
using FuFramework.ReferencePool.Runtime;

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
            /// 声音资源全路径。
            /// </summary>
            public string SoundAssetPath { get; private set; }

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
            /// <param name="soundGroup">所在声音组。</param>
            /// <param name="soundParams">播放声音时的参数。</param>
            /// <param name="soundParams3D">播放3D声音时的参数。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <param name="onPlayEnd">播放结束时的回调。</param>
            /// <returns>创建的播放声音信息。</returns>
            public static PlaySoundInfo Create(int serialId, string soundName, object soundAsset, SoundGroup soundGroup, SoundParams soundParams, SoundParams3D soundParams3D,
                                               object userData, Action onPlayEnd)
            {
                var playSoundInfo = ReferencePool.Runtime.ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo.SerialId       = serialId;
                playSoundInfo.SoundAssetPath = soundName;
                playSoundInfo.SoundAsset     = soundAsset;
                playSoundInfo.SoundGroup     = soundGroup;
                playSoundInfo.SoundParams    = soundParams;
                playSoundInfo.SoundParams3D  = soundParams3D;
                playSoundInfo.OnPlayEnd      = onPlayEnd;
                playSoundInfo.UserData       = userData;
                return playSoundInfo;
            }

            /// <summary>
            /// 清理播放声音信息。
            /// </summary>
            public void Clear()
            {
                SerialId       = 0;
                SoundAssetPath = null;
                SoundAsset     = null;
                SoundGroup     = null;
                SoundParams    = null;
                SoundParams3D  = null;
                OnPlayEnd      = null;
                UserData       = null;
            }
        }
    }
}