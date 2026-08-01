using UnityEngine;
using Hotfix.Framework.ReferencePool;
using Hotfix.Framework.Core;

namespace Hotfix.Framework.Sound
{
    /// <summary>
    /// 播放3D声音时的参数。
    /// 包括：绑定的实体、声音在世界空间中的位置、用户自定义数据。
    /// </summary>
    public sealed class SoundParams3D : IReference
    {
        /// <summary>
        /// 绑定的实体。
        /// </summary>
        public Entity.Entity BindingEntity { get; private set; }

        /// <summary>
        /// 声音在世界空间中的位置。
        /// </summary>
        public Vector3 WorldPosition { get; private set; } = Vector3.zero;

        /// <summary>
        /// 创建播放声音额外信息。
        /// </summary>
        /// <param name="bindingEntity">绑定的实体。</param>
        /// <param name="worldPosition">声音在世界空间中的位置。</param>
        /// <returns>创建的播放声音信息。</returns>
        public static SoundParams3D Create(Entity.Entity bindingEntity, Vector3 worldPosition)
        {
            var playSoundInfo = GlobalModule.ReferencePoolModule.Acquire<SoundParams3D>();
            playSoundInfo.BindingEntity = bindingEntity;
            playSoundInfo.WorldPosition = worldPosition;
            return playSoundInfo;
        }

        /// <summary>
        /// 清理播放声音信息。
        /// </summary>
        public void Clear()
        {
            BindingEntity = null;
            WorldPosition = Vector3.zero;
        }
    }
}
