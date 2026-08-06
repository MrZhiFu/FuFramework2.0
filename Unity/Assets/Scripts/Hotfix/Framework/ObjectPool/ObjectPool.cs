using System;
using UnityEngine;
using Hotfix.Framework.Core;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 具体管理对象的对象池。
    /// 功能：
    ///     1. 允许/禁止自动销毁：可以设置池中空闲对象是否在一定时间后自动销毁，以节省内存。
    ///     2. 设置优先级：可以设置对象池的优先级，在需要强制销毁对象时（如内存不足），优先销毁低优先级池中的对象。
    /// </summary>
    /// <typeparam name="T">对象池中的对象类型。</typeparam>
    public sealed partial class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
    {
        /// <summary>
        /// 存储对象的多值字典，key为对象名称，value为对象(可为多个)。
        /// 允许同一个对象名称对应多个对象实例。这对于需要管理具有相同名称的多个对象（如子弹、特效等）非常重要，能够支持高效的对象复用。
        /// </summary>
        private readonly FuMultiDictionary<string, T> m_ObjectMultiDict;

        /// <summary>
        /// 存储目标对象与其对应的内部对象的字典，key为目标对象，value为对应的内部对象。
        /// </summary>
        private readonly Dictionary<object, T> m_TargetObjectDict;

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
        /// 对象池自动销毁可销毁对象的间隔秒数。
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
        internal ObjectPool(string name, bool allowSpawnInUse, float autoDisposeInterval, int capacity, float expireTime, int priority) : base(name)
        {
            m_ObjectMultiDict                    = new FuMultiDictionary<string, T>();
            m_TargetObjectDict                   = new Dictionary<object, T>();
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
        /// 注册一个对象到对象池中。
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <param name="spawned">对象是否提前生成。</param>
        public void Register(T obj, bool spawned)
        {
            if (obj        == null) throw new InvalidOperationException("[ObjectPoolModule] 要创建并注册对象不能为空.");
            if (obj.Target == null) throw new InvalidOperationException("[ObjectPoolModule] 要注册的对象目标不能为空.");

            // 同一目标对象不允许重复注册，避免双字典写入不一致
            if (m_TargetObjectDict.ContainsKey(obj.Target))
                throw new InvalidOperationException($"[ObjectPoolModule] 对象池 '{new TypeNamePair(typeof(T), Name)}' 中已存在目标对象.");

            m_ObjectMultiDict.Add(obj.Name, obj);
            m_TargetObjectDict.Add(obj.Target, obj);

            // 对象是否提前生成，若是则直接走一次生成流程（计数+1、刷新最后使用时间、触发 OnSpawn）
            if (spawned) obj.Spawn();

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
                Dispose();
            }
        }

        /// <summary>
        /// 检查对象是否可获取。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要检查的对象是否可获取。</returns>
        public bool CanGet(string name)
        {
            if (name == null) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

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
                    results.Add(new ObjectInfo(obj.Name, obj.Locked, obj.CustomCanDisposeFlag, obj.Priority, obj.LastUseTime, obj.SpawnCount));
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
