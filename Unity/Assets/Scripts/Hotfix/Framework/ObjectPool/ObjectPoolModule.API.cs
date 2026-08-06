using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池管理模块的公共 API。
    /// 功能：
    ///     1. 提供对象池的创建、获取、查询、销毁接口。
    ///     2. 提供模块级的资源释放接口。
    /// </summary>
    public sealed partial class ObjectPoolModule : ModuleBase
    {
        /// <summary>
        /// 获取对象池数量。
        /// </summary>
        public int Count => m_ObjPoolDict.Count;

        #region 创建对象池

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, DefaultAutoDisposeInterval, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池(命名池 + 容量 + 过期时间 + 优先级，自动销毁间隔取默认)。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, DefaultAutoDisposeInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="autoDisposeInterval">对象池自动销毁可销毁对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, float autoDisposeInterval, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, autoDisposeInterval, capacity, expireTime, priority);
        }

        #endregion

        #region 获取对象池

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>(string poolName) where T : ObjectBase
        {
            if (string.IsNullOrEmpty(poolName))
                throw new InvalidOperationException("[ObjectPoolModule] 对象池名称不能为空.");
            return HasObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase
        {
            if (string.IsNullOrEmpty(poolName))
                throw new InvalidOperationException("[ObjectPoolModule] 对象池名称不能为空.");

            var typeNamePair = new TypeNamePair(typeof(T), poolName);
            var objectPool   = (ObjectPool<T>)GetObjectPoolInternal(typeNamePair);
            if (objectPool == null) throw new InvalidOperationException($"[ObjectPoolModule] 不存在对象池 '{typeNamePair}'.");
            return objectPool;
        }

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <returns>所有对象池。</returns>
        public ObjectPoolBase[] GetAllObjectPools(bool sort = false)
        {
            if (sort)
            {
                var results = new List<ObjectPoolBase>();
                foreach (var (_, objPool) in m_ObjPoolDict)
                {
                    results.Add(objPool);
                }

                results.Sort(ObjectPoolComparer);
                return results.ToArray();
            }
            else
            {
                var index   = 0;
                var results = new ObjectPoolBase[m_ObjPoolDict.Count];
                foreach (var (_, objPool) in m_ObjPoolDict)
                {
                    results[index++] = objPool;
                }

                return results;
            }
        }

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <param name="results">所有对象池。</param>
        public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results)
        {
            if (results == null) throw new InvalidOperationException("[ObjectPoolModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                results.Add(objPool);
            }

            if (sort)
                results.Sort(ObjectPoolComparer);
        }

        #endregion

        #region 销毁对象池

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DisposeObjectPool<T>(string poolName) where T : ObjectBase
        {
            if (string.IsNullOrEmpty(poolName))
                throw new InvalidOperationException("[ObjectPoolModule] 对象池名称不能为空.");
            return DisposeObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DisposeObjectPool<T>(ObjectPool<T> objectPool) where T : ObjectBase
        {
            if (objectPool == null) throw new InvalidOperationException("[ObjectPoolModule] 对象池为不能为空.");
            return DisposeObjectPoolInternal(new TypeNamePair(typeof(T), objectPool.Name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DisposeObjectPool(ObjectPoolBase objectPool)
        {
            if (objectPool == null) throw new InvalidOperationException("[ObjectPoolModule] 对象池为不能为空.");
            return DisposeObjectPoolInternal(new TypeNamePair(objectPool.ObjectType, objectPool.Name));
        }

        #endregion

        #region 模块级销毁

        /// <summary>
        /// 销毁所有对象池中超过容量的可销毁对象。
        /// </summary>
        public void DisposeOverCapacity()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 销毁所有对象池中超过容量的可销毁对象...");
            GetAllObjectPools(true, m_CachedObjPoolList);
            foreach (var objectPool in m_CachedObjPoolList)
            {
                objectPool.DisposeOverCapacity();
            }
        }

        /// <summary>
        /// 销毁对象池中的所有未使用对象。
        /// </summary>
        public void DisposeAllUnused()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 销毁所有对象池中的所有未使用对象...");
            GetAllObjectPools(true, m_CachedObjPoolList);
            foreach (var objectPool in m_CachedObjPoolList)
            {
                objectPool.DisposeAllUnused();
            }
        }

        #endregion
    }
}