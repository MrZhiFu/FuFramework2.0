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
            if (m_ExpireTime >= float.MaxValue) return;

            var expireTimeThreshold = DateTime.UtcNow.AddSeconds(-m_ExpireTime);
            GetCanDisposeObjects(m_CachedCanDisposeObjectList);
            foreach (var obj in m_CachedCanDisposeObjectList)
            {
                // 对象闲置时间早于过期时间点，视为过期，纳入销毁
                if (obj.LastUseTime <= expireTimeThreshold)
                {
                    // 防御性 null 检查
                    if (obj == null) continue;

                    // 提前捕获对象名，销毁后对象会被回收清理，Name 变为空
                    var objName = obj.Name;
                    try
                    {
                        DisposeObjectInternal(obj);
                    }
                    catch (Exception e)
                    {
                        FuLogger.LogWarning($"[ObjectPoolModule] 销毁过期对象 '{objName}' 时出现异常: {e.Message}");
                    }
                }
            }
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

            // 再次按照过滤器函数筛选需要销毁的对象
            var toDisposeObjects = releaseObjectFilterCallback(m_CachedCanDisposeObjectList, toDisposeCount, expireTimeThreshold);
            if (toDisposeObjects is not { Count: > 0 }) return;

            // 销毁对象（单个对象异常不影响整批销毁）
            foreach (var toDisposeObject in toDisposeObjects)
            {
                // 防御自定义筛选器返回 null 对象
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
        /// 销毁对象池中的所有未使用对象。
        /// </summary>
        public override void DisposeAllUnused()
        {
            m_AutoDisposeTimer = 0f;
            GetCanDisposeObjects(m_CachedCanDisposeObjectList);
            foreach (var toDisposeObject in m_CachedCanDisposeObjectList)
            {
                // 防御自定义筛选器返回 null 对象
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
            if (obj.IsInUse) return false;
            if (obj.Locked) return false;
            if (!obj.CustomCanDisposeFlag) return false;

            FuLogger.LogInfo($"[ObjectPoolModule] 真正销毁对象池中的可销毁对象 '{obj.Name}'");

            m_ObjectMultiDict.Remove(obj.Name, obj);
            m_TargetObjectDict.Remove(obj.Target);

            try
            {
                obj.OnDispose();
            }
            finally
            {
                // 即使 OnDispose 异常也回收对象到引用池，避免跳过清理
                GlobalModule.ReferencePoolModule.Recycle(obj);
            }

            return true;
        }

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
                if (obj.IsInUse || obj.Locked || !obj.CustomCanDisposeFlag)
                {
                    continue;
                }

                results.Add(obj);
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
                    // 对象最后使用时间比过期时间点晚（更近）= 还没闲置到 expireTime，未过期，跳过
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