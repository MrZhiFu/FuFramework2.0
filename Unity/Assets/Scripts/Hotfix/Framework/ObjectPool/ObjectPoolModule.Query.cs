using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        #region 获取对象池

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>() where T : ObjectBase
        {
            return HasObjectPoolInternal(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 检查是否存在对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>是否存在对象池。</returns>
        public bool HasObjectPool<T>(string poolName) where T : ObjectBase
        {
            return HasObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>() where T : ObjectBase
        {
            return (ObjectPool<T>)GetObjectPoolInternal(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 获取对象池。
        /// </summary>
        /// <typeparam name="T">对象类型。</typeparam>
        /// <param name="poolName">对象池名称。</param>
        /// <returns>要获取的对象池。</returns>
        public ObjectPool<T> GetObjectPool<T>(string poolName) where T : ObjectBase
        {
            return (ObjectPool<T>)GetObjectPoolInternal(new TypeNamePair(typeof(T), poolName));
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
    }
}
