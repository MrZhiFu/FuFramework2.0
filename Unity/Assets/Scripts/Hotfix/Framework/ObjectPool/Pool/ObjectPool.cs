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
        /// 缓存默认销毁筛选函数返回的待销毁对象列表（仅由 DefaultDisposeObjectFilterCallback 写入）。
        /// 调用方读取后必须立即拷贝到自己的快照列表中再遍历，避免嵌套重入时被清空而破坏遍历。
        /// </summary>
        private readonly List<T> m_CachedToDisposeObjectList;

        /// <summary>
        /// 默认筛选函数做“小量 top-k 部分选择”时复用的最大堆（正常路径零分配，堆内存放候选集合的下标）。
        /// 仅由 SelectSmallestByDisposeOrder 使用：该流程内只做 CompareDisposeOrder 比较与堆调整，
        /// 不调用任何用户代码，故不存在嵌套重入覆写问题；但每次使用前后都会 Clear。
        /// </summary>
        private readonly List<int> m_CachedSelectHeapIndices;

        /// <summary>
        /// 本池专属的“本轮待销毁对象”快照字段（正常路径零分配）。
        /// 与筛选函数返回值字段 m_CachedToDisposeObjectList 分离：后者会被筛选函数反复 Clear/重填，
        /// 若共用会在嵌套重入时破坏正在遍历的列表。该字段仅在非重入时复用；发生嵌套重入
        /// （OnDispose 内再次进入销毁流程，如回收超容量）时由 BeginTodoSnapshot 改用局部列表，
        /// 避免覆写外层正在遍历的同一字段。
        /// </summary>
        private readonly List<T> m_CachedTodoSnapshot;

        /// <summary>
        /// 快照字段 m_CachedTodoSnapshot 是否正被销毁遍历占用（用于识别嵌套重入）。
        /// </summary>
        private bool m_TodoSnapshotInUse;

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
        private float m_ExpireTimeAfterIdle;


        /// <summary>
        /// 自动销毁计时器。用于计时，每隔 AutoDisposeCheckInterval 秒触发一次自动销毁检查。
        /// </summary>
        private float m_AutoDisposeTimer;

        /// <summary>
        /// 对象池自动销毁检查的间隔秒数。
        /// </summary>
        private float m_AutoDisposeCheckInterval;

        /// <summary>
        /// 获取或设置对象池每次轮询中自动销毁检查的间隔秒数。
        /// </summary>
        public override float AutoDisposeCheckInterval
        {
            get => m_AutoDisposeCheckInterval;
            set
            {
                // NaN/Infinity 必须显式拒绝：NaN 与任何数比较均为 false，能穿过 value < 0f 的守卫，
                // 落库后会让 Update 里的“m_AutoDisposeTimer >= AutoDisposeCheckInterval”恒为 false，
                // 自动销毁检查永不触发；正无穷虽等价于“永不检查”，但不是有明文语义的取值，一并拒绝。
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new InvalidOperationException("[ObjectPoolModule] 自动销毁检查间隔秒数不能为 NaN 或 Infinity.");
                if (value < 0f) throw new InvalidOperationException("[ObjectPoolModule] 自动销毁检查间隔秒数不能小于0.");
                if (Mathf.Approximately(m_AutoDisposeCheckInterval, value)) return;

                m_AutoDisposeCheckInterval = value;
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
        /// 只读统计，不复用 m_CachedCanDisposeObjectList 共享缓存（该缓存会被销毁流程反复 Clear/重填，
        /// 读属性直接改写会踩掉其他流程正在使用的数据），改为独立计数遍历。
        /// </summary>
        public override int CanDisposeCount
        {
            get
            {
                var count = 0;
                foreach (var (_, obj) in m_TargetObjectDict)
                {
                    if (IsCanDisposeObject(obj)) count++;
                }

                return count;
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
                // Capacity 是 int，不存在 NaN/Infinity，value < 0 已覆盖全部非法取值
                if (value      < 0) throw new InvalidOperationException("[ObjectPoolModule] 对象池容量不能小于0.");
                if (m_Capacity == value) return;

                m_Capacity = value;
                DisposeOverCapacity();
            }
        }

        /// <summary>
        /// 获取或设置对象池对象过期秒数。
        /// 对象闲置（距上次使用或回收）超过该秒数即视为过期，纳入销毁候选。
        /// </summary>
        public override float ExpireTimeAfterIdle
        {
            get => m_ExpireTimeAfterIdle;
            set
            {
                // NaN/Infinity 必须显式拒绝：NaN 与任何数比较均为 false，能穿过 value < 0f 的守卫，
                // 落库后会让过期判定“now - LastUseTime >= m_ExpireTimeAfterIdle”恒为 false，对象永不销毁；
                // 正无穷虽等价于“永不过期”，但不是有明文语义的取值（哨兵是 float.MaxValue），一并拒绝。
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new InvalidOperationException("[ObjectPoolModule] 对象过期秒数不能为 NaN 或 Infinity.");
                if (value < 0f) throw new InvalidOperationException("[ObjectPoolModule] 对象过期秒数不能小于0.");
                if (Mathf.Approximately(ExpireTimeAfterIdle, value)) return;

                m_ExpireTimeAfterIdle = value;
                DisposeExpired();
            }
        }

        /// <summary>
        /// 初始化对象池的新实例。
        /// </summary>
        /// <param name="name">对象池名称。</param>
        /// <param name="allowSpawnInUse">是否允许对象池中对象正在使用的状态下被获取。</param>
        /// <param name="autoDisposeCheckInterval">对象池自动销毁检查的间隔秒数。</param>
        /// <param name="capacity">对象池的容量。</param>
        /// <param name="expireTimeAfterIdle">对象闲置超过该秒数即视为过期。</param>
        /// <param name="priority">对象池的优先级。</param>
        internal ObjectPool(string name, bool allowSpawnInUse, float autoDisposeCheckInterval, int capacity, float expireTimeAfterIdle, int priority) : base(name)
        {
            m_ObjectMultiDict                    = new FuMultiDictionary<string, T>();
            m_TargetObjectDict                   = new Dictionary<object, T>();
            m_DefaultDisposeObjectFilterCallback = DefaultDisposeObjectFilterCallback;
            m_CachedCanDisposeObjectList         = new List<T>();
            m_CachedToDisposeObjectList          = new List<T>();
            m_CachedTodoSnapshot                 = new List<T>();
            m_CachedSelectHeapIndices            = new List<int>();

            AllowSpawnInUse     = allowSpawnInUse;
            AutoDisposeCheckInterval = autoDisposeCheckInterval;
            Capacity            = capacity;
            ExpireTimeAfterIdle          = expireTimeAfterIdle;
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
            if (AutoDisposeCheckInterval >= float.MaxValue) return;

            m_AutoDisposeTimer += unscaledDeltaTime;

            // 每隔 AutoDisposeCheckInterval 秒触发一次自动销毁检查
            if (m_AutoDisposeTimer >= AutoDisposeCheckInterval)
            {
                // 减去一个检查间隔而非直接清零：把余数留到下一次累加，消除检查间隔漂移。
                // 计时器只在这一次检查完成后推进，其他流程（Dispose/DisposeAllUnused）不得重置，
                // 否则持续回收会让自动销毁检查被反复推迟（饿死）。
                m_AutoDisposeTimer = Mathf.Max(0f, m_AutoDisposeTimer - AutoDisposeCheckInterval);

                if (Count > m_Capacity)
                {
                    // 超容量：默认筛选器一次选出"过期对象 + 超容量部分"销毁，无需再单独清过期（避免二次全量扫描）
                    DisposeOverCapacity();
                }
                else
                {
                    // 未超容量：只需清理过期对象
                    DisposeExpired();
                }
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

            // 先统计处于使用中的对象数量，再汇总成一条告警：
            // 正常退出（EntityModule/UIModule teardown 时实体/界面仍存活）会有大量在用对象，
            // 逐条告警会刷屏淹没真正的告警，这里只提示总量。
            var inUseCount = 0;
            foreach (var obj in objects)
            {
                if (obj.IsInUse) inUseCount++;
            }

            if (inUseCount > 0)
            {
                FuLogger.LogWarning($"[ObjectPoolModule] 对象池 {Name} 关闭时仍有 {inUseCount} 个对象处于使用中，将被强制回收（存在未归还对象，请检查回收时序）。");
            }

            foreach (var obj in objects)
            {
                // 重入保护：本方法可能被某个 obj.OnDispose() 内部触发的销毁重入，此时同批剩余对象已被内层
                // 循环处理过（OnDispose 会清空对象状态、Name 置空，与 DisposeObjectInternal 的跳过判断对齐）。
                // 若不跳过，它们会被二次 OnDispose + 二次 ReferencePool.Recycle（并被 catch 成误导性告警）。
                if (string.IsNullOrEmpty(obj.Name)) continue;

                // 先在两个字典中解除登记，再调用用户回调 OnDispose()（与 DisposeObjectInternal 的做法一致）：
                // OnDispose 是用户代码（EntityObject 会销毁 GameObject、WinObject 会触发回调），可能重入本池
                // （Spawn/Recycle/DisposeObject）。若此时对象仍登记在册，Spawn 的无效对象分支会经
                // RemoveDeadObject 对同一对象再次 OnDispose + Recycle，导致句柄双释放/二次回收。
                // 移除登记必须在 OnDispose 之前完成，使重入者看不到该对象。
                // 提前捕获名称：OnDispose 会清空对象状态（Name 置空），移除登记需要它。
                var objName = obj.Name;
                if (!string.IsNullOrEmpty(objName))
                    m_ObjectMultiDict.Remove(objName, obj);

                if (obj.Target != null)
                    m_TargetObjectDict.Remove(obj.Target);

                // 使用中的对象也会被强制回收（否则这些对象会残留在引用池外、无法清理）
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
                        ReferencePool.Recycle(obj);
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
            m_CachedSelectHeapIndices.Clear();

            // 刻意不清 m_CachedTodoSnapshot、不复位 m_TodoSnapshotInUse：
            // 本方法可能在 DisposeTodoObjects 批量销毁“途中”被重入（某个 obj.OnDispose() 内部触发 DisposeObjectPool）。
            // 若在此清空快照/复位占用标记，外层正在遍历的快照会被清空 → 本批剩余对象被静默跳过（句柄不释放、计数不归零）。
            // 快照的清空与占用标记复位统一由外层 BeginTodoSnapshot/EndTodoSnapshot 的 finally 负责；
            // 非重入场景下快照在每次 BeginTodoSnapshot 时也会先 Clear，故不清理不会残留脏数据。
        }
    }
}