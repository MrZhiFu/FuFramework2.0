using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池管理模块。
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
        /// 对象池默认自动释放间隔秒数(默认不自动释放)。
        /// </summary>
        private const float DefaultAutoReleaseInterval = float.MaxValue;

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
        /// 初始化。
        /// </summary>
        protected internal override void OnInit()
        {
            Application.lowMemory += OnLowMemory;
        }

        /// <summary>
        /// 帧更新。
        /// </summary>
        /// <param name="deltaTime">帧间隔时间。</param>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        protected internal override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var (_, objPool) in m_ObjPoolDict)
            {
                objPool.Update(unscaledDeltaTime);
            }
        }

        /// <summary>
        /// 释放。
        /// </summary>
        protected internal override void OnDispose()
        {
            foreach (var (typeNamePair, objPool) in m_ObjPoolDict)
            {
                try
                {
                    objPool.OnDispose();
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 释放对象池 {typeNamePair} 时出现异常: {e.Message}");
                }
            }

            m_ObjPoolDict.Clear();
            m_CachedObjPoolList.Clear();

            Application.lowMemory -= OnLowMemory;
        }

        /// <summary>
        /// 低内存回调。
        /// </summary>
        private void OnLowMemory()
        {
            FuLogger.LogInfo("[ObjectPoolModule] 低内存警告, 释放对象池中所有未使用的资源...");
            ReleaseAllUnused();
        }

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

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <param name="typeNamePair">类型与名称的组合。</param>
        /// <returns>是否存在对象池。</returns>
        private bool HasObjectPoolInternal(TypeNamePair typeNamePair)
        {
            return m_ObjPoolDict.ContainsKey(typeNamePair);
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <param name="typeNamePair">类型与名称的组合。</param>
        /// <returns>要获取的对象池。</returns>
        private ObjectPoolBase GetObjectPoolInternal(TypeNamePair typeNamePair)
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
        private ObjectPool<T> CreateObjectPoolInternal<T>(string poolName, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime,
                                                          int priority) where T : ObjectBase
        {
            var typeNamePair = new TypeNamePair(typeof(T), poolName);
            if (HasObjectPoolInternal(typeNamePair))
                throw new InvalidOperationException($"[ObjectPoolModule] 对象池 '{typeNamePair}' 已存在, 不可重复创建.");

            var objectPool = new ObjectPool<T>(poolName, allowSpawnInUse, autoReleaseInterval, capacity, expireTime, priority);
            m_ObjPoolDict.Add(typeNamePair, objectPool);
            return objectPool;
        }

        /// <summary>
        /// 销毁对象池。
        /// </summary>
        /// <param name="typeNamePair">类型与名称的组合。</param>
        /// <returns>是否销毁对象池成功。</returns>
        private bool DestroyObjectPoolInternal(TypeNamePair typeNamePair)
        {
            if (!m_ObjPoolDict.TryGetValue(typeNamePair, out var objectPool)) return false;
            objectPool.OnDispose();
            return m_ObjPoolDict.Remove(typeNamePair);
        }

        /// <summary>
        /// 对象池比较器。
        /// </summary>
        /// <param name="a">对象池a。</param>
        /// <param name="b">对象池b。</param>
        /// <returns>优先级比较结果。</returns>
        private static int ObjectPoolComparer(ObjectPoolBase a, ObjectPoolBase b)
        {
            return a.Priority.CompareTo(b.Priority);
        }
    }
}
