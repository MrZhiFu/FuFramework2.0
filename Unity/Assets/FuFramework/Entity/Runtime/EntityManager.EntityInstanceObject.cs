using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public sealed partial class EntityManager
    {
        /// <summary>
        /// 实体实例对象。
        /// </summary>
        private sealed class EntityInstanceObject : ObjectBase
        {
            /// <summary>
            /// 实体资源
            /// </summary>
            private object m_EntityAsset;

            /// <summary>
            /// 实体帮助器
            /// </summary>
            private IEntityHelper m_EntityHelper;

            /// <summary>
            /// 创建实体实例对象
            /// </summary>
            /// <param name="name"></param>
            /// <param name="entityAsset"></param>
            /// <param name="entityInstance"></param>
            /// <param name="entityHelper"></param>
            /// <returns></returns>
            /// <exception cref="FuException"></exception>
            public static EntityInstanceObject Create(string name, object entityAsset, object entityInstance, IEntityHelper entityHelper)
            {
                if (entityAsset  == null) throw new FuException("Entity asset is invalid.");
                if (entityHelper == null) throw new FuException("Entity helper is invalid.");

                var entityInstanceObject = ReferencePool.Acquire<EntityInstanceObject>();
                entityInstanceObject.Initialize(name, entityInstance);
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
            protected override void Release(bool isShutdown) => m_EntityHelper.ReleaseEntity(m_EntityAsset, Target);
        }
    }
}