using System;
using UnityEngine;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 具体管理对象的对象池。
        /// 功能：
        ///     1. 允许/禁止自动销毁：可以设置池中空闲对象是否在一定时间后自动销毁，以节省内存。
        ///     2. 设置优先级：可以设置对象池的优先级，在需要强制销毁对象时（如内存不足），优先销毁低优先级池中的对象。
        /// </summary>
        /// <typeparam name="T">对象池中的对象类型。</typeparam>
        public sealed class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
        {
            /// <summary>
            /// 存储对象的多值字典，key为对象名称，value为对象(可为多个)。
            /// 允许同一个对象名称对应多个对象实例。这对于需要管理具有相同名称的多个对象（如子弹、特效等）非常重要，能够支持高效的对象复用。
            /// </summary>
            private readonly FuMultiDictionary<string, Object<T>> m_ObjectMultiDict;

            /// <summary>
            /// 存储目标对象与其对应的内部对象的字典，key为目标对象，value为对应的内部对象。
            /// </summary>
            private readonly Dictionary<object, Object<T>> m_TargetObjectDict;

            /// <summary>
            /// 缓存当前所有可以销毁的对象列表(未使用的、未加锁的、自定义标记为可销毁的)。
            /// </summary>
            private readonly List<T> m_CachedCanDisposeObjectList;

            /// <summary>
            /// 缓存经过筛选函数后最终决定要销毁的对象列表。
            /// </summary>
            private readonly List<T> m_CachedToDisposeObjectList;

            /// <summary>
            /// 默认销毁对象的筛选函数(销毁策略)。定义了如何从候选列表中选出要销毁的对象（基于优先级和最后使用时间）。
            /// </summary>
            private readonly DisposeObjectFilterCallback<T> m_DefaultDisposeObjectFilterCallback;


            /// <summary>
            /// 对象池的容量。
            /// </summary>
            private int m_Capacity;

            /// <summary>
            /// 对象过期时间秒数。一个对象闲置超过这个时间（秒），就会被标记为可销毁。
            /// </summary>
            private float m_ExpireTime;


            /// <summary>
            /// 自动销毁计时器。用于计时，每隔 AutoDisposeInterval 秒触发一次自动销毁检查。
            /// </summary>
            private float m_AutoDisposeTimer;

            /// <summary>
            /// 获取或设置对象池每次轮询中自动销毁可销毁对象的间隔秒数。
            /// </summary>
            private float m_AutoDisposeInterval;

            /// <summary>
            /// 获取或设置对象池每次轮询中自动销毁可销毁对象的间隔秒数。
            /// </summary>
            public override float AutoDisposeInterval
            {
                get => m_AutoDisposeInterval;
                set
                {
                    if (value < 0f) throw new InvalidOperationException("[ObjectPoolModule] 自动销毁间隔秒数不能小于0.");
                    if (Mathf.Approximately(m_AutoDisposeInterval, value)) return;

                    m_AutoDisposeInterval = value;
                }
            }

            /// <summary>
            /// 获取或设置对象池的优先级。该优先级会影响该池子在对象池管理模块中卸载的顺序。
            /// </summary>
            public override int Priority { get; set; }

            /// <summary>
            /// 获取对象池中的对象时，是否允许获取正在被使用的对象。一般都为false。
            /// false--对象只能在回收后才能再次被获取，即池中会存在多个同名对象;
            /// true --对象能在未回收的状态下就能再次被获取，这样会使得池中的对象只有一个，每次获取之后这个对象的引用计数++
            /// </summary>
            public override bool AllowSpawnInUse { get; }

            /// <summary>
            /// 获取对象池对象类型。
            /// </summary>
            public override Type ObjectType => typeof(T);

            /// <summary>
            /// 获取对象池中对象的数量。
            /// </summary>
            public override int Count => m_TargetObjectDict.Count;

            /// <summary>
            /// 获取对象池中能被销毁的对象的数量。
            /// </summary>
            public override int CanDisposeCount
            {
                get
                {
                    GetCanDisposeObjects(m_CachedCanDisposeObjectList);
                    return m_CachedCanDisposeObjectList.Count;
                }
            }

            /// <summary>
            /// 获取或设置对象池的容量。
            /// </summary>
            public override int Capacity
            {
                get => m_Capacity;
                set
                {
                    if (value      < 0) throw new InvalidOperationException("[ObjectPoolModule] 对象池容量不能小于0.");
                    if (m_Capacity == value) return;

                    m_Capacity = value;
                    Dispose();
                }
            }

            /// <summary>
            /// 获取或设置对象池对象过期秒数。
            /// </summary>
            public override float ExpireTime
            {
                get => m_ExpireTime;
                set
                {
                    if (value < 0f) throw new InvalidOperationException("[ObjectPoolModule] 对象过期秒数不能小于0.");
                    if (Mathf.Approximately(ExpireTime, value)) return;

                    m_ExpireTime = value;
                    Dispose();
                }
            }

            /// <summary>
            /// 初始化对象池的新实例。
            /// </summary>
            /// <param name="name">对象池名称。</param>
            /// <param name="allowSpawnInUse">是否允许对象池中对象正在使用的状态下被获取。</param>
            /// <param name="autoDisposeInterval">对象池自动销毁可销毁对象的间隔秒数。</param>
            /// <param name="capacity">对象池的容量。</param>
            /// <param name="expireTime">对象池对象过期秒数。</param>
            /// <param name="priority">对象池的优先级。</param>
            public ObjectPool(string name, bool allowSpawnInUse, float autoDisposeInterval, int capacity, float expireTime, int priority) : base(name)
            {
                m_ObjectMultiDict                    = new FuMultiDictionary<string, Object<T>>();
                m_TargetObjectDict                   = new Dictionary<object, Object<T>>();
                m_DefaultDisposeObjectFilterCallback = DefaultDisposeObjectFilterCallback;
                m_CachedCanDisposeObjectList         = new List<T>();
                m_CachedToDisposeObjectList          = new List<T>();

                AllowSpawnInUse     = allowSpawnInUse;
                AutoDisposeInterval = autoDisposeInterval;
                Capacity            = capacity;
                ExpireTime          = expireTime;
                Priority            = priority;
                m_AutoDisposeTimer  = 0f;
            }

            /// <summary>
            /// 对象池轮询。
            /// </summary>
            /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
            internal override void Update(float unscaledDeltaTime)
            {
                // 默认不自动销毁时短路，避免无谓的每帧累加
                if (AutoDisposeInterval >= float.MaxValue) return;

                m_AutoDisposeTimer += unscaledDeltaTime;

                // 每隔 AutoDisposeInterval 秒触发一次自动销毁检查
                if (m_AutoDisposeTimer >= AutoDisposeInterval)
                {
                    Dispose();
                }
            }

            /// <summary>
            /// 关闭并清理对象池。
            /// </summary>
            internal override void OnDispose()
            {
                foreach (var (_, obj) in m_TargetObjectDict)
                {
                    try
                    {
                        obj.OnDispose();
                    }
                    catch (Exception e)
                    {
                        FuLogger.LogWarning($"[ObjectPoolModule] 销毁对象池 {Name} 中的对象时出现异常: {e.Message}");
                    }

                    GlobalModule.ReferencePoolModule.Recycle(obj);
                }

                m_ObjectMultiDict.Clear();
                m_TargetObjectDict.Clear();
                m_CachedCanDisposeObjectList.Clear();
                m_CachedToDisposeObjectList.Clear();
            }

            /// <summary>
            /// 注册一个对象到对象池中。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否提前生成。</param>
            public void Register(T obj, bool spawned)
            {
                if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 要创建并注册对象不能为空.");

                var tempObj = Object<T>.Create(obj, spawned);
                m_ObjectMultiDict.Add(obj.Name, tempObj);
                m_TargetObjectDict.Add(obj.Target, tempObj);

                if (Count > m_Capacity)
                    Dispose();
            }

            /// <summary>
            /// 获取已存在的对象。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要获取的对象。</returns>
            public T Get(string name)
            {
                if (name == null) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

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
                    Dispose();
                }
            }

            /// <summary>
            /// 销毁指定对象。
            /// </summary>
            /// <param name="obj">要销毁的对象。</param>
            /// <returns>销毁对象是否成功。</returns>
            public bool DisposeObject(T obj)
            {
                if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");
                return DisposeObject(obj.Target);
            }

            /// <summary>
            /// 销毁指定对象。
            /// </summary>
            /// <param name="target">要销毁的对象。</param>
            /// <returns>销毁对象是否成功。</returns>
            public bool DisposeObject(object target)
            {
                if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");

                var obj = GetObject(target);
                if (obj == null) return false;


                if (obj.IsInUse) return false;
                if (obj.Locked) return false;
                if (!obj.CustomCanDisposeFlag) return false;

                FuLogger.LogInfo($"[ObjectPoolModule] 真正销毁对象池中的可销毁对象 '{obj.Name}'");

                m_ObjectMultiDict.Remove(obj.Name, obj);
                m_TargetObjectDict.Remove(obj.TargetObject.Target);

                obj.OnDispose();
                GlobalModule.ReferencePoolModule.Recycle(obj);
                return true;
            }

            /// <summary>
            /// 销毁对象池中的可销毁对象(超过容量的数量为尝试销毁的对象数量)
            /// </summary>
            public override void Dispose()
            {
                var overCapacity = Count - m_Capacity;
                Dispose(overCapacity, m_DefaultDisposeObjectFilterCallback);
            }

            /// <summary>
            /// 销毁对象池中的可销毁对象。
            /// </summary>
            /// <param name="releaseObjectFilterCallback">销毁对象筛选函数。</param>
            public void Dispose(DisposeObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                Dispose(Count - m_Capacity, releaseObjectFilterCallback);
            }

            /// <summary>
            /// 尝试销毁对象池中的可销毁对象。
            /// </summary>
            /// <param name="toDisposeCount">尝试销毁对象数量。</param>
            /// <param name="releaseObjectFilterCallback">销毁对象筛选函数。</param>
            public void Dispose(int toDisposeCount, DisposeObjectFilterCallback<T> releaseObjectFilterCallback)
            {
                if (releaseObjectFilterCallback == null)
                    throw new InvalidOperationException("[ObjectPoolModule] 销毁对象筛选函数不能为空.");

                if (toDisposeCount <= 0) return;

                // 找到对象过期时间点，最后使用时间早于这个时间点的对象就被认为是“过期”的。为空时表示不限制过期时间点
                DateTime? expireTimeThreshold = null;
                if (m_ExpireTime < float.MaxValue) // < float.MaxValue 意味着设置了过期时间
                {
                    // 过期时间点 = 当前UTC时间 - 过期时间秒数。例如，如果过期时间设置为10秒，那么过期时间点就是10秒前的时刻。任何超过10秒没被用过的对象都被视为过期。
                    expireTimeThreshold = DateTime.UtcNow.AddSeconds(-m_ExpireTime);
                }

                // 重置计时器
                m_AutoDisposeTimer = 0f;

                // 获取所有可销毁的对象
                GetCanDisposeObjects(m_CachedCanDisposeObjectList);
                FuLogger.LogInfo($"[ObjectPoolModule] 尝试销毁对象池中的可销毁对象-对象数量: '{m_CachedCanDisposeObjectList.Count}'");

                // 筛选需要销毁的对象
                var toDisposeObjects = releaseObjectFilterCallback(m_CachedCanDisposeObjectList, toDisposeCount, expireTimeThreshold);
                if (toDisposeObjects is not { Count: > 0 }) return;

                // 销毁对象
                foreach (var toDisposeObject in toDisposeObjects)
                {
                    DisposeObject(toDisposeObject);
                }
            }

            /// <summary>
            /// 销毁对象池中的所有未使用对象。
            /// </summary>
            public override void DisposeAllUnused()
            {
                m_AutoDisposeTimer = 0f;
                GetCanDisposeObjects(m_CachedCanDisposeObjectList);
                foreach (var toDisposeObject in m_CachedCanDisposeObjectList)
                {
                    DisposeObject(toDisposeObject);
                }
            }

            /// <summary>
            /// 检查对象是否存在。
            /// </summary>
            /// <returns>要检查的对象是否存在。</returns>
            public bool CanGet() => CanGet(string.Empty);

            /// <summary>
            /// 检查对象是否可获取。
            /// </summary>
            /// <param name="name">对象名称。</param>
            /// <returns>要检查的对象是否可获取。</returns>
            public bool CanGet(string name)
            {
                if (name == null) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

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
                    throw new InvalidOperationException($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中未找到目标，目标类型为“{target.GetType().FullName}”，目标值为“{target}”..");

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
                        results.Add(new ObjectInfo(obj.Name, obj.Locked, obj.CustomCanDisposeFlag,
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
            private Object<T> GetObject(object target)
            {
                if (target == null) throw new InvalidOperationException("[ObjectPoolModule] 目标对象不能为空.");
                return m_TargetObjectDict.GetValueOrDefault(target);
            }

            /// <summary>
            /// 获取对象池中能被销毁的对象的数量
            /// </summary>
            /// <param name="results">结果列表</param>
            private void GetCanDisposeObjects(List<T> results)
            {
                if (results == null) throw new InvalidOperationException("[ObjectPoolModule] 结果列表不能为空.");

                results.Clear();
                foreach (var (_, obj) in m_TargetObjectDict)
                {
                    // 如果对象正在使用中，或者被加锁，或者自定义标记为不能被销毁，则跳过。
                    if (obj.IsInUse || obj.Locked || !obj.CustomCanDisposeFlag)
                    {
                        continue;
                    }

                    results.Add(obj.TargetObject);
                }
            }

            /// <summary>
            /// 销毁对象筛选函数。
            /// 筛选条件：
            /// 1.过期的对象先销毁。
            /// 2.优先级小的先销毁。或者优先级相等，但是最后使用时间更早的对象先销毁。
            /// </summary>
            /// <typeparam name="T">对象类型。</typeparam>
            /// <param name="candidateObjects">要筛选的对象集合。</param>
            /// <param name="toDisposeCount">需要销毁的对象数量。</param>
            /// <param name="expireTimeThreshold">对象过期时间点(为空时表示不限制过期时间点)。</param>
            /// <returns>经筛选需要销毁的对象集合。</returns>
            private List<T> DefaultDisposeObjectFilterCallback(List<T> candidateObjects, int toDisposeCount, DateTime? expireTimeThreshold)
            {
                m_CachedToDisposeObjectList.Clear();

                // 第一阶段：根据最后使用时间筛选过期对象。
                if (expireTimeThreshold.HasValue)
                {
                    for (var i = candidateObjects.Count - 1; i >= 0; i--)
                    {
                        // 如果对象最后使用时间 > 过期时间点，说明了对象还没过期，则继续筛选。
                        if (candidateObjects[i].LastUseTime > expireTimeThreshold.Value) continue;
                        m_CachedToDisposeObjectList.Add(candidateObjects[i]);
                        candidateObjects.RemoveAt(i);
                    }

                    toDisposeCount -= m_CachedToDisposeObjectList.Count;
                }

                // 第二阶段：按（优先级升序，最后使用时间升序）排序，取前 toDisposeCount 个
                candidateObjects.Sort((a, b) =>
                {
                    var priorityCmp = a.Priority.CompareTo(b.Priority);
                    return priorityCmp != 0 ? priorityCmp : a.LastUseTime.CompareTo(b.LastUseTime);
                });
                for (var i = 0; i < toDisposeCount && i < candidateObjects.Count; i++)
                {
                    m_CachedToDisposeObjectList.Add(candidateObjects[i]);
                }

                return m_CachedToDisposeObjectList;
            }
        }
    }
}
