using System;
using Hotfix.Framework.Core;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池中对象的存取与管理。
    /// 功能：
    ///     1. 提供对象的注册、获取与回收接口。
    ///     2. 提供对象的锁定、优先级设置与信息查询。
    /// </summary>
    public sealed partial class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
    {
        /// <summary>
        /// 注册一个对象到对象池中。
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <param name="inUse">对象注册时是否已处于使用中。</param>
        public void Register(T obj, bool inUse)
        {
            if (obj        == null) throw new InvalidOperationException("[ObjectPoolModule] 要创建并注册对象不能为空.");
            if (obj.Target == null) throw new InvalidOperationException("[ObjectPoolModule] 要注册的对象目标不能为空.");

            // 同一目标对象不允许重复注册，避免双字典写入不一致
            if (m_TargetObjectDict.ContainsKey(obj.Target))
                throw new InvalidOperationException($"[ObjectPoolModule] 对象池 '{new TypeNamePair(typeof(T), Name)}' 中已存在目标对象.");

            m_ObjectMultiDict.Add(obj.Name, obj);
            m_TargetObjectDict.Add(obj.Target, obj);

            // 对象是否已处于使用中，若是则直接走一次生成流程（计数+1、刷新最后使用时间、触发 OnSpawn）
            if (inUse) obj.Spawn();

            if (Count > m_Capacity)
                DisposeOverCapacity();
        }

        /// <summary>
        /// 从对象池获取对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要获取的对象，池中无可用对象时返回null。</returns>
        public T Spawn(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

            if (!m_ObjectMultiDict.TryGetValue(name, out var objects)) return null;

            foreach (var obj in objects)
            {
                // 如果允许获取正在使用的对象，则直接获取。
                if (AllowSpawnInUse)
                {
                    obj.Spawn();
                    return obj;
                }

                // 如果对象没有正在使用，则直接获取。
                if (!obj.IsInUse)
                {
                    obj.Spawn();
                    return obj;
                }
            }

            return null;
        }

        /// <summary>
        /// 回收对象。
        /// </summary>
        /// <param name="obj">要回收的对象。</param>
        public void Recycle(T obj)
        {
            if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");
            Recycle(obj.Target);
        }

        /// <summary>
        /// 回收对象。
        /// </summary>
        /// <param name="target">要回收的对象。</param>
        public void Recycle(object target)
        {
            if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 要回收的目标对象不能为空.");

            var obj = GetObject(target);
            if (obj == null)
                throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中找不到目标对象 '{target.GetType().FullName}'.");

            obj.Recycle();
            if (Count > m_Capacity && obj.SpawnCount <= 0)
            {
                DisposeOverCapacity();
            }
        }

        /// <summary>
        /// 检查对象是否可获取。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要检查的对象是否可获取。</returns>
        public bool CanSpawn(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

            if (!m_ObjectMultiDict.TryGetValue(name, out var objects)) return false;

            foreach (var obj in objects)
            {
                // 如果允许多次获取，则直接返回true。
                if (AllowSpawnInUse) return true;

                // 如果对象没有正在使用，则直接返回true。
                if (!obj.IsInUse) return true;
            }

            return false;
        }

        /// <summary>
        /// 设置对象是否被加锁。
        /// </summary>
        /// <param name="obj">要设置被加锁的对象。</param>
        /// <param name="locked">是否被加锁。</param>
        public void SetLocked(T obj, bool locked)
        {
            if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");
            SetLocked(obj.Target, locked);
        }

        /// <summary>
        /// 设置对象是否被加锁。
        /// </summary>
        /// <param name="target">要设置被加锁的对象。</param>
        /// <param name="locked">是否被加锁。</param>
        public void SetLocked(object target, bool locked)
        {
            if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");

            var obj = GetObject(target);
            if (obj == null)
                throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”.");
            obj.Locked = locked;
        }

        /// <summary>
        /// 设置对象的优先级。
        /// </summary>
        /// <param name="obj">要设置优先级的对象。</param>
        /// <param name="priority">优先级。</param>
        public void SetPriority(T obj, int priority)
        {
            if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 对象不能为空.");
            SetPriority(obj.Target, priority);
        }

        /// <summary>
        /// 设置对象的优先级。
        /// </summary>
        /// <param name="target">要设置优先级的对象。</param>
        /// <param name="priority">优先级。</param>
        public void SetPriority(object target, int priority)
        {
            if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");

            var obj = GetObject(target);
            if (obj == null)
                throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”.");
            obj.Priority = priority;
        }

        /// <summary>
        /// 获取所有对象信息。
        /// </summary>
        /// <returns>所有对象信息。</returns>
        public override ObjectInfo[] GetAllObjectInfos()
        {
            var results = new List<ObjectInfo>();
            foreach (var (_, objectRang) in m_ObjectMultiDict)
            {
                foreach (var obj in objectRang)
                {
                    results.Add(new ObjectInfo(obj.Name, obj.Target, obj.Locked, obj.CustomCanDisposeFlag, obj.Priority, obj.LastUseTime, obj.SpawnCount));
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// 获取对象。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <returns>目标对象对应的池内对象，不存在时返回null。</returns>
        private T GetObject(object target)
        {
            if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");
            return m_TargetObjectDict.GetValueOrDefault(target);
        }
    }
}