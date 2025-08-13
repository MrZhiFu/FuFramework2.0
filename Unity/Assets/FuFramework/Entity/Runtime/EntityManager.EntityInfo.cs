using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public sealed partial class EntityManager
    {
        /// <summary>
        /// 实体信息。
        /// </summary>
        private sealed class EntityInfo : IReference
        {
            /// <summary>
            /// 实体。
            /// </summary>
            public IEntity Entity { get; private set; }

            /// <summary>
            /// 子实体列表。
            /// </summary>
            private readonly List<IEntity> m_ChildEntities = new();

            /// <summary>
            /// 父实体。
            /// </summary>
            public IEntity ParentEntity { get; set; }

            /// <summary>
            /// 实体状态。
            /// </summary>
            public EntityStatus Status { get; set; } = EntityStatus.Unknown;


            /// <summary>
            /// 创建实体信息。
            /// </summary>
            /// <param name="entity"></param>
            /// <returns></returns>
            /// <exception cref="FuException"></exception>
            public static EntityInfo Create(IEntity entity)
            {
                if (entity == null) throw new FuException("Entity is invalid.");
                var entityInfo = ReferencePool.Acquire<EntityInfo>();
                entityInfo.Entity = entity;
                entityInfo.Status = EntityStatus.WillInit;
                return entityInfo;
            }

            /// <summary>
            /// 清空实体信息。
            /// </summary>
            public void Clear()
            {
                Entity       = null;
                Status       = EntityStatus.Unknown;
                ParentEntity = null;
                m_ChildEntities.Clear();
            }

            /// <summary>
            /// 子实体数量。
            /// </summary>
            public int ChildEntityCount => m_ChildEntities.Count;

            /// <summary>
            /// 获取第一个子实体。
            /// </summary>
            /// <returns></returns>
            public IEntity GetChildEntity() => m_ChildEntities.Count > 0 ? m_ChildEntities[0] : null;

            /// <summary>
            /// 获取所有子实体。
            /// </summary>
            /// <returns></returns>
            public IEntity[] GetChildEntities() => m_ChildEntities.ToArray();

            /// <summary>
            /// 获取所有子实体。
            /// </summary>
            /// <param name="results"></param>
            /// <exception cref="FuException"></exception>
            public void GetChildEntities(List<IEntity> results)
            {
                if (results == null) throw new FuException("Results is invalid.");
                results.Clear();
                results.AddRange(m_ChildEntities);
            }

            /// <summary>
            /// 添加子实体。
            /// </summary>
            /// <param name="childEntity"></param>
            /// <exception cref="FuException"></exception>
            public void AddChildEntity(IEntity childEntity)
            {
                if (m_ChildEntities.Contains(childEntity)) throw new FuException("Can not add child entity which is already exist.");
                m_ChildEntities.Add(childEntity);
            }

            /// <summary>
            /// 移除子实体。
            /// </summary>
            /// <param name="childEntity"></param>
            /// <exception cref="FuException"></exception>
            public void RemoveChildEntity(IEntity childEntity)
            {
                if (m_ChildEntities.Remove(childEntity)) return;
                throw new FuException("Can not remove child entity which is not exist.");
            }
        }
    }
}