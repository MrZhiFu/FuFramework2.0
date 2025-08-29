using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 对象池组件。
    /// "只允许单次获取"代表对象池中的对象只能在回收后才能再次被获取;
    /// "允许多次获取"代表对象池对中的对象能在未回收的状态下就能再次被获取
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/ObjectPoolComponent(对象池管理器)")]
    public sealed class ObjectPoolComponent : FuComponent
    {
        protected internal override int Priority => 20000;

        /// 对象池管理器
        private IObjectPoolManager m_ObjectPoolManager;

        /// <summary>
        /// 获取对象池数量。
        /// </summary>
        public int Count => m_ObjectPoolManager.Count;

        protected internal override void OnInit()
        {
            m_ObjectPoolManager = FuEntry.GetModule<IObjectPoolManager>();
            if (m_ObjectPoolManager != null) return;
            Log.Fatal("对象池管理器为空.");
        }
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }
        protected internal override void OnShutdown(ShutdownType shutdownType)
        {
            // throw new NotImplementedException();
        }

        #region 获取对象池相关

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>() where T : ObjectBase => m_ObjectPoolManager.HasObjectPool<T>();

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Type objectType) => m_ObjectPoolManager.HasObjectPool(objectType);

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>(string poolName) where T : ObjectBase => m_ObjectPoolManager.HasObjectPool<T>(poolName);

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Type objectType, string poolName) => m_ObjectPoolManager.HasObjectPool(objectType, poolName);

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool(Predicate<ObjectPoolBase> condition) => m_ObjectPoolManager.HasObjectPool(condition);

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>要获取的对象池。</returns>
        public IObjectPool<T> GetObjectPool<T>() where T : ObjectBase => m_ObjectPoolManager.GetObjectPool<T>();

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Type objectType) => m_ObjectPoolManager.GetObjectPool(objectType);

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public IObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase => m_ObjectPoolManager.GetObjectPool<T>(poolName);

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Type objectType, string poolName) => m_ObjectPoolManager.GetObjectPool(objectType, poolName);

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase GetObjectPool(Predicate<ObjectPoolBase> condition) => m_ObjectPoolManager.GetObjectPool(condition);

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPoolBase[] GetObjectPools(Predicate<ObjectPoolBase> condition) => m_ObjectPoolManager.GetObjectPools(condition);

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="results">要获取的对象池。</param>
        public void GetObjectPools(Predicate<ObjectPoolBase> condition, List<ObjectPoolBase> results)
            => m_ObjectPoolManager.GetObjectPools(condition, results);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        public ObjectPoolBase[] GetAllObjectPools() => m_ObjectPoolManager.GetAllObjectPools();

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="results">所有对象池。</param>
        public void GetAllObjectPools(List<ObjectPoolBase> results) => m_ObjectPoolManager.GetAllObjectPools(results);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <returns>所有对象池。</returns>
        public ObjectPoolBase[] GetAllObjectPools(bool sort) => m_ObjectPoolManager.GetAllObjectPools(sort);

        /// <summary>
        /// 获取所有对象池。
        /// </summary>
        /// <param name="sort">是否根据对象池的优先级排序。</param>
        /// <param name="results">所有对象池。</param>
        public void GetAllObjectPools(bool sort, List<ObjectPoolBase> results) => m_ObjectPoolManager.GetAllObjectPools(sort, results);

        #endregion

        #region 创建对象池

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>() where T : ObjectBase => m_ObjectPoolManager.CreateObjectPool<T>();

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType) => m_ObjectPoolManager.CreateObjectPool(objectType);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName) where T : ObjectBase => m_ObjectPoolManager.CreateObjectPool<T>(poolName);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName) => m_ObjectPoolManager.CreateObjectPool(objectType, poolName);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(int capacity) where T : ObjectBase => m_ObjectPoolManager.CreateObjectPool<T>(capacity);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity) => m_ObjectPoolManager.CreateObjectPool(objectType, capacity);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(float expireTime) where T : ObjectBase => m_ObjectPoolManager.CreateObjectPool<T>(expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, float expireTime) => m_ObjectPoolManager.CreateObjectPool(objectType, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, int capacity) where T : ObjectBase
            => m_ObjectPoolManager.CreateObjectPool<T>(poolName, capacity);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity)
            => m_ObjectPoolManager.CreateObjectPool(objectType, poolName, capacity);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, float expireTime) where T : ObjectBase
            => m_ObjectPoolManager.CreateObjectPool<T>(poolName, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, float expireTime)
            => m_ObjectPoolManager.CreateObjectPool(objectType, poolName, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime) where T : ObjectBase
            => m_ObjectPoolManager.CreateObjectPool<T>(capacity, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, float expireTime)
            => m_ObjectPoolManager.CreateObjectPool(objectType, capacity, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(int capacity, int priority) where T : ObjectBase
            => m_ObjectPoolManager.CreateObjectPool<T>(capacity, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, int priority)
            => m_ObjectPoolManager.CreateObjectPool(objectType, capacity, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(float expireTime, int priority) where T : ObjectBase
            => m_ObjectPoolManager.CreateObjectPool<T>(expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, float expireTime, int priority) 
            => m_ObjectPoolManager.CreateObjectPool(objectType, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime) where T : ObjectBase 
            => m_ObjectPoolManager.CreateObjectPool<T>(poolName, capacity, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, float expireTime) 
            => m_ObjectPoolManager.CreateObjectPool(objectType, poolName, capacity, expireTime);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, int priority) where T : ObjectBase =>
            m_ObjectPoolManager.CreateObjectPool<T>(poolName, capacity, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, int priority) =>
            m_ObjectPoolManager.CreateObjectPool(objectType, poolName, capacity, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, float expireTime, int priority) where T : ObjectBase =>
            m_ObjectPoolManager.CreateObjectPool<T>(poolName, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, float expireTime, int priority) =>
            m_ObjectPoolManager.CreateObjectPool(objectType, poolName, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(int capacity, float expireTime, int priority) where T : ObjectBase =>
            m_ObjectPoolManager.CreateObjectPool<T>(capacity, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, int capacity, float expireTime, int priority) =>
            m_ObjectPoolManager.CreateObjectPool(objectType, capacity, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, int capacity, float expireTime, int priority) where T : ObjectBase =>
            m_ObjectPoolManager.CreateObjectPool<T>(poolName, capacity, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, int capacity, float expireTime, int priority) =>
            m_ObjectPoolManager.CreateObjectPool(objectType, poolName, capacity, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public IObjectPool<T> CreateObjectPool<T>(string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority) where T : ObjectBase =>
            m_ObjectPoolManager.CreateObjectPool<T>(poolName, autoReleaseInterval, capacity, expireTime, priority);

        /// <summary>
        /// 创建对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">对象池名称。</param>
        /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTime">对象池对象过期秒数。</param>
        /// <param name="priority">对象池的优先级。</param>
        /// <returns>创建的对象池。</returns>
        public ObjectPoolBase CreateObjectPool(Type objectType, string poolName, float autoReleaseInterval, int capacity, float expireTime, int priority) =>
            m_ObjectPoolManager.CreateObjectPool(objectType, poolName, autoReleaseInterval, capacity, expireTime, priority);

        #endregion

        #region 销毁对象池

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>() where T : ObjectBase => m_ObjectPoolManager.DestroyObjectPool<T>();

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(Type objectType) => m_ObjectPoolManager.DestroyObjectPool(objectType);

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(string poolName) where T : ObjectBase => m_ObjectPoolManager.DestroyObjectPool<T>(poolName);

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectType">对象类型。</param>
        /// <param name="poolName">要销毁的对象池名称。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(Type objectType, string poolName) => m_ObjectPoolManager.DestroyObjectPool(objectType, poolName);

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool<T>(IObjectPool<T> objectPool) where T : ObjectBase => m_ObjectPoolManager.DestroyObjectPool(objectPool);

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="objectPool">要销毁的对象池。</param>
        /// <returns>是否销毁对象池成功。</returns>
        public bool DestroyObjectPool(ObjectPoolBase objectPool) => m_ObjectPoolManager.DestroyObjectPool(objectPool);

        #endregion

        #region 释放对象池

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        public void Release()
        {
            Log.Info("[ObjectPoolComponent]释放对象池中可释放对象...");
            m_ObjectPoolManager.Release();
        }

        /// <summary>
        /// 释放对象池中的所有未使用对象。
        /// </summary>
        public void ReleaseAllUnused()
        {
            Log.Info("[ObjectPoolComponent]释放对象池中的所有未使用对象...");
            m_ObjectPoolManager.ReleaseAllUnused();
        }

        #endregion
    }
}