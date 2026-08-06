using System;
using UnityEngine;
using Hotfix.Framework.Core;
using System.Collections.Generic;
using AOT.Framework.Core.Log;

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
                DisposeOverCapacity();
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
                DisposeOverCapacity();
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
                m_AutoDisposeTimer = 0f;

                // 清理超过容量的对象
                DisposeOverCapacity();

                // 清理过期对象
                DisposeExpired();
            }
        }

        /// <summary>
        /// 关闭并清理对象池。
        /// </summary>
        internal override void OnDispose()
        {
            // 复制到临时列表，避免对象 OnDispose 中修改池字典导致遍历异常
            var objects = new List<T>(m_TargetObjectDict.Count);
            foreach (var (_, obj) in m_TargetObjectDict)
            {
                objects.Add(obj);
            }

            foreach (var obj in objects)
            {
                try
                {
                    obj.OnDispose();
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 销毁对象池 {Name} 中的对象时出现异常: {e.Message}");
                }
                finally
                {
                    try
                    {
                        // 单次回收：即使 OnDispose 异常也回收对象到引用池
                        GlobalModule.ReferencePoolModule.Recycle(obj);
                    }
                    catch (Exception e)
                    {
                        FuLogger.LogWarning($"[ObjectPoolModule] 回收对象池 {Name} 中的对象时出现异常: {e.Message}");
                    }
                }
            }

            m_ObjectMultiDict.Clear();
            m_TargetObjectDict.Clear();
            m_CachedCanDisposeObjectList.Clear();
            m_CachedToDisposeObjectList.Clear();
        }
    }
}