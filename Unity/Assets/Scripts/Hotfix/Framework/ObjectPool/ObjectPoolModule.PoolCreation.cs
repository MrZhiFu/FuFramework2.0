using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        #region 创建对象池

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(string.Empty, allowSpawnInUse, DefaultAutoReleaseInterval, DefaultCapacity, DefaultExpireTime, DefaultPriority);
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
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, DefaultAutoReleaseInterval, DefaultCapacity, DefaultExpireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime = float.MaxValue, bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(string.Empty, allowSpawnInUse, DefaultAutoReleaseInterval, capacity, expireTime, DefaultPriority);
        }

        /// <summary>
        /// 创建对象池(命名池 + 容量 + 过期时间 + 优先级，自动释放间隔取默认)。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <param name="allowSpawnInUse">是否允许对象在使用时获取。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority,
                                                 bool allowSpawnInUse = false) where T : ObjectBase
        {
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, DefaultAutoReleaseInterval, capacity, expireTime, priority);
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
            return CreateObjectPoolInternal<T>(poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
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
            return DestroyObjectPoolInternal(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(string poolName) where T : ObjectBase
        {
            return DestroyObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(ObjectPool<T> objectPool) where T : ObjectBase
        {
            if (objectPool == null) throw new InvalidOperationException("[ObjectPoolModule] 对象池为不能为空.");
            return DestroyObjectPoolInternal(new TypeNamePair(typeof(T), objectPool.Name));
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(ObjectPoolBase objectPool)
        {
            if (objectPool == null) throw new InvalidOperationException("[ObjectPoolModule] 对象池为不能为空.");
            return DestroyObjectPoolInternal(new TypeNamePair(objectPool.ObjectType, objectPool.Name));
        }

        #endregion
    }
}
