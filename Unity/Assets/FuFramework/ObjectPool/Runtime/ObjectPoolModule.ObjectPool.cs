using System;
using UnityEngine;
using FuFramework.Core.Runtime;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Runtime
{
    public sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 具体管理对象的对象池。
        /// 功能：
        ///     1. 允许/禁止自动释放：可以设置池中空闲对象是否在一定时间后自动销毁，以节省内存。
        ///     2. 设置优先级：可以设置对象池的优先级，在需要强制释放对象时（如内存不足），优先释放低优先级池中的对象。
        /// </summary>
        /// <typeparam name="T">对象池中的对象类型。</typeparam>
        public sealed class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
        {
            /// 存储对象的多值字典，key为对象名称，value为对象(可为多个)。
            /// 允许同一个对象名称对应多个对象实例。这对于需要管理具有相同名称的多个对象（如子弹、特效等）非常重要，能够支持高效的对象复用
            private readonly FuMultiDictionary<string, Object<T>> m_ObjectMultiDict;

            /// 存储目标对象与其对应的内部对象的字典，key为目标对象，value为对应的内部对象.
            private readonly Dictionary<object, Object<T>> m_TargetObjectDict;

            /// 缓存当前所有可以释放的对象列表(未使用的、未加锁的、自定义标记为可释放的).
            private readonly List<T> m_CachedCanReleaseObjectList;

            /// 缓存经过筛选函数后最终决定要释放的对象列表
            private readonly List<T> m_CachedToReleaseObjectList;

            /// 默认释放对象的筛选函数(释放策略)。定义了如何从候选列表中选出要释放的对象（基于优先级和最后使用时间）。
            private readonly ReleaseObjectFilterCallback<T> m_DefaultReleaseObjectFilterCallback;


            /// 对象池的容量。
            private int m_Capacity;

            /// 对象过期时间秒数。一个对象闲置超过这个时间（秒），就会被标记为可释放。
            private float m_ExpireTime;


            /// 自动释放计时器。用于计时，每隔 AutoReleaseInterval 秒触发一次自动释放检查。
            private float m_AutoReleaseTimer;

            /// 获取或设置对象池每次轮询中自动释放可释放对象的间隔秒数。
            public override float AutoReleaseInterval { get; set; }

            /// 获取或设置对象池的优先级。该优先级会影响该池子在对象池管理模块中卸载的顺序。
            public override int Priority { get; set; }

            /// <summary>
            /// 获取对象池中的对象时，是否允许获取正在被使用的对象。一般都为false。
            /// false--对象只能在回收后才能再次被获取，即池中会存在多个同名对象;
            /// true --对象能在未回收的状态下就能再次被获取，这样会使得池中的对象只有一个，每次获取之后这个对象的引用计数++
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
                    if (value      < 0) throw new FuException("[ObjectPoolModule] 对象池容量不能小于0.");
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
                    if (value < 0f) throw new FuException("[ObjectPoolModule] 对象过期秒数不能小于0.");
                    if (Mathf.Approximately(ExpireTime, value)) return;

                    m_ExpireTime = value;
                    Release();
                }
            }

            /// <summary>
            /// 初始化对象池的新实例。
            /// </summary>
            /// <param name="name">对象池名称。</param>
            /// <param name="allowSpawnInUse">是否允许对象池中对象正在使用的状态下被获取。</param>
            /// <param name="autoReleaseInterval">对象池自动释放可释放对象的间隔秒数。</param>
            /// <param name="capacity">对象池的容量。</param>
            /// <param name="expireTime">对象池对象过期秒数。</param>
            /// <param name="priority">对象池的优先级。</param>
            public ObjectPool(string name, bool allowSpawnInUse, float autoReleaseInterval, int capacity, float expireTime, int priority) : base(name)
            {
                m_ObjectMultiDict                    = new FuMultiDictionary<string, Object<T>>();
                m_TargetObjectDict                   = new Dictionary<object, Object<T>>();
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
            /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
            internal override void Update(float unscaledDeltaTime)
            {
                m_AutoReleaseTimer += unscaledDeltaTime;

                // 每隔 AutoReleaseInterval 秒触发一次自动释放检查
                if (m_AutoReleaseTimer >= AutoReleaseInterval)
                {
                    Release();
                }
            }

            /// <summary>
            /// 关闭并清理对象池。
            /// </summary>
            internal override void OnDispose()
            {
                foreach (var (_, obj) in m_TargetObjectDict)
                {
                    obj.OnRelease();
                    ReferencePool.Runtime.ReferencePool.Release(obj);
                }

                m_ObjectMultiDict.Clear();
                m_TargetObjectDict.Clear();
                m_CachedCanReleaseObjectList.Clear();
                m_CachedToReleaseObjectList.Clear();
            }

            /// <summary>
            /// 注册一个对象到对象池中。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否提前生成。</param>
            public void Register(T obj, bool spawned)
            {
                if (obj == null) throw new FuException("[ObjectPoolModule] 要创建并注册对象不能为空.");

                var tempObj = Object<T>.Create(obj, spawned);
                m_ObjectMultiDict.Add(obj.Name, tempObj);
                m_TargetObjectDict.Add(obj.Target, tempObj);

                if (Count > m_Capacity)
                    Release();
            }

            /// <summary>
            /// 获取已存在的对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要获取的对象。</returns>
            public T Spawn(string name)
            {
                if (name == null) throw new FuException("[ObjectPoolModule] 对象名称不能为空.");

                if (!m_ObjectMultiDict.TryGetValue(name, out var objectRange)) return null;

                foreach (var obj in objectRange)
                {
                    // 如果允许获取正在使用的对象，则直接获取。
                    if (AllowSpawnInUse)
                        return obj.Spawn();

                    // 如果对象没有正在使用，则直接获取。
                    if (!obj.IsInUse)
                        return obj.Spawn();
                }

                return null;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="obj">要回收的对象。</param>
            public void Recycle(T obj)
            {
                if (obj == null) throw new FuException("[ObjectPoolModule] 对象不能为空.");
                Recycle(obj.Target);
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            /// <param name="target">要回收的对象。</param>
            public void Recycle(object target)
            {
                if (target == null) throw new FuException("[ObjectPoolModule] 要回收的目标对象不能为空.");

                var obj = _GetObject(target);
                if (obj == null)
                    throw new FuException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中找不到目标对象 '{target.GetType().FullName}'.");
                obj.Recycle();
                if (Count > m_Capacity && obj.SpawnCount <= 0)
                {
                    Release();
                }
            }

            /// <summary>
            /// 释放指定对象。
            /// </summary>
            /// <param name="obj">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(T obj)
            {
                if (obj == null) throw new FuException("[ObjectPoolModule] 目标对象不能为空.");
                return ReleaseObject(obj.Target);
            }

            /// <summary>
            /// 释放指定对象。
            /// </summary>
            /// <param name="target">要释放的对象。</param>
            /// <returns>释放对象是否成功。</returns>
            public bool ReleaseObject(object target)
            {
                if (target == null) throw new FuException("[ObjectPoolModule] 目标对象不能为空.");

                var obj = _GetObject(target);
                if (obj == null) return false;


                if (obj.IsInUse) return false;
                if (obj.Locked) return false;
                if (!obj.CustomCanReleaseFlag) return false;

                FuLogger.LogInfo($"[ObjectPoolModule] 真正释放对象池中的可释放对象 '{obj.Name}'");

                m_ObjectMultiDict.Remove(obj.Name, obj);
                m_TargetObjectDict.Remove(obj.TargetObject.Target);

                obj.OnRelease();
                ReferencePool.Runtime.ReferencePool.Release(obj);
                return true;
            }

            /// <summary>
            /// 释放对象池中的可释放对象(超过容量的数量为尝试释放的对象数量)
            /// </summary>
            public override void Release()
            {
                var overCapacity = Count - m_Capacity;
                Release(overCapacity, m_DefaultReleaseObjectFilterCallback);
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
                    throw new FuException("[ObjectPoolModule] 释放对象筛选函数不能为空.");

                if (toReleaseCount <= 0) return;

                // 找到对象过期时间点，最后使用时间早于这个时间点的对象就被认为是“过期”的。为空时表示不限制过期时间点
                DateTime? expireTimeThreshold = null;
                if (m_ExpireTime < float.MaxValue) // < float.MaxValue 意味着设置了过期时间
                {
                    // 过期时间点 = 当前UTC时间 - 过期时间秒数。例如，如果过期时间设置为10秒，那么过期时间点就是10秒前的时刻。任何超过10秒没被用过的对象都被视为过期。
                    expireTimeThreshold = DateTime.UtcNow.AddSeconds(-m_ExpireTime);
                }

                // 重置计时器
                m_AutoReleaseTimer = 0f;

                // 获取所有可释放的对象
                _GetCanReleaseObjects(m_CachedCanReleaseObjectList);
                FuLogger.LogInfo($"[ObjectPoolModule] 尝试释放对象池中的可释放对象-对象数量: '{m_CachedCanReleaseObjectList.Count}'");

                // 筛选需要释放的对象
                var toReleaseObjects = releaseObjectFilterCallback(m_CachedCanReleaseObjectList, toReleaseCount, expireTimeThreshold);
                if (toReleaseObjects is not { Count: > 0 }) return;

                // 释放对象
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
            /// 检查对象是否存在。
            /// </summary>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanSpawn() => CanSpawn(string.Empty);

            /// <summary>
            /// 检查对象是否可生成。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要检查的对象是否可生成。</returns>
            public bool CanSpawn(string name)
            {
                if (name == null) throw new FuException("[ObjectPoolModule] 对象名称不能为空.");

                if (!m_ObjectMultiDict.TryGetValue(name, out var objectRange)) return false;

                foreach (var obj in objectRange)
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
                if (obj == null) throw new FuException("[ObjectPoolModule] 对象不能为空.");
                SetLocked(obj.Target, locked);
            }

            /// <summary>
            /// 设置对象是否被加锁。
            /// </summary>
            /// <param name="target">要设置被加锁的对象。</param>
            /// <param name="locked">是否被加锁。</param>
            public void SetLocked(object target, bool locked)
            {
                if (target == null) throw new FuException("[ObjectPoolModule] 对象不能为空.");

                var obj = _GetObject(target);
                if (obj == null)
                    throw new FuException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”.");
                obj.Locked = locked;
            }

            /// <summary>
            /// 设置对象的优先级。
            /// </summary>
            /// <param name="obj">要设置优先级的对象。</param>
            /// <param name="priority">优先级。</param>
            public void SetPriority(T obj, int priority)
            {
                if (obj == null) throw new FuException("[ObjectPoolModule] 对象不能为空.");
                SetPriority(obj.Target, priority);
            }

            /// <summary>
            /// 设置对象的优先级。
            /// </summary>
            /// <param name="target">要设置优先级的对象。</param>
            /// <param name="priority">优先级。</param>
            public void SetPriority(object target, int priority)
            {
                if (target == null) throw new FuException("[ObjectPoolModule] 目标对象不能为空.");

                var obj = _GetObject(target);
                if (obj == null)
                    throw new FuException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”..");

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
                        results.Add(new ObjectInfo(obj.Name, obj.Locked, obj.CustomCanReleaseFlag,
                                                   obj.Priority, obj.LastUseTime, obj.SpawnCount));
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
                if (target == null) throw new FuException("[ObjectPoolModule] 目标对象不能为空.");
                return m_TargetObjectDict.GetValueOrDefault(target);
            }

            /// <summary>
            /// 获取对象池中能被释放的对象的数量
            /// </summary>
            /// <param name="results">结果列表</param>
            private void _GetCanReleaseObjects(List<T> results)
            {
                if (results == null) throw new FuException("[ObjectPoolModule] 结果列表不能为空.");

                results.Clear();
                foreach (var (_, obj) in m_TargetObjectDict)
                {
                    // 如果对象正在使用中，或者被加锁，或者自定义标记为不能被释放，则跳过。
                    if (obj.IsInUse || obj.Locked || !obj.CustomCanReleaseFlag)
                    {
                        continue;
                    }

                    results.Add(obj.TargetObject);
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
            /// <param name="expireTimeThreshold">对象过期时间点(为空时表示不限制过期时间点)。</param>
            /// <returns>经筛选需要释放的对象集合。</returns>
            private List<T> _DefaultReleaseObjectFilterCallback(List<T> candidateObjects, int toReleaseCount, DateTime? expireTimeThreshold)
            {
                m_CachedToReleaseObjectList.Clear();

                // 第一阶段：根据最后使用时间筛选过期对象。
                if (expireTimeThreshold.HasValue)
                {
                    for (var i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        // 如果对象最后使用时间 > 过期时间点，说明了对象还没过期，则继续筛选。
                        if (candidateObjects[i].LastUseTime > expireTimeThreshold.Value) continue;
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