using System.Collections.Generic;
using System.Linq;
using FuFramework.Core.Runtime;
using FuFramework.ObjectPool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public sealed partial class EntityManager
    {
        /// <summary>
        /// 实体组。
        /// 功能：
        /// 1.实现实体组的基本操作接口，管理实体组中的实体实例，
        /// 2.对外提供获取实体、获取实体组中实体数量、设置实体实例的优先级等接口。
        /// </summary>
        private sealed class EntityGroup : IEntityGroup
        {
            /// <summary>
            /// 实体实例对象池。
            /// </summary>
            private readonly IObjectPool<EntityInstanceObject> m_InstancePool;

            /// <summary>
            /// 实体组实体链表。
            /// </summary>
            private readonly FuLinkedList<IEntity> m_Entities;

            /// <summary>
            /// 缓存实体的链表节点。
            /// </summary>
            private LinkedListNode<IEntity> m_CachedNode;


            /// <summary>
            /// 获取实体组名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取实体组辅助器。
            /// </summary>
            public IEntityGroupHelper Helper { get; }


            /// <summary>
            /// 初始化实体组的新实例。
            /// </summary>
            /// <param name="name">实体组名称。</param>
            /// <param name="instanceAutoReleaseInterval">实体实例对象池自动释放可释放对象的间隔秒数。</param>
            /// <param name="instanceCapacity">实体实例对象池容量。</param>
            /// <param name="instanceExpireTime">实体实例对象池对象过期秒数。</param>
            /// <param name="instancePriority">实体实例对象池的优先级。</param>
            /// <param name="entityGroupHelper">实体组辅助器。</param>
            /// <param name="objectPoolManager">对象池管理器。</param>
            public EntityGroup(string name, float instanceAutoReleaseInterval, int instanceCapacity, float instanceExpireTime, int instancePriority, IEntityGroupHelper entityGroupHelper,
                               IObjectPoolManager objectPoolManager)
            {
                if (string.IsNullOrEmpty(name)) throw new FuException("Entity group name is invalid.");

                Name = name;

                Helper = entityGroupHelper ?? throw new FuException("Entity group helper is invalid.");

                var poolName = Utility.Text.Format("Entity Instance Pool ({0})", name);
                m_InstancePool                     = objectPoolManager.CreateObjectPool<EntityInstanceObject>(poolName, instanceCapacity, instanceExpireTime, instancePriority);
                m_InstancePool.AutoReleaseInterval = instanceAutoReleaseInterval;

                m_Entities   = new FuLinkedList<IEntity>();
                m_CachedNode = null;
            }

            /// <summary>
            /// 获取实体组中实体数量。
            /// </summary>
            public int EntityCount => m_Entities.Count;

            /// <summary>
            /// 获取或设置实体组实例对象池自动释放可释放对象的间隔秒数。
            /// </summary>
            public float InstanceAutoReleaseInterval
            {
                get => m_InstancePool.AutoReleaseInterval;
                set => m_InstancePool.AutoReleaseInterval = value;
            }

            /// <summary>
            /// 获取或设置实体组实例对象池的容量。
            /// </summary>
            public int InstanceCapacity
            {
                get => m_InstancePool.Capacity;
                set => m_InstancePool.Capacity = value;
            }

            /// <summary>
            /// 获取或设置实体组实例对象池对象过期秒数。
            /// </summary>
            public float InstanceExpireTime
            {
                get => m_InstancePool.ExpireTime;
                set => m_InstancePool.ExpireTime = value;
            }

            /// <summary>
            /// 获取或设置实体组实例对象池的优先级。
            /// </summary>
            public int InstancePriority
            {
                get => m_InstancePool.Priority;
                set => m_InstancePool.Priority = value;
            }

            /// <summary>
            /// 实体组轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                var current = m_Entities.First;
                while (current != null)
                {
                    m_CachedNode = current.Next;
                    current.Value.OnUpdate(elapseSeconds, realElapseSeconds);
                    current      = m_CachedNode;
                    m_CachedNode = null;
                }
            }

            /// <summary>
            /// 实体组中是否存在实体。
            /// </summary>
            /// <param name="entityId">实体序列编号。</param>
            /// <returns>实体组中是否存在实体。</returns>
            public bool HasEntity(int entityId) => m_Entities.Any(entity => entity.Id == entityId);

            /// <summary>
            /// 实体组中是否存在实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <returns>实体组中是否存在实体。</returns>
            public bool HasEntity(string entityAssetName)
            {
                if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
                return m_Entities.Any(entity => entity.EntityAssetName == entityAssetName);
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityId">实体序列编号。</param>
            /// <returns>要获取的实体。</returns>
            public IEntity GetEntity(int entityId) => m_Entities.FirstOrDefault(entity => entity.Id == entityId);

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <returns>要获取的实体。</returns>
            public IEntity GetEntity(string entityAssetName)
            {
                if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
                return m_Entities.FirstOrDefault(entity => entity.EntityAssetName == entityAssetName);
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <returns>要获取的实体。</returns>
            public IEntity[] GetEntities(string entityAssetName)
            {
                if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
                return m_Entities.Where(entity => entity.EntityAssetName == entityAssetName).ToArray();
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <param name="results">要获取的实体。</param>
            public void GetEntities(string entityAssetName, List<IEntity> results)
            {
                if (string.IsNullOrEmpty(entityAssetName)) throw new FuException("Entity asset name is invalid.");
                if (results == null) throw new FuException("Results is invalid.");
                results.Clear();
                results.AddRange(m_Entities.Where(entity => entity.EntityAssetName == entityAssetName));
            }

            /// <summary>
            /// 从实体组中获取所有实体。
            /// </summary>
            /// <returns>实体组中的所有实体。</returns>
            public IEntity[] GetAllEntities() => m_Entities.ToArray();

            /// <summary>
            /// 从实体组中获取所有实体。
            /// </summary>
            /// <param name="results">实体组中的所有实体。</param>
            public void GetAllEntities(List<IEntity> results)
            {
                if (results == null) throw new FuException("Results is invalid.");
                results.Clear();
                results.AddRange(m_Entities);
            }

            /// <summary>
            /// 往实体组增加实体。
            /// </summary>
            /// <param name="entity">要增加的实体。</param>
            public void AddEntity(IEntity entity) => m_Entities.AddLast(entity);

            /// <summary>
            /// 从实体组移除实体。
            /// </summary>
            /// <param name="entity">要移除的实体。</param>
            public void RemoveEntity(IEntity entity)
            {
                if (m_CachedNode != null && m_CachedNode.Value == entity)
                    m_CachedNode = m_CachedNode.Next;
                if (!m_Entities.Remove(entity))
                    throw new FuException(Utility.Text.Format("Entity group '{0}' not exists specified entity '[{1}]{2}'.", Name, entity.Id, entity.EntityAssetName));
            }

            /// <summary>
            /// 创建并注册一个指定实体实例对象。
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="spawned"></param>
            public void RegisterEntityInstanceObject(EntityInstanceObject obj, bool spawned) => m_InstancePool.Register(obj, spawned);

            /// <summary>
            /// 生成一个指定实体实例对象。
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public EntityInstanceObject SpawnEntityInstanceObject(string name) => m_InstancePool.Spawn(name);

            /// <summary>
            /// 回收指定实体实例对象。
            /// </summary>
            /// <param name="entity"></param>
            public void RecycleEntity(IEntity entity) => m_InstancePool.Recycle(entity.Handle);

            /// <summary>
            /// 设置实体实例对象是否被锁定。
            /// </summary>
            /// <param name="entityInstance"></param>
            /// <param name="locked"></param>
            /// <exception cref="FuException"></exception>
            public void SetEntityInstanceLocked(object entityInstance, bool locked)
            {
                if (entityInstance == null) throw new FuException("Entity instance is invalid.");
                m_InstancePool.SetLocked(entityInstance, locked);
            }

            /// <summary>
            /// 设置实体实例对象优先级。
            /// </summary>
            /// <param name="entityInstance"></param>
            /// <param name="priority"></param>
            /// <exception cref="FuException"></exception>
            public void SetEntityInstancePriority(object entityInstance, int priority)
            {
                if (entityInstance == null) throw new FuException("Entity instance is invalid.");
                m_InstancePool.SetPriority(entityInstance, priority);
            }
        }
    }
}