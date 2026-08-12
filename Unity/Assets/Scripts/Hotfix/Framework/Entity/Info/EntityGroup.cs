using System;
﻿using UnityEngine;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using EntityGroupCfg = Hotfix.Game.Config.Tables.EntityGroup;
using Hotfix.Framework.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Entity
{
    /// <summary>
    /// 实体组。
    /// 功能：
    ///     1. 管理实体组中的实体，
    ///     2. 对外提供获取实体、获取实体组中实体数量、设置实体实例的优先级等接口。
    /// </summary>
    public sealed class EntityGroup
    {
        /// <summary>
        /// 实体实例对象池。
        /// </summary>
        private readonly ObjectPool<EntityObject> m_EntityPool;

        /// <summary>
        /// 实体组实体链表。
        /// </summary>
        private readonly FuLinkedList<Entity> m_Entities;

        /// <summary>
        /// 缓存实体的链表节点。
        /// </summary>
        private LinkedListNode<Entity> m_CachedNode;

        /// <summary>
        /// 获取实体组名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取实体组对应的GameObject。
        /// </summary>
        public GameObject GroupGo { get; }


        /// <summary>
        /// 获取实体组中实体数量。
        /// </summary>
        public int EntityCount => m_Entities.Count;

        /// <summary>
        /// 获取或设置实体组实例对象池自动销毁检查的间隔秒数。
        /// </summary>
        public float PoolAutoDisposeCheckInterval
        {
            get => m_EntityPool.AutoDisposeCheckInterval;
            set => m_EntityPool.AutoDisposeCheckInterval = value;
        }

        /// <summary>
        /// 获取或设置实体组实例对象池的容量。
        /// </summary>
        public int PoolCapacity
        {
            get => m_EntityPool.Capacity;
            set => m_EntityPool.Capacity = value;
        }

        /// <summary>
        /// 获取或设置实体组实例对象池对象过期秒数。
        /// 对象闲置（距上次使用或回收）超过该秒数即视为过期，纳入销毁候选。
        /// </summary>
        public float PoolExpireTimeAfterIdle
        {
            get => m_EntityPool.ExpireTimeAfterIdle;
            set => m_EntityPool.ExpireTimeAfterIdle = value;
        }

        /// <summary>
        /// 获取或设置实体组实例对象池的优先级。
        /// </summary>
        public int PoolObjectPriority
        {
            get => m_EntityPool.Priority;
            set => m_EntityPool.Priority = value;
        }

        /// <summary>
        /// 构造实体组实例。
        /// </summary>
        /// <param name="row">实体组配置行。</param>
        /// <param name="groupGo">实体组对应的GameObject。</param>
        /// <param name="objectPoolModule">对象池管理模块。</param>
        public EntityGroup(EntityGroupCfg row, GameObject groupGo, ObjectPoolModule objectPoolModule)
        {
            if (row is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组设置信息为空.");

            Name    = row.Id.ToString();
            GroupGo = groupGo ?? throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组GameObject为空.");

            var poolName = $"EntityPool-{Name}";
            m_EntityPool = objectPoolModule.CreateObjectPool<EntityObject>(poolName, row.PoolCapacity, row.PoolExpireTimeAfterIdle, row.PoolPriority);

            m_EntityPool.AutoDisposeCheckInterval = row.PoolAutoDisposeCheckInterval;

            m_Entities   = new FuLinkedList<Entity>();
            m_CachedNode = null;
        }

        /// <summary>
        /// 实体组轮询。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            var current = m_Entities.First;
            while (current != null)
            {
                m_CachedNode = current.Next;
                current.Value.OnUpdate(deltaTime, unscaledDeltaTime);
                current      = m_CachedNode;
                m_CachedNode = null;
            }
        }

        /// <summary>
        /// 实体组中是否存在实体。
        /// </summary>
        /// <param name="entityId">实体序列编号。</param>
        /// <returns>实体组中是否存在实体。</returns>
        public bool HasEntity(int entityId)
        {
            foreach (var entity in m_Entities)
            {
                if (entity.Id == entityId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 实体组中是否存在实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>实体组中是否存在实体。</returns>
        public bool HasEntity(string entityAssetName)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityGroup] 实体资源名称为空.");
            foreach (var entity in m_Entities)
            {
                if (entity.EntityAssetName == entityAssetName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 从实体组中获取实体。
        /// </summary>
        /// <param name="entityId">实体序列编号。</param>
        /// <returns>要获取的实体。</returns>
        public Entity GetEntity(int entityId)
        {
            foreach (var entity in m_Entities)
            {
                if (entity.Id == entityId)
                    return entity;
            }

            return null;
        }

        /// <summary>
        /// 从实体组中获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>要获取的实体。</returns>
        public Entity GetEntity(string entityAssetName)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityGroup] 实体资源名称为空.");
            foreach (var entity in m_Entities)
            {
                if (entity.EntityAssetName == entityAssetName)
                    return entity;
            }

            return null;
        }

        /// <summary>
        /// 从实体组中获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <returns>要获取的实体。</returns>
        public Entity[] GetEntities(string entityAssetName)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityGroup] 实体资源名称为空.");
            var results = new List<Entity>();
            foreach (var entity in m_Entities)
            {
                if (entity.EntityAssetName == entityAssetName)
                    results.Add(entity);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 从实体组中获取实体。
        /// </summary>
        /// <param name="entityAssetName">实体资源名称。</param>
        /// <param name="results">要获取的实体。</param>
        public void GetEntities(string entityAssetName, List<Entity> results)
        {
            if (string.IsNullOrEmpty(entityAssetName)) throw new InvalidOperationException("[EntityGroup] 实体资源名称为空.");
            if (results is null) throw new InvalidOperationException("[EntityGroup] 结果列表为空.");
            results.Clear();
            foreach (var entity in m_Entities)
            {
                if (entity.EntityAssetName == entityAssetName)
                    results.Add(entity);
            }
        }

        /// <summary>
        /// 从实体组中获取所有实体。
        /// </summary>
        /// <returns>实体组中的所有实体。</returns>
        public Entity[] GetAllEntities()
        {
            var results = new Entity[m_Entities.Count];
            var index   = 0;
            foreach (var entity in m_Entities)
            {
                results[index++] = entity;
            }

            return results;
        }

        /// <summary>
        /// 从实体组中获取所有实体。
        /// </summary>
        /// <param name="results">实体组中的所有实体。</param>
        public void GetAllEntities(List<Entity> results)
        {
            if (results is null) throw new InvalidOperationException("[EntityGroup] 结果列表为空.");
            results.Clear();
            results.AddRange(m_Entities);
        }

        /// <summary>
        /// 往实体组增加实体。
        /// </summary>
        /// <param name="entity">要增加的实体。</param>
        public void AddEntity(Entity entity) => m_Entities.AddLast(entity);

        /// <summary>
        /// 从实体组移除实体。
        /// </summary>
        /// <param name="entity">要移除的实体。</param>
        public void RemoveEntity(Entity entity)
        {
            if (m_CachedNode != null && m_CachedNode.Value == entity)
                m_CachedNode = m_CachedNode.Next;
            if (!m_Entities.Remove(entity))
                throw new InvalidOperationException($"[EntityGroup] 移除实体失败，实体组 '{Name}' 中不存在指定的实体 '[{entity.Id}]{entity.EntityAssetName}'.");
        }

        /// <summary>
        /// 创建并注册一个指定实体实例对象。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="inUse">对象注册时是否已处于使用中。</param>
        public void RegisterEntityObject(EntityObject obj, bool inUse) => m_EntityPool.Register(obj, inUse);

        /// <summary>
        /// 生成一个指定实体实例对象。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityObject SpawnEntityObject(string name) => m_EntityPool.Spawn(name);

        /// <summary>
        /// 回收指定实体实例对象。
        /// </summary>
        /// <param name="entity"></param>
        public void RecycleEntity(Entity entity) => m_EntityPool.Recycle(entity.Go);

        /// <summary>
        /// 回收实体实例对象到对象池。
        /// </summary>
        /// <param name="entityObject">要回收的实体实例对象。</param>
        public void RecycleEntityObject(EntityObject entityObject) => m_EntityPool.Recycle(entityObject);

        /// <summary>
        /// 设置实体实例对象是否被锁定。
        /// </summary>
        /// <param name="entityGo"></param>
        /// <param name="locked"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetEntityObjectLocked(object entityGo, bool locked)
        {
            if (entityGo is null) throw new InvalidOperationException("[EntityGroup] 设置实体实例对象是否被锁定时异常，实体实例为空.");
            m_EntityPool.SetLocked(entityGo, locked);
        }

        /// <summary>
        /// 设置实体实例对象优先级。
        /// </summary>
        /// <param name="entityGo"></param>
        /// <param name="priority"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetEntityObjectPriority(object entityGo, int priority)
        {
            if (entityGo is null) throw new InvalidOperationException("[EntityGroup] 设置实体实例对象优先级时异常，实体实例为空.");
            m_EntityPool.SetPriority(entityGo, priority);
        }

        /// <summary>
        /// 销毁实体组对象池及其所有实体对象（释放句柄）。
        /// 供模块 OnDispose 显式清理本组持有的句柄，不依赖 ObjectPoolModule 逆序销毁的隐式顺序。
        /// </summary>
        /// <param name="objectPoolModule">对象池管理模块。</param>
        public void DisposeEntityPool(ObjectPoolModule objectPoolModule)
        {
            if (objectPoolModule is null) throw new InvalidOperationException("[EntityGroup] 销毁实体组对象池失败，对象池管理模块为空.");
            objectPoolModule.DisposeObjectPool(m_EntityPool);
        }
    }
}