//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.Sound.Runtime
{
    /// <summary>
    /// 播放声音信息。
    /// </summary>
    internal sealed class PlaySoundInfo : IReference
    {
        /// <summary>
        /// 绑定的实体。
        /// </summary>
        private Entity.Runtime.Entity m_BindingEntity;

        /// <summary>
        /// 声音在世界空间中的位置。
        /// </summary>
        private Vector3 m_WorldPosition;

        /// <summary>
        /// 用户自定义数据。
        /// </summary>
        private object m_UserData;

        /// <summary>
        /// 初始化播放声音信息的新实例。
        /// </summary>
        public PlaySoundInfo()
        {
            m_BindingEntity = null;
            m_WorldPosition = Vector3.zero;
            m_UserData = null;
        }

        /// <summary>
        /// 获取绑定的实体。
        /// </summary>
        public Entity.Runtime.Entity BindingEntity
        {
            get
            {
                return m_BindingEntity;
            }
        }

        /// <summary>
        /// 获取声音在世界空间中的位置。
        /// </summary>
        public Vector3 WorldPosition
        {
            get
            {
                return m_WorldPosition;
            }
        }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData
        {
            get
            {
                return m_UserData;
            }
        }

        /// <summary>
        /// 创建播放声音信息。
        /// </summary>
        /// <param name="bindingEntity">绑定的实体。</param>
        /// <param name="worldPosition">声音在世界空间中的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>创建的播放声音信息。</returns>
        public static PlaySoundInfo Create(Entity.Runtime.Entity bindingEntity, Vector3 worldPosition, object userData)
        {
            PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
            playSoundInfo.m_BindingEntity = bindingEntity;
            playSoundInfo.m_WorldPosition = worldPosition;
            playSoundInfo.m_UserData = userData;
            return playSoundInfo;
        }

        /// <summary>
        /// 清理播放声音信息。
        /// </summary>
        public void Clear()
        {
            m_BindingEntity = null;
            m_WorldPosition = Vector3.zero;
            m_UserData = null;
        }
    }
}
