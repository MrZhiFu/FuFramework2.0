using System.Collections.Generic;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    /// <summary>
    /// 实体信息, 用于管理实体的状态、及其下子实体，如添加、移除子实体。
    /// </summary>
    public sealed class EntityInfo : IReference
    {
        /// <summary>
        /// 实体。
        /// </summary>
        public Entity Entity { get; private set; }

        /// <summary>
        /// 父实体。
        /// </summary>
        public Entity ParentEntity { get; set; }

        /// <summary>
        /// 实体状态。
        /// </summary>
        public EEntityStatus Status { get; set; } = EEntityStatus.Unknown;

        /// <summary>
        /// 子实体列表。
        /// </summary>
        private readonly List<Entity> m_ChildEntities = new();


        /// <summary>
        /// 获取子实体数量。
        /// </summary>
        public int ChildEntityCount => m_ChildEntities.Count;

        /// <summary>
        /// 创建实体信息。
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        public static EntityInfo Create(Entity entity)
        {
            if (entity is null) throw new FuException("[EntityInfo] 创建实体信息失败，实体显示对象为空!");
            var entityInfo = ReferencePool.Runtime.ReferencePool.Acquire<EntityInfo>();
            entityInfo.Entity = entity;
            entityInfo.Status = EEntityStatus.WillInit;
            return entityInfo;
        }

        /// <summary>
        /// 清空实体信息。
        /// </summary>
        public void Clear()
        {
            Entity       = null;
            Status       = EEntityStatus.Unknown;
            ParentEntity = null;
            m_ChildEntities.Clear();
        }

        /// <summary>
        /// 获取第一个子实体。
        /// </summary>
        /// <returns></returns>
        public Entity GetChildEntity() => m_ChildEntities.Count > 0 ? m_ChildEntities[0] : null;

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <returns></returns>
        public Entity[] GetChildEntities() => m_ChildEntities.ToArray();

        /// <summary>
        /// 获取所有子实体。
        /// </summary>
        /// <param name="results"></param>
        /// <exception cref="FuException"></exception>
        public void GetChildEntities(List<Entity> results)
        {
            if (results is null) throw new FuException("[EntityInfo] 结果列表为空!");
            results.Clear();
            results.AddRange(m_ChildEntities);
        }

        /// <summary>
        /// 添加子实体。
        /// </summary>
        /// <param name="childEntity"></param>
        /// <exception cref="FuException"></exception>
        public void AddChildEntity(Entity childEntity)
        {
            if (m_ChildEntities.Contains(childEntity)) throw new FuException("[EntityInfo]添加子实体失败, 子实体已存在, 不能重复添加!");
            m_ChildEntities.Add(childEntity);
        }

        /// <summary>
        /// 移除子实体。
        /// </summary>
        /// <param name="childEntity"></param>
        /// <exception cref="FuException"></exception>
        public void RemoveChildEntity(Entity childEntity)
        {
            if (m_ChildEntities.Remove(childEntity)) return;
            throw new FuException("[EntityInfo]移除子实体失败, 子实体不存在, 不能移除!");
        }
    }
}