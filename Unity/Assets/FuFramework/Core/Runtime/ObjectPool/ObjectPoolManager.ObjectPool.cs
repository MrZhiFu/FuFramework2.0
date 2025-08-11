using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    internal sealed partial class ObjectPoolManager
    {
        /// <summary>
        /// 一个具体存放T类型对象的对象池。继承于ObjectPoolBase，实现了IObjectPool接口
        /// </summary>
        /// <typeparam name="T">对象池中的对象类型。</typeparam>
        private sealed class ObjectPool<T> : ObjectPoolBase, IObjectPool<T> where T : ObjectBase
        {
            /// 存储对象的多值字典，key为对象名称，value为对象(可为多个)。
            /// 允许同一个对象名称对应多个对象实例。这对于需要管理具有相同名称的多个对象（如子弹、特效等）非常重要，能够支持高效的对象复用
            private readonly FuMultiDictionary<string, Object<T>> m_ObjectMultiDict;

            /// 存储目标对象与其对应的内部对象的字典，key为标对象，value为对应的内部对象.
            private readonly Dictionary<object, Object<T>> m_TargetObjectDict;

            /// 缓存可以释放的对象。
            private readonly List<T> m_CachedCanReleaseObjectList;

            /// 缓存经过筛选函数后可以释放的对象
            private readonly List<T> m_CachedToReleaseObjectList;

            /// 默认释放对象的筛选函数。
            private readonly ReleaseObjectFilterCallback<T> m_DefaultReleaseObjectFilterCallback;


            /// 对象池的容量。
            private int m_Capacity;

            /// 对象池中的对象过期时间秒数。
            private float m_ExpireTime;


            /// 对象池每次轮询中自动释放可释放对象的计时器。
            private float m_AutoReleaseTimer;

            /// 获取或设置对象池每次轮询中自动释放可释放对象的间隔秒数。
            public override float AutoReleaseInterval { get; set; }

            /// 获取或设置对象池的优先级。该优先级会影响该池子在对象池管理器中卸载的顺序。
            public override int Priority { get; set; }

            /// <summary>
            /// 获取对象池中的对象时，是否允许获取正在被使用的对象。一般都为false。
            /// false--对象只能在回收后才能再次被获取，即池中会存在多个同名对象;
            /// true--对象能在未回收的状态下就能再次被获取，这样会使得池中的对象永远只有一个
            /// </summary>
            public override bool AllowSpawnInUse { get; }

            /// 获取对象池对象类型。
            public override Type ObjectType => typeof(T);

            /// 获取对象池中对象的数量。
            public override int Count => m_TargetObjectDict.Count;

            /// 获取对象池中能被释放的对象的数量。
            public override int CanReleaseCount
            {
                get
                {
                    _GetCanReleaseObjects(m_CachedCanReleaseObjectList);
                    return m_CachedCanReleaseObjectList.Count;
                }
            }

            /// 获取或设置对象池的容量。
            public override int Capacity
            {
                get => m_Capacity;
                set
                {
                    if (value      < 0) throw new FuException("对象池容量不能小于0.");
                    if (m_Capacity == value) return;

                    m_Capacity = value;
                    Release();
                }
            }

            /// 获取或设置对象池对象过期秒数。
            public override float ExpireTime
            {
                get => m_ExpireTime;
                set
                {
                    if (value < 0f) throw new FuException("对象过期秒数不能小于0.");
                    if (Mathf.Approximately(ExpireTime, value)) return;

                    m_ExpireTime = value;
                    Release();
                }
            }

            /// <summary>
            /// 初始化对象池的新实例。
            /// </summary>
            /// <param name="name">对象池名称。</param>
            /// <param name="allowSpawnInUse">是否允许对象被多次获取。</param>
            /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
            /// <param name="capacity">对象池的容量。</param>
            /// <param name="expireTime">对象池对象过期秒数。</param>
            /// <param name="priority">对象池的优先级。</param>
            public ObjectPool(string name, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime, int priority) : base(name)
            {
                m_ObjectMultiDict                    = new FuMultiDictionary<string, Object<T>>();
                m_TargetObjectDict                         = new Dictionary<object, Object<T>>();
                m_DefaultReleaseObjectFilterCallback = _DefaultReleaseObjectFilterCallback;
                m_CachedCanReleaseObjectList         = new List<T>();
                m_CachedToReleaseObjectList          = new List<T>();

                AllowSpawnInUse     = allowSpawnInUse;
                AutoReleaseInterval = autoReleaseInterval;
                Capacity            = capacity;
                ExpireTime          = expireTime;
                Priority            = priority;
                m_AutoReleaseTimer  = 0f;
            }

            /// <summary>
            /// 对象池轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            internal override void Update(float elapseSeconds, float realElapseSeconds)
            {
                m_AutoReleaseTimer += realElapseSeconds;
                if (m_AutoReleaseTimer < AutoReleaseInterval) return;
                Release();
            }

            /// <summary>
            /// 关闭并清理对象池。
            /// </summary>
            internal override void Shutdown()
            {
                foreach (var (_, internalObject) in m_TargetObjectDict)
                {
                    internalObject.Release(true);
                    ReferencePool.Release(internalObject);
                }

                m_ObjectMultiDict.Clear();
                m_TargetObjectDict.Clear();
                m_CachedCanReleaseObjectList.Clear();
                m_CachedToReleaseObjectList.Clear();
            }

            /// <summary>
            /// 创建并注册一个对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否已被获取。</param>
            public void Register(T obj, bool spawned)
            {
                if (obj == null) throw new FuException("要创建并注册对象不能为空.");

                var internalObject = Object<T>.Create(obj, spawned);
                m_ObjectMultiDict.Add(obj.Name, internalObject);
                m_TargetObjectDict.Add(obj.Target, internalObject);

                if (Count > m_Capacity)
                    Release();
            }

            /// <summary>
            /// 检查对象。
            /// </summary>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn() => CanSpawn(string.Empty);

            /// <summary>
            /// 检查对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn(string name)
            {
                if (name == null) throw new FuException("对象名称不能为空.");

                if (!m_ObjectMultiDict.TryGetValue(name, out var objectRange)) return false;

                foreach (var internalObject in objectRange)
                {
                    if (AllowSpawnInUse || !internalObject.IsInUse)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <returns>要获取的对象。</returns>
            public T Spawn() => Spawn(string.Empty);

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要获取的对象。</returns>
            public T Spawn(string name)
            {
                if (name == null) throw new FuException("对象名称不能为空.");

                if (!m_ObjectMultiDict.TryGetValue(name, out var objectRange)) return null;

                foreach (var internalObject in objectRange)
                {
                    if (AllowSpawnInUse || !internalObject.IsInUse)
                        return internalObject.Spawn();
                }

                return null;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="obj">要回收的对象。</param>
            public void Recycle(T obj)
            {
                if (obj == null) throw new FuException("对象不能为空.");
                Recycle(obj.Target);
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="target">要回收的对象。</param>
            public void Recycle(object target)
            {
                if (target == null) throw new FuException("要回收的目标对象不能为空.");

                var internalObject = _GetObject(target);
                if (internalObject == null)
                    throw new FuException(Utility.Text.Format("在对象池“{0}”中找不到目标，目标类型为“{1}”，目标值为“{2}'.", new TypeNamePair(typeof(T), Name),
                                                                                 target.GetType().FullName, target));
                internalObject.Recycle();
                if (Count > m_Capacity && internalObject.SpawnCount <= 0)
                {
                    Release();
                }
            }

            /// <summary>
            /// 设置对象是否被加锁。
            /// </summary>
            /// <param name="obj">要设置被加锁的对象。</param>
            /// <param name="locked">是否被加锁。</param>
            public void SetLocked(T obj, bool locked)
            {
                if (obj == null) throw new FuException("对象不能为空.");
                SetLocked(obj.Target, locked);
            }

            /// <summary>
            /// 设置对象是否被加锁。
            /// </summary>
            /// <param name="target">要设置被加锁的对象。</param>
            /// <param name="locked">是否被加锁。</param>
            public void SetLocked(object target, bool locked)
            {
                if (target == null) throw new FuException("对象不能为空.");

                var internalObject = _GetObject(target);
                if (internalObject == null)
                    throw new FuException(Utility.Text.Format("在对象池“{0}”中未找到目标，目标类型为“{1}”，目标值为“{2}”.", new TypeNamePair(typeof(T), Name),
                                                                                 target.GetType().FullName, target));
                internalObject.Locked = locked;
            }

            /// <summary>
            /// 设置对象的优先级。
            /// </summary>
            /// <param name="obj">要设置优先级的对象。</param>
            /// <param name="priority">优先级。</param>
            public void SetPriority(T obj, int priority)
            {
                if (obj == null) throw new FuException("对象不能为空.");
                SetPriority(obj.Target, priority);
            }

            /// <summary>
            /// 设置对象的优先级。
            /// </summary>
            /// <param name="target">要设置优先级的对象。</param>
            /// <param name="priority">优先级。</param>
            public void SetPriority(object target, int priority)
            {
                if (target == null) throw new FuException("目标对象不能为空.");

                var internalObject = _GetObject(target);
                if (internalObject == null)
                    throw new FuException(Utility.Text.Format("在对象池“{0}”中未找到目标，目标类型为“{1}”，目标值为“{2}”..", new TypeNamePair(typeof(T), Name),
                                                                                 target.GetType().FullName, target));

                internalObject.Priority = priority;
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="obj">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(T obj)
            {
                if (obj == null) throw new FuException("目标对象不能为空.");
                return ReleaseObject(obj.Target);
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="target">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(object target)
            {
                
                if (target == null) throw new FuException("目标对象不能为空.");

                var internalObject = _GetObject(target);
                if (internalObject == null) return false;

                
                if (internalObject.IsInUse || internalObject.Locked || !internalObject.CustomCanReleaseFlag)
                    return false;

                FuLog.Info("真正释放对象池中的可释放对象 '{0}'", internalObject.Name);
                
                m_ObjectMultiDict.Remove(internalObject.Name, internalObject);
                m_TargetObjectDict.Remove(internalObject.Peek().Target);

                internalObject.Release(false);
                ReferencePool.Release(internalObject);
                return true;
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            public override void Release()
            {
                var overCapacity = Count - m_Capacity; // 指定超过容量的数量为尝试释放的对象数量
                Release(overCapacity, m_DefaultReleaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="toReleaseCount">尝试释放对象数量。</param>
            public override void Release(int toReleaseCount)
            {
                Release(toReleaseCount, m_DefaultReleaseObjectFilterCallback);
            }

            /// <summary>
            /// 释放对象池中的可释放对象。
            /// </summary>
            /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
            public void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                Release(Count - m_Capacity, releaseObjectFilterCallback);
            }

            /// <summary>
            /// 尝试释放对象池中的可释放对象。
            /// </summary>
            /// <param name="toReleaseCount">尝试释放对象数量。</param>
            /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
            public void Release(int toReleaseCount, ReleaseObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                if (releaseObjectFilterCallback == null)
                    throw new FuException("释放对象筛选函数不能为空.");

                if (toReleaseCount < 0)
                    toReleaseCount = 0;
                
                var expireTime = DateTime.MinValue;
                if (m_ExpireTime < float.MaxValue)
                    expireTime = DateTime.UtcNow.AddSeconds(-m_ExpireTime);

                
                m_AutoReleaseTimer = 0f;
                _GetCanReleaseObjects(m_CachedCanReleaseObjectList);
                FuLog.Info("尝试释放对象池中的可释放对象-对象数量: '{0}'", m_CachedCanReleaseObjectList.Count);

                // 筛选需要释放的对象
                var toReleaseObjects = releaseObjectFilterCallback(m_CachedCanReleaseObjectList, toReleaseCount, expireTime);
                if (toReleaseObjects is not { Count: > 0 }) return;

                foreach (var toReleaseObject in toReleaseObjects)
                {
                    ReleaseObject(toReleaseObject);
                }
            }

            /// <summary>
            /// 释放对象池中的所有未使用对象。
            /// </summary>
            public override void ReleaseAllUnused()
            {
                m_AutoReleaseTimer = 0f;
                _GetCanReleaseObjects(m_CachedCanReleaseObjectList);
                foreach (var toReleaseObject in m_CachedCanReleaseObjectList)
                {
                    ReleaseObject(toReleaseObject);
                }
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
                    foreach (var internalObject in objectRang)
                    {
                        results.Add(new ObjectInfo(internalObject.Name, internalObject.Locked, internalObject.CustomCanReleaseFlag,
                                                   internalObject.Priority, internalObject.LastUseTime, internalObject.SpawnCount));
                    }
                }

                return results.ToArray();
            }

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <param name="target"></param>
            /// <returns></returns>
            private Object<T> _GetObject(object target)
            {
                if (target == null) throw new FuException("目标对象不能为空.");
                return m_TargetObjectDict.GetValueOrDefault(target);
            }

            /// <summary>
            /// 获取对象池中能被释放的对象的数量
            /// </summary>
            /// <param name="results">结果列表</param>
            private void _GetCanReleaseObjects(List<T> results)
            {
                if (results == null) throw new FuException("结果列表不能为空.");

                results.Clear();
                foreach (var (_, internalObject) in m_TargetObjectDict)
                {
                    if (internalObject.IsInUse || internalObject.Locked || !internalObject.CustomCanReleaseFlag)
                        continue;

                    results.Add(internalObject.Peek());
                }
            }

            /// <summary>
            /// 释放对象筛选函数。
            /// 筛选条件：
            /// 1.过期的对象先释放。
            /// 2.优先级小的先释放。或者优先级相等，但是最后使用时间更早的对象先释放。
            /// </summary>
            /// <typeparam name="T">对象类型。</typeparam>
            /// <param name="candidateObjects">要筛选的对象集合。</param>
            /// <param name="toReleaseCount">需要释放的对象数量。</param>
            /// <param name="expireTime">对象过期参考时间。</param>
            /// <returns>经筛选需要释放的对象集合。</returns>
            private List<T> _DefaultReleaseObjectFilterCallback(List<T> candidateObjects, int toReleaseCount, DateTime expireTime)
            {
                m_CachedToReleaseObjectList.Clear();

                // 第一阶段：根据最后使用时间筛选过期对象。
                // 如果对象最后使用时间 > 过期时间，说明了对象存活还不够字段ExpireTime指定的时间，则继续筛选。
                if (expireTime > DateTime.MinValue)
                {
                    for (var i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        if (candidateObjects[i].LastUseTime > expireTime) continue;
                        m_CachedToReleaseObjectList.Add(candidateObjects[i]);
                        candidateObjects.RemoveAt(i);
                    }

                    toReleaseCount -= m_CachedToReleaseObjectList.Count;
                }

                // 第二阶段：根据优先级和最后使用时间，在剩余可释放候选列表中，找到超过需要释放的数量的对象，加入到待释放列表中
                for (var i = 0; toReleaseCount > 0 && i < candidateObjects.Count; i++)
                {
                    for (var j = i + 1; j < candidateObjects.Count; j++)
                    {
                        // 如果当前对象的优先级高于下一个对象，或者优先级相同但最后使用时间更晚，则交换位置。
                        if (candidateObjects[i].Priority > candidateObjects[j].Priority ||
                            candidateObjects[i].Priority    == candidateObjects[j].Priority &&
                            candidateObjects[i].LastUseTime > candidateObjects[j].LastUseTime)
                        {
                            (candidateObjects[i], candidateObjects[j]) = (candidateObjects[j], candidateObjects[i]);
                        }
                    }
                    
                    // 上面一层循环结束后，candidateObjects[i]的位置就是优先级最低的对象，加入到待释放列表中
                    m_CachedToReleaseObjectList.Add(candidateObjects[i]);
                    toReleaseCount--;
                }

                return m_CachedToReleaseObjectList;
            }
        }
    }
}