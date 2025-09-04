using FuFramework.Core.Runtime;
using FuFramework.ObjectPool.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public sealed partial class EntityManager
    {
        /// <summary>
        /// 实体实例对象。
        /// 包装了实体资源和实体帮助器，用于实体实例对象池管理，方便释放实体资源。
        /// </summary>
        public sealed class EntityInstanceObject : ObjectBase
        {
            /// <summary>
            /// 实体资源
            /// </summary>
            private object m_EntityAsset;

            /// <summary>
            /// 实体帮助器
            /// </summary>
            private DefaultEntityHelper m_EntityHelper;

            /// <summary>
            /// 创建实体实例对象
            /// </summary>
            /// <param name="name">实体名称</param>
            /// <param name="entityAsset">实体资源</param>
            /// <param name="entityInstanceGo">实体实例GameObject</param>
            /// <param name="entityHelper"></param>
            /// <returns></returns>
            /// <exception cref="FuException"></exception>
            public static EntityInstanceObject Create(string name, object entityAsset, GameObject entityInstanceGo, DefaultEntityHelper entityHelper)
            {
                if (entityAsset  == null) throw new FuException("Entity asset is invalid.");
                if (entityHelper == null) throw new FuException("Entity helper is invalid.");

                var entityInstanceObject = ReferencePool.Runtime.ReferencePool.Acquire<EntityInstanceObject>();
                entityInstanceObject.Initialize(name, entityInstanceGo);
                entityInstanceObject.m_EntityAsset  = entityAsset;
                entityInstanceObject.m_EntityHelper = entityHelper;
                return entityInstanceObject;
            }

            /// <summary>
            /// 清理实体实例对象
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                m_EntityAsset  = null;
                m_EntityHelper = null;
            }

            /// <summary>
            /// 释放实体
            /// </summary>
            /// <param name="isShutdown"></param>
            protected override void Release(bool isShutdown)
            {
                m_EntityHelper.ReleaseEntity(m_EntityAsset, Target);
            }
        }
    }
}