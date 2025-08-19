using UnityEngine;
using FuFramework.Core.Runtime;
using ReferencePool = FuFramework.Core.Runtime.ReferencePool;

// ReSharper disable once CheckNamespace
namespace FuFramework.Sound.Runtime
{
    /// <summary>
    /// 播放时的声音额外信息。包括绑定的实体、声音在世界空间中的位置、用户自定义数据。
    /// </summary>
    internal sealed class PlaySoundInfoExtra : IReference
    {
        /// <summary>
        /// 绑定的实体。
        /// </summary>
        public Entity.Runtime.Entity BindingEntity { get; private set; }

        /// <summary>
        /// 声音在世界空间中的位置。
        /// </summary>
        public Vector3 WorldPosition { get; private set; }

        /// <summary>
        /// 用户自定义数据。
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public object UserData { get; private set; }

        /// <summary>
        /// 初始化播放声音信息的新实例。
        /// </summary>
        public PlaySoundInfoExtra()
        {
            BindingEntity = null;
            WorldPosition = Vector3.zero;
            UserData      = null;
        }

        /// <summary>
        /// 创建播放声音额外信息。
        /// </summary>
        /// <param name="bindingEntity">绑定的实体。</param>
        /// <param name="worldPosition">声音在世界空间中的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的播放声音信息。</returns>
        public static PlaySoundInfoExtra Create(Entity.Runtime.Entity bindingEntity, Vector3 worldPosition, object userData)
        {
            var playSoundInfo = ReferencePool.Acquire<PlaySoundInfoExtra>();
            playSoundInfo.BindingEntity = bindingEntity;
            playSoundInfo.WorldPosition = worldPosition;
            playSoundInfo.UserData      = userData;
            return playSoundInfo;
        }

        /// <summary>
        /// 清理播放声音信息。
        /// </summary>
        public void Clear()
        {
            BindingEntity = null;
            UserData      = null;
            WorldPosition = Vector3.zero;
        }
    }
}