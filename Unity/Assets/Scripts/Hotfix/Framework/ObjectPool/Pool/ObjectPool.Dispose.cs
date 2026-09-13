using System;
using System.Collections.Generic;
using Hotfix.Framework.Core;
using AOT.Framework.Core.Log;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池中对象的销毁与筛选。
    /// 功能：
    ///     1. 提供容量/过期驱动的销毁接口与全部闲置对象销毁。
    ///     2. 提供销毁对象的筛选策略与过期清理。
    /// </summary>
    public sealed partial class ObjectPool<T> : ObjectPoolBase where T : ObjectBase
    {
        /// <summary>
        /// 销毁对象池中已过期的可销毁对象（不限于超容量）。
        /// 与 DisposeOverCapacity() 配合，使自动销毁间隔同时覆盖"超容量裁剪"与"过期闲置清理"。
        /// </summary>
        private void DisposeExpired()
        {
            // 未设置过期时间时无需处理
            if (m_ExpireTimeAfterIdle >= float.MaxValue) return;

            // 用“已闲置时长”判定过期，而不是先算一个 DateTime 过期时间点：
            // DateTime.UtcNow.AddSeconds(-m_ExpireTimeAfterIdle) 在过期值“大而有限”时（上面的守卫只拦
            // >= float.MaxValue，拦不住它）会越过 DateTime.MinValue 而抛 ArgumentOutOfRangeException，
            // 等于策划写个近似永不过期的大数就运行时崩溃。直接比较时长对大过期值天然安全。
            var now = DateTime.UtcNow;

            GetCanDisposeObjects(m_CachedCanDisposeObjectList);

            // 用本池专属快照承载"本轮待销毁对象"：DisposeObjectInternal 内 OnDispose 可能重入本池的
            // Dispose/DisposeExpired（例如回收超容量），若直接遍历共享字段会被嵌套调用清空，
            // 导致本批剩余对象被静默跳过。快照字段正常路径零分配，嵌套重入时自动退化为局部列表。
            var toDisposeObjects = BeginTodoSnapshot(m_CachedCanDisposeObjectList.Count);
            try
            {
                for (var i = 0; i < m_CachedCanDisposeObjectList.Count; i++)
                {
                    var obj = m_CachedCanDisposeObjectList[i];

                    // 防御性 null 检查（在解引用前）
                    if (obj == null) continue;

                    // 已闲置时长达到过期秒数，视为过期，纳入销毁。
                    // 与筛选函数第一阶段（LastUseTime <= 过期时间点）数学等价，语义保持一致。
                    if ((now - obj.LastUseTime).TotalSeconds >= m_ExpireTimeAfterIdle)
                        toDisposeObjects.Add(obj);
                }

                DisposeTodoObjects(toDisposeObjects);
            }
            finally
            {
                EndTodoSnapshot(toDisposeObjects);
            }
        }

        /// <summary>
        /// 计算“过期时间点”阈值（当前时间回溯 ExpireTimeAfterIdle 秒）。
        /// 用 Ticks 的饱和减法实现，避免 DateTime.AddSeconds(-x)：当 x 大而有限时（调用方守卫只拦
        /// >= float.MaxValue 的“永不过期”值）回溯会越过 DateTime.MinValue 而抛 ArgumentOutOfRangeException。
        /// 回溯量超出可表示范围时饱和为 DateTime.MinValue——不存在早于该时刻的真实 LastUseTime，
        /// 因此对筛选函数第一阶段而言等价于“永不过期”（所有候选都判为未过期），不会抛异常。
        /// </summary>
        /// <param name="now">当前UTC时间。</param>
        /// <returns>过期时间点。</returns>
        private DateTime ComputeExpireTimeThreshold(DateTime now)
        {
            var backTicks = (double)m_ExpireTimeAfterIdle * TimeSpan.TicksPerSecond;
            if (backTicks >= now.Ticks - DateTime.MinValue.Ticks) return DateTime.MinValue;

            return new DateTime(now.Ticks - (long)backTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// 销毁对象池中超过容量的可销毁对象。
        /// </summary>
        public override void DisposeOverCapacity()
        {
            var overCapacity = Count - m_Capacity;
            Dispose(overCapacity, m_DefaultDisposeObjectFilterCallback);
        }

        /// <summary>
        /// 销毁对象池中超过容量的可销毁对象。
        /// </summary>
        /// <param name="releaseObjectFilterCallback">销毁对象筛选函数。</param>
        public void DisposeOverCapacity(DisposeObjectFilterCallback<T> releaseObjectFilterCallback)
        {
            var overCapacity = Count - m_Capacity;
            Dispose(overCapacity, releaseObjectFilterCallback);
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
            if (m_ExpireTimeAfterIdle < float.MaxValue) // < float.MaxValue 意味着设置了过期时间
            {
                // 过期时间点 = 当前UTC时间 - 过期时间秒数。例如，如果过期时间设置为10秒，那么过期时间点就是10秒前的时刻。任何超过10秒没被用过的对象都被视为过期。
                // 该阈值还要交给（可能是自定义的）筛选函数的第一个参数使用，故仍需算出具体的 DateTime；
                // 但必须用带饱和的减法而非 DateTime.AddSeconds：后者在“大而有限”的过期值下会越过
                // DateTime.MinValue 抛 ArgumentOutOfRangeException（见 ComputeExpireTimeThreshold）。
                expireTimeThreshold = ComputeExpireTimeThreshold(DateTime.UtcNow);
            }

            // 注意：这里不再重置 m_AutoDisposeTimer。持续回收会反复调用本方法，重置计时器会让
            // 自动销毁检查被无限推迟（饿死）；计时器只由 Update 的一次检查完成后推进。

            // 获取所有可销毁的对象
            GetCanDisposeObjects(m_CachedCanDisposeObjectList);
            FuLogger.LogInfo($"[ObjectPoolModule] 尝试销毁对象池中的可销毁对象-对象数量: '{m_CachedCanDisposeObjectList.Count}'");

            // 再次按照过滤器函数筛选需要销毁的对象
            var filteredObjects = releaseObjectFilterCallback(m_CachedCanDisposeObjectList, toDisposeCount, expireTimeThreshold);
            if (filteredObjects is not { Count: > 0 }) return;

            // 快照到本池专属字段：filteredObjects 通常是共享字段（DefaultDisposeObjectFilterCallback 的返回值），
            // 销毁过程中若重入本池 Dispose（回收超容量）会被嵌套 Clear，破坏正在遍历的列表
            var toDisposeObjects = BeginTodoSnapshot(filteredObjects.Count);
            try
            {
                for (var i = 0; i < filteredObjects.Count; i++)
                {
                    // 防御自定义筛选器返回 null 对象
                    if (filteredObjects[i] == null) continue;

                    toDisposeObjects.Add(filteredObjects[i]);
                }

                DisposeTodoObjects(toDisposeObjects);
            }
            finally
            {
                EndTodoSnapshot(toDisposeObjects);
            }
        }

        /// <summary>
        /// 销毁对象池中的所有未使用对象。
        /// </summary>
        public override void DisposeAllUnused()
        {
            GetCanDisposeObjects(m_CachedCanDisposeObjectList);

            // 快照到本池专属字段，避免 DisposeObjectInternal 内 OnDispose 重入清空正在遍历的共享列表
            var toDisposeObjects = BeginTodoSnapshot(m_CachedCanDisposeObjectList.Count);
            try
            {
                for (var i = 0; i < m_CachedCanDisposeObjectList.Count; i++)
                {
                    // 防御性 null 检查
                    if (m_CachedCanDisposeObjectList[i] == null) continue;

                    toDisposeObjects.Add(m_CachedCanDisposeObjectList[i]);
                }

                DisposeTodoObjects(toDisposeObjects);
            }
            finally
            {
                EndTodoSnapshot(toDisposeObjects);
            }
        }

        /// <summary>
        /// 获取承载“本轮待销毁对象”的快照列表。
        /// 非重入时返回本池专属字段 m_CachedTodoSnapshot（零分配）；重入（OnDispose 内再次进入销毁流程）
        /// 时返回新分配的局部列表，避免覆写外层正在遍历的同一字段。
        /// 必须与 EndTodoSnapshot 成对使用（放在 try/finally 中）。
        /// </summary>
        /// <param name="capacity">快照预期容量（仅用于重入分支局部列表的初始容量）。</param>
        /// <returns>已清空的快照列表。</returns>
        private List<T> BeginTodoSnapshot(int capacity)
        {
            // 已有销毁遍历占用快照字段（嵌套重入），改用局部列表避免互相覆写
            if (m_TodoSnapshotInUse) return new List<T>(capacity);

            m_TodoSnapshotInUse = true;
            m_CachedTodoSnapshot.Clear();
            return m_CachedTodoSnapshot;
        }

        /// <summary>
        /// 结束快照使用。仅当快照是本池专属字段时才清空并释放占用标记（重入分支的局部列表无需处理）。
        /// </summary>
        /// <param name="snapshot">BeginTodoSnapshot 返回的列表。</param>
        private void EndTodoSnapshot(List<T> snapshot)
        {
            if (!ReferenceEquals(snapshot, m_CachedTodoSnapshot)) return;

            m_CachedTodoSnapshot.Clear();
            m_TodoSnapshotInUse = false;
        }

        /// <summary>
        /// 批量销毁快照中的对象。单个对象异常不影响整批销毁。
        /// 按索引遍历而非 foreach：即使嵌套流程改动了列表也不会抛“集合已被修改”异常。
        /// </summary>
        /// <param name="toDisposeObjects">本轮待销毁对象快照。</param>
        private void DisposeTodoObjects(List<T> toDisposeObjects)
        {
            for (var i = 0; i < toDisposeObjects.Count; i++)
            {
                var toDisposeObject = toDisposeObjects[i];
                if (toDisposeObject == null) continue;

                // 提前捕获对象名，销毁后对象会被回收清理，Name 变为空
                var toDisposeObjectName = toDisposeObject.Name;
                try
                {
                    DisposeObjectInternal(toDisposeObject);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 销毁对象 '{toDisposeObjectName}' 时出现异常: {e.Message}");
                }
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

            return DisposeObjectInternal(obj);
        }

        /// <summary>
        /// 销毁池内已知可销毁的对象（内部批量销毁使用，跳过字典查找与归属校验）。
        /// </summary>
        /// <param name="obj">要销毁的对象。</param>
        /// <returns>销毁对象是否成功。</returns>
        private bool DisposeObjectInternal(T obj)
        {
            // 名称已为空说明对象已被销毁过（ObjectBase.Clear() 会把 Name 置空）：直接返回。
            // 公开 API Dispose(int, DisposeObjectFilterCallback<T>) 允许自定义筛选函数返回重复项，
            // 池销毁重入也可能让同一对象在同一批里出现两次，第二次在此处
            // m_ObjectMultiDict.Remove(null, obj) 会抛 ArgumentNullException（被上层 catch 吞成误导告警）。
            // 守卫口径与 RemoveDeadObject 一致。
            if (string.IsNullOrEmpty(obj.Name)) return false;

            if (obj.IsInUse) return false;
            if (obj.Locked) return false;
            if (!obj.CustomCanDisposeFlag) return false;

            // 提前捕获名称：OnDispose 会清空对象状态（Name 置空），后续告警需要它
            var objName = obj.Name;
            FuLogger.LogInfo($"[ObjectPoolModule] 真正销毁对象池中的可销毁对象 '{objName}'");

            m_ObjectMultiDict.Remove(objName, obj);
            m_TargetObjectDict.Remove(obj.Target);

            try
            {
                obj.OnDispose();
            }
            finally
            {
                // 即使 OnDispose 异常也回收对象到引用池，避免跳过清理。
                // 回收自身必须兜底（与 ObjectPool.OnDispose、RemoveDeadObject 对齐）：重复回收时
                // 引用池会抛异常，不拦截会直接从公开的 DisposeObject(target) 抛给调用方。
                try
                {
                    ReferencePool.Recycle(obj);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 回收已销毁的对象 '{objName}' 时出现异常: {e.Message}");
                }
            }

            return true;
        }

        /// <summary>
        /// 判断对象是否属于可销毁候选（未使用中、未加锁、自定义标记允许销毁）。
        /// </summary>
        /// <param name="obj">要判断的对象。</param>
        /// <returns>是否可销毁。</returns>
        private static bool IsCanDisposeObject(T obj) => obj != null && !obj.IsInUse && !obj.Locked && obj.CustomCanDisposeFlag;

        /// <summary>
        /// 获取对象池中能被销毁的对象（填充到结果列表）。
        /// </summary>
        /// <param name="results">结果列表</param>
        private void GetCanDisposeObjects(List<T> results)
        {
            if (results == null) throw new InvalidOperationException("[ObjectPoolModule] 结果列表不能为空.");

            results.Clear();
            foreach (var (_, obj) in m_TargetObjectDict)
            {
                // 如果对象正在使用中，或者被加锁，或者自定义标记为不能被销毁，则跳过。
                if (!IsCanDisposeObject(obj)) continue;

                results.Add(obj);
            }
        }

        /// <summary>
        /// 销毁对象筛选函数。
        /// 筛选条件：
        /// 1.过期的对象先销毁。
        /// 2.优先级小的先销毁。或者优先级相等，但是最后使用时间更早的对象先销毁。
        /// 注意：返回值是共享字段 m_CachedToDisposeObjectList（每次调用会先 Clear），
        /// 调用方必须在返回后立即拷贝快照再遍历，否则嵌套重入会破坏正在遍历的列表。
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
                    // 对象最后使用时间比过期时间点晚（更近）= 还没闲置到 expireTimeAfterIdle，未过期，跳过
                    if (candidateObjects[i].LastUseTime > expireTimeThreshold.Value) continue;
                    m_CachedToDisposeObjectList.Add(candidateObjects[i]);
                    candidateObjects.RemoveAt(i);
                }

                toDisposeCount -= m_CachedToDisposeObjectList.Count;
            }

            // 第二阶段：按（优先级升序，最后使用时间升序）排序，取前 toDisposeCount 个。
            // 仅当需要销毁的数量少于候选总数时才排序：toDisposeCount >= 候选数意味着“全取”，
            // 结果集与顺序无关，跳过可省掉大池的一次 O(n log n) 全量排序（并顺带不再打乱调用方列表）。
            // 注意：toDisposeCount 很小而候选很多（如超容量 1 个）时仍会全量排序；
            // 若该路径成为热点，可再改为单遍部分选择（top-k，O(n·k)）——本次为保证语义不变未改。
            if (toDisposeCount < candidateObjects.Count)
            {
                candidateObjects.Sort((a, b) =>
                {
                    var priorityCmp = a.Priority.CompareTo(b.Priority);
                    return priorityCmp != 0 ? priorityCmp : a.LastUseTime.CompareTo(b.LastUseTime);
                });
            }

            for (var i = 0; i < toDisposeCount && i < candidateObjects.Count; i++)
            {
                m_CachedToDisposeObjectList.Add(candidateObjects[i]);
            }

            return m_CachedToDisposeObjectList;
        }
    }
}