using System;
using System.Collections.Generic;
using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Runtime
{
    /// <summary>
    /// 对象池管理器。
    /// "只允许单次获取"代表对象池中的对象只能在回收后才能再次被获取;
    /// "允许多次获取"代表对象池对中的对象能在未回收的状态下就能再次被获取
    /// </summary>
    internal sealed partial class ObjectPoolManager : FuModule, IObjectPoolManager
    {
        /// <summary>
        /// 获取游戏框架模块优先级。
        /// </summary>
        /// <remarks>优先级较高的模块会优先轮询，并且关闭操作会后进行。</remarks>
        protected override int Priority => 6;
        
        /// 对象池默认容量。
        private const int DefaultCapacity = int.MaxValue;

        /// 对象池默认过期时间。
        private const float DefaultExpireTime = float.MaxValue;

        /// 对象池默认优先级。
        private const int DefaultPriority = 0;


        /// 存储所有对象池的字典, Key为对象池的类型+名称，Value为对象池。
        private readonly Dictionary<TypeNamePair, ObjectPoolBase> m_ObjPoolDict;

        /// 缓存所有对象池的列表。
        private readonly List<ObjectPoolBase> m_CachedObjPoolList;

        /// 对象池比较器。用于对象池排序
        private readonly Comparison<ObjectPoolBase> m_ObjPoolComparer;

        /// <summary>
        /// 初始化对象池管理器的新实例。
        /// </summary>
        public ObjectPoolManager()
        {
            m_ObjPoolDict       = new Dictionary<TypeNamePair, ObjectPoolBase>();
            m_CachedObjPoolList = new List<ObjectPoolBase>();
            m_ObjPoolComparer   = _ObjectPoolComparer;
        }
        

        /// <summary>
        /// 获取对象池数量。
        /// </summary>
        public int Count => m_ObjPoolDict.Count;

        /// <summary>
        /// 对象池管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                objPool.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理对象池管理器。
        /// </summary>
        protected override void Shutdown()
        {
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                objPool.Shutdown();
            }

            m_ObjPoolDict.Clear();
            m_CachedObjPoolList.Clear();
        }

        #region 获取对象池相关

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>() where T : ObjectBase
            => _HasObjectPool(new TypeNamePair(typeof(T)));

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Type objectType)
        {
            if (objectType == null) throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            return _HasObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>(string name) where T : ObjectBase
            => _HasObjectPool(new TypeNamePair(typeof(T), name));

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Type objectType, string name)
        {
            if (objectType == null) throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            return _HasObjectPool(new TypeNamePair(objectType, name));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null)
                throw new FuException("检查条件不能为空.");

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
        public IObjectPool<T> GetObjectPool<T>() where T : ObjectBase
            => (IObjectPool<T>)_GetObjectPool(new TypeNamePair(typeof(T)));

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Type objectType)
        {
            if (objectType == null) throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            return _GetObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public IObjectPool<T> GetObjectPool<T>(string name) where T : ObjectBase
            => (IObjectPool<T>)_GetObjectPool(new TypeNamePair(typeof(T), name));

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Type objectType, string name)
        {
            if (objectType == null) throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            return _GetObjectPool(new TypeNamePair(objectType, name));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Predicate<ObjectPoolBase> condition)
        {
            if (condition == null) throw new FuException("检查条件不能为空.");

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
            if (condition == null) throw new FuException("检查条件不能为空.");

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
            if (condition == null) throw new FuException("检查条件不能为空.");
            if (results   == null) throw new FuException("结果列表不能为空.");

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

                results.Sort(m_ObjPoolComparer);
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
            if (results == null) throw new FuException("结果列表不能为空.");

            results.Clear();
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                results.Add(objPool);
            }

            if (sort)
                results.Sort(m_ObjPoolComparer);
        }

        #endregion

        #region 创建对象池

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(bool allowSpawnInUse = false) where T : ObjectBase
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
            return _CreateObjectPool(objectType, string.Empty, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, DefaultExpireTime, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(int capacity, bool allowSpawnInUse = false) where T : ObjectBase
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
        public IObjectPool<T> CreateObjectPool<T>(float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
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
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, int capacity, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, int capacity, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, float expireTime, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
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
        public IObjectPool<T> CreateObjectPool<T>(int capacity, int priority, bool allowSpawnInUse = false) where T : ObjectBase
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
        public IObjectPool<T> CreateObjectPool<T>(float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
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
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, int capacity, float expireTime, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, int capacity, float expireTime, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, expireTime, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, int capacity, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, int capacity, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, DefaultExpireTime, capacity, DefaultExpireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, expireTime, DefaultCapacity, expireTime, priority);
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
        public IObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
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
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, int capacity, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, expireTime, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string name, float autoReleaseInterval, int capacity, float expireTime, int priority, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return _CreateObjectPool<T>(name, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string name, float autoReleaseInterval, int capacity, float expireTime, int priority, bool allowSpawnInUse = false)
        {
            return _CreateObjectPool(objectType, name, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
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
                throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            return _DestroyObjectPool(new TypeNamePair(objectType));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="name">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(string name) where T : ObjectBase
        {
            return _DestroyObjectPool(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(Type objectType, string name)
        {
            if (objectType == null)
                throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            return _DestroyObjectPool(new TypeNamePair(objectType, name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(IObjectPool<T> objectPool) where T : ObjectBase
        {
            if (objectPool == null) throw new FuException("对象池为不能为空.");
            return _DestroyObjectPool(new TypeNamePair(typeof(T), objectPool.Name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(ObjectPoolBase objectPool)
        {
            if (objectPool == null) throw new FuException("对象池为不能为空.");
            return _DestroyObjectPool(new TypeNamePair(objectPool.ObjectType, objectPool.Name));
        }

        #endregion

        #region 释放对象池

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        public void Release()
        {
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
        /// <param name="name">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        private IObjectPool<T> _CreateObjectPool<T>(string name, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : ObjectBase
        {
            var typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasObjectPool<T>(name))
                throw new FuException(Utility.Text.Format("对象池 '{0}' 已存在.", typeNamePair));

            var objectPool = new ObjectPool<T>(name, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjPoolDict.Add(typeNamePair, objectPool);
            return objectPool;
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="name">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns></returns>
        /// <exception cref="FuException"></exception>
        private ObjectPoolBase _CreateObjectPool(Type objectType, string name, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime, int priority)
        {
            if (objectType == null) throw new FuException("对象类型不能为空.");

            if (!typeof(ObjectBase).IsAssignableFrom(objectType))
                throw new FuException(Utility.Text.Format("对象类型 '{0}' 不是 ObjectBase 的子类.", objectType.FullName));

            var typeNamePair = new TypeNamePair(objectType, name);
            if (HasObjectPool(objectType, name))
                throw new FuException(Utility.Text.Format("对象池 '{0}' 已存在.", typeNamePair));

            var objectPoolType = typeof(ObjectPool<>).MakeGenericType(objectType);
            var objectPool     = (ObjectPoolBase)Activator.CreateInstance(objectPoolType, name, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjPoolDict.Add(typeNamePair, objectPool);
            return objectPool;
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <returns>是否销毁对象池成功。</returns>
        private bool _DestroyObjectPool(TypeNamePair typeNamePair)
        {
            if (!m_ObjPoolDict.TryGetValue(typeNamePair, out var objectPool))
                return false;

            objectPool.Shutdown();
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