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
        private readonly ObjectPoolModule.ObjectPool<EntityInstanceObject> m_InstancePool;

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
        /// 构造实体组实例。
        /// </summary>
        /// <param name="groupSetting">实体组设置。</param>
        /// <param name="groupGo">实体组对应的GameObject。</param>
        /// <param name="objectPoolModule">对象池管理模块。</param>
        public EntityGroup(EntityGroupCfg row, GameObject groupGo, ObjectPoolModule objectPoolModule)
        {
            if (row is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组设置信息为空.");
            if (groupGo is null) throw new InvalidOperationException("[EntityGroup] 构造实体组实例失败，实体组GameObject为空.");

            Name    = row.Id.ToString();
            GroupGo = groupGo;

            var poolName = $"Entity Instance Pool ({Name})";
            m_InstancePool = objectPoolModule.CreateObjectPool<EntityInstanceObject>(poolName, row.InstanceCapacity, row.InstanceExpireTime, row.InstancePriority);
            m_InstancePool.AutoReleaseInterval = row.InstanceAutoReleaseInterval;

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
        /// <param name="spawned"></param>
        public void RegisterEntityInstanceObject(EntityInstanceObject obj, bool spawned) => m_InstancePool.Register(obj, spawned);

        /// <summary>
        /// 生成一个指定实体实例对象。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityInstanceObject SpawnEntityInstanceObject(string name) => m_InstancePool.Get(name);

        /// <summary>
        /// 回收指定实体实例对象。
        /// </summary>
        /// <param name="entity"></param>
        public void RecycleEntity(Entity entity) => m_InstancePool.Recycle(entity.Go);

        /// <summary>
        /// 回收实体实例对象到对象池。
        /// </summary>
        /// <param name="entityInstanceObject">要回收的实体实例对象。</param>
        public void RecycleEntityInstanceObject(EntityInstanceObject entityInstanceObject) => m_InstancePool.Recycle(entityInstanceObject);

        /// <summary>
        /// 设置实体实例对象是否被锁定。
        /// </summary>
        /// <param name="entityInstance"></param>
        /// <param name="locked"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetEntityInstanceLocked(object entityInstance, bool locked)
        {
            if (entityInstance is null) throw new InvalidOperationException("[EntityGroup] 设置实体实例对象是否被锁定时异常，实体实例为空.");
            m_InstancePool.SetLocked(entityInstance, locked);
        }

        /// <summary>
        /// 设置实体实例对象优先级。
        /// </summary>
        /// <param name="entityInstance"></param>
        /// <param name="priority"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetEntityInstancePriority(object entityInstance, int priority)
        {
            if (entityInstance is null) throw new InvalidOperationException("[EntityGroup] 设置实体实例对象优先级时异常，实体实例为空.");
            m_InstancePool.SetPriority(entityInstance, priority);
        }
    }
}
