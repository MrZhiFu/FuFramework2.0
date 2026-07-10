using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Runtime
{
    /// <summary>
    /// 对象池管理模块。
    /// 
    /// 目标：通过对象池管理 Unity端的GameObject的创建、销毁和复用，减少实例化(Instantiate)和销毁(Destroy)的开销。
    /// 功能：
    ///     1. 提供对象池的创建、获取、释放和销毁接口。
    /// </summary>
    public sealed partial class ObjectPoolModule : ModuleBase
    {
        /// <summary>
        /// 对象池默认容量。
        /// </summary>
        private const int DefaultCapacity = int.MaxValue;

        /// <summary>
        /// 对象池默认过期时间。
        /// </summary>
        private const float DefaultExpireTime = float.MaxValue;

        /// <summary>
        /// 对象池默认优先级。
        /// </summary>
        private const int DefaultPriority = 0;


        /// <summary>
        /// 存储所有对象池的字典, Key为对象池的类型+名称，Value为对象池。
        /// </summary>
        private readonly Dictionary<TypeNamePair, ObjectPoolBase> m_ObjPoolDict = new();

        /// <summary>
        /// 缓存所有对象池的列表。释放所有对象池时使用。
        /// </summary>
        private readonly List<ObjectPoolBase> m_CachedObjPoolList = new();

        /// <summary>
        /// 获取对象池数量。
        /// </summary>
        public int Count => m_ObjPoolDict.Count;

        /// <summary>
        /// 初始化
        /// </summary>
        protected override void OnInit()
        {
            Application.lowMemory += OnLowMemory;
        }

        /// <summary>
        /// 帧更新。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                objPool.Update(unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected override void OnDispose()
        {
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                objPool.OnDispose();
            }

            m_ObjPoolDict.Clear();
            m_CachedObjPoolList.Clear();

            Application.lowMemory -= OnLowMemory;
        }

        /// <summary>
        /// 低内存回调
        /// </summary>
        private void OnLowMemory()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 低内存警告, 释放对象池中所有未使用的资源...");

            // 释放对象池中所有未使用的资源
            ReleaseAllUnused();
        }

        #region 获取对象池相关

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>() where T : ObjectBase
        {
            return _HasObjectPool(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Type objectType)
        {
            if (objectType == null) throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            return _HasObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>(string poolName) where T : ObjectBase
        {
            return _HasObjectPool(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Type objectType, string poolName)
        {
            if (objectType == null) throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            return _HasObjectPool(new TypeNamePair(objectType, poolName));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null)
                throw new FuException("[ObjectPoolModule] 检查条件不能为空.");

            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                if (!condition(objPool)) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>() where T : ObjectBase
        {
            return (ObjectPool<T>)_GetObjectPool(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Type objectType)
        {
            if (objectType == null) throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            return _GetObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase
        {
            return (ObjectPool<T>)_GetObjectPool(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Type objectType, string poolName)
        {
            if (objectType == null) throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            return _GetObjectPool(new TypeNamePair(objectType, poolName));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null) throw new FuException("[ObjectPoolModule] 检查条件不能为空.");

            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                if (!condition(objPool)) continue;
                return objPool;
            }

            return null;
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase[] GetObjectPools(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null) throw new FuException("[ObjectPoolModule] 检查条件不能为空.");

            var results = new List<ObjectPoolBase>();
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                if (!condition(objPool)) continue;
                results.Add(objPool);
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="results">要获取的对象池。</param>
        public void GetObjectPools(Predicate<ObjectPoolBase> condition, List<ObjectPoolBase> results)
        {
            if (condition == null) throw new FuException("[ObjectPoolModule] 检查条件不能为空.");
            if (results   == null) throw new FuException("[ObjectPoolModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                if (!condition(objPool)) continue;
                results.Add(objPool);
            }
        }

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <returns>所有对象池。</returns>
        public ObjectPoolBase[] GetAllObjectPools() => GetAllObjectPools(false);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="results">所有对象池。</param>
        public void GetAllObjectPools(List<ObjectPoolBase> results) => GetAllObjectPools(false, results);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <returns>所有对象池。</returns>
        public ObjectPoolBase[] GetAllObjectPools(bool sort)
        {
            if (sort)
            {
                var results = new List<ObjectPoolBase>();
                foreach (var (_, objPool) in m_ObjPoolDict)
                {
                    results.Add(objPool);
                }

                results.Sort(_ObjectPoolComparer);
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
            if (results == null) throw new FuException("[ObjectPoolModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                results.Add(objPool);
            }

            if (sort)
                results.Sort(_ObjectPoolComparer);
        }

        #endregion

        #region 创建对象池

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime,
                                     DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(int capacity, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, float expireTime, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, float expireTime, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, float expireTime, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(int capacity, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, float expireTime, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(string.Empty, allowSpawnInUse, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority, bool allowSpawnInUse = false)
            where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority,
                                                 bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority,
                                               bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
        }

        #endregion

        #region 销毁对象池

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>() where T : ObjectBase
        {
            return _DestroyObjectPool(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(Type objectType)
        {
            if (objectType == null)
                throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            return _DestroyObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(string poolName) where T : ObjectBase
        {
            return _DestroyObjectPool(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(Type objectType, string poolName)
        {
            if (objectType == null)
                throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            return _DestroyObjectPool(new TypeNamePair(objectType, poolName));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(ObjectPool<T> objectPool) where T : ObjectBase
        {
            if (objectPool == null) throw new FuException("[ObjectPoolModule] 对象池为不能为空.");
            return _DestroyObjectPool(new TypeNamePair(typeof(T), objectPool.Name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(ObjectPoolBase objectPool)
        {
            if (objectPool == null) throw new FuException("[ObjectPoolModule] 对象池为不能为空.");
            return _DestroyObjectPool(new TypeNamePair(objectPool.ObjectType, objectPool.Name));
        }

        #endregion

        #region 释放对象池

        /// <summary>
        /// 释放所有对象池中的所有可释放对象。
        /// </summary>
        public void Release()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 释放所有对象池中可释放对象...");
            GetAllObjectPools(true, m_CachedObjPoolList);
            foreach (var objectPool in m_CachedObjPoolList)
            {
                objectPool.Release();
            }
        }

        /// <summary>
        /// 释放对象池中的所有未使用对象。
        /// </summary>
        public void ReleaseAllUnused()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 释放所有对象池中的所有未使用对象...");
            GetAllObjectPools(true, m_CachedObjPoolList);
            foreach (var objectPool in m_CachedObjPoolList)
            {
                objectPool.ReleaseAllUnused();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <returns>是否存在对象池。</returns>
        private bool _HasObjectPool(TypeNamePair typeNamePair)
        {
            return m_ObjPoolDict.ContainsKey(typeNamePair);
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <returns>要获取的对象池。</returns>
        private ObjectPoolBase _GetObjectPool(TypeNamePair typeNamePair)
        {
            return m_ObjPoolDict.GetValueOrDefault(typeNamePair);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        private ObjectPool<T> _CreateObjectPool<T>(string poolName, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime,
                                                   int priority) where T : ObjectBase
        {
            var typeNamePair = new TypeNamePair(typeof(T), poolName);
            if (HasObjectPool<T>(poolName))
                throw new FuException($"[ObjectPoolModule] 对象池 '{typeNamePair}' 已存在, 不可重复创建.");

            var objectPool = new ObjectPool<T>(poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjPoolDict.Add(typeNamePair, objectPool);
            return objectPool;
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        private ObjectPoolBase _CreateObjectPool(Type objectType, string poolName, bool allowSpawnInUse, float autoReleaseInterval, int capacity,
                                                 float expireTime, int priority)
        {
            if (objectType == null) throw new FuException("[ObjectPoolModule] 对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException($"[ObjectPoolModule] 对象类型 '{objectType.FullName}' 不是 ObjectBase 的子类.");

            var typeNamePair = new TypeNamePair(objectType, poolName);
            if (HasObjectPool(objectType, poolName))
                throw new FuException($"[ObjectPoolModule] 对象池 '{typeNamePair}' 已存在, 不可重复创建");

            var objectPoolType = typeof(ObjectPool<>).MakeGenericType(objectType);
            var objectPool = (ObjectPoolBase)Activator.CreateInstance(objectPoolType, poolName, allowSpawnInUse, autoReleaseInterval, capacity,
                                                                      expireTime, priority);
            m_ObjPoolDict.Add(typeNamePair, objectPool);
            return objectPool;
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <returns>是否销毁对象池成功。</returns>
        private bool _DestroyObjectPool(TypeNamePair typeNamePair)
        {
            if (!m_ObjPoolDict.TryGetValue(typeNamePair, out var objectPool)) return false;
            objectPool.OnDispose();
            return m_ObjPoolDict.Remove(typeNamePair);
        }

        /// <summary>
        /// 对象池比较器
        /// </summary>
        /// <param name="a">对象池a</param>
        /// <param name="b">对象池b</param>
        /// <returns></returns>
        private static int _ObjectPoolComparer(ObjectPoolBase a, ObjectPoolBase b)
        {
            return a.Priority.CompareTo(b.Priority);
        }

        #endregion
    }
}