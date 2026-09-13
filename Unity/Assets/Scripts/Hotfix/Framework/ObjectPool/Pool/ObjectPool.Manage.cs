using System;
using Hotfix.Framework.Core;
using System.Collections.Generic;
using AOT.Framework.Core.Log;

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

            // 目标真实对象（如 GameObject）已被 Unity 销毁时视为无效对象：不注册并剔除可能的残留登记，
            // 然后抛出异常让调用方感知——否则调用方 Acquire 出来的对象既不注册也不归还，会永久泄漏，
            // 且调用方误以为已登记。与上方 obj.Target == null 的抛异常风格一致。
            // 注意 Target 的静态类型为 object，obj.Target == null 只是引用比较，识别不了 Unity 的"假 null"。
            if (IsTargetDead(obj))
            {
                // 提前捕获名称：RemoveDeadObject 内的 OnDispose 会把对象状态清空（Name 置空）
                var deadObjName = obj.Name;
                RemoveDeadObject(obj);
                throw new InvalidOperationException($"[ObjectPoolModule] 对象池“{new TypeNamePair(typeof(T), Name)}”中要注册的对象 '{deadObjName}' 的目标真实对象已被 Unity 销毁（假 null），无法注册。");
            }

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
        /// 目标真实对象已被 Unity 销毁的无效对象会被跳过并自动剔除，继续查找下一个可用对象。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要获取的对象，池中无可用对象时返回null。</returns>
        public T Spawn(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new InvalidOperationException("[ObjectPoolModule] 对象名称不能为空.");

            if (!m_ObjectMultiDict.TryGetValue(name, out var objects)) return null;

            // 第一步：只读遍历，收集无效对象并选出可获取的候选对象。
            // 这一段里不调用任何用户代码：FuLinkedListRange 的枚举器是沿 LinkedListNode.Next 直走的裸指针遍历
            // （无版本校验，节点还会经 FuLinkedList 的缓存队列复用），一旦在枚举期间由用户回调（OnSpawn）
            // 重入本池改动链表，剩余结点就可能指向已被回收/复用的节点（值为 default 或他处对象）。
            // 因此把“收集死对象”“选中候选”与“调用用户代码”三件事彻底分开。
            List<T> deadObjects = null;
            T         candidate   = null;
            foreach (var obj in objects)
            {
                // 目标真实对象已被 Unity 销毁的对象视为无效：跳过，继续找下一个可用对象
                if (IsTargetDead(obj))
                {
                    deadObjects ??= new List<T>();
                    deadObjects.Add(obj);
                    continue;
                }

                // 如果允许获取正在使用的对象，或者对象没有正在使用，则直接获取。
                if (AllowSpawnInUse || !obj.IsInUse)
                {
                    candidate = obj;
                    break;
                }
            }

            // 第二步（在枚举之外）：剔除无效对象。剔除会触发用户代码（OnDispose）并可能重入本池，
            // 此时已无任何枚举器存活，不会走进已回收节点。放在 candidate.Spawn() 之前，
            // 使 Spawn 抛异常时本批死对象同样被剔除（与原先 finally 中的行为一致）。
            RemoveDeadObjects(deadObjects);

            // 第三步（在枚举之外）：真正生成。OnSpawn 用户代码同样可能重入本池改动链表，
            // 故必须在遍历结束后才调用，且返回值不依赖任何枚举状态。
            if (candidate == null) return null;

            candidate.Spawn();
            return candidate;
        }

        /// <summary>
        /// 剔除一批目标真实对象已被 Unity 销毁的无效对象（必须在任何链表枚举之外调用）。
        /// 逐项 try/catch 隔离（与 DisposeTodoObjects 一致）：RemoveDeadObject 内 OnDispose 抛异常时
        /// 不得逃逸覆盖调用方的原始异常，也不能因此跳过本批剩余死对象的剔除。
        /// </summary>
        /// <param name="deadObjects">待剔除的无效对象集合，可为 null。</param>
        private void RemoveDeadObjects(List<T> deadObjects)
        {
            if (deadObjects == null) return;

            for (var i = 0; i < deadObjects.Count; i++)
            {
                var deadObject = deadObjects[i];
                if (deadObject == null) continue;

                // 提前捕获名称：OnDispose 会清空对象状态（Name 置空），异常告警需要它
                var deadObjectName = deadObject.Name;
                try
                {
                    RemoveDeadObject(deadObject);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 剔除对象池“{new TypeNamePair(typeof(T), Name)}”中的无效对象 '{deadObjectName}' 时出现异常: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 判断对象的目标真实对象是否已被 Unity 销毁（Unity 的"假 null"）。
        /// Target 的静态类型为 object，直接 == null 只做引用比较，无法识别 Unity 重载的假 null。
        /// </summary>
        /// <param name="obj">要检查的对象。</param>
        /// <returns>目标真实对象是否已失效。</returns>
        private static bool IsTargetDead(T obj) => obj != null && obj.Target is UnityEngine.Object unityObject && !unityObject;

        /// <summary>
        /// 剔除目标真实对象已被 Unity 销毁的无效对象：先从两个字典中移除登记，
        /// 再走完整销毁流程（OnDispose 释放资源 + 回收到引用池）并告警。
        /// 仅移除登记而不销毁回收会导致被剔除对象（如 EntityObject）的资源句柄泄漏、
        /// 引用池计数（UsingReferenceCount）不归零。
        /// </summary>
        /// <param name="obj">要剔除的对象。</param>
        private void RemoveDeadObject(T obj)
        {
            // 提前捕获对象名：OnDispose 会清理对象状态（Name 置空），后续告警与移除登记都需要它
            var objName = obj.Name;
            FuLogger.LogWarning($"[ObjectPoolModule] 对象池“{new TypeNamePair(typeof(T), Name)}”中的对象 '{objName}' 的目标真实对象已被 Unity 销毁（假 null），视为无效对象，已从对象池中剔除。");

            if (!string.IsNullOrEmpty(objName))
                m_ObjectMultiDict.Remove(objName, obj);

            if (obj.Target != null)
                m_TargetObjectDict.Remove(obj.Target);

            // 已解除登记后必须补全销毁与回收：ObjectBase : IReference，正常都能回收。
            try
            {
                obj.OnDispose();
            }
            finally
            {
                // 回收失败的告警放在 try/catch 内，避免 OnDispose 异常之外再抛二次异常掩盖原始错误
                try
                {
                    ReferencePool.Recycle(obj);
                }
                catch (Exception e)
                {
                    FuLogger.LogWarning($"[ObjectPoolModule] 回收被剔除的无效对象 '{objName}' 时出现异常: {e.Message}");
                }
            }
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

            RecycleInternal(obj);
        }

        /// <summary>
        /// 尝试回收对象。找不到对应目标对象时返回 false 并告警，不抛异常，供高频调用方优雅处理。
        /// 仅"找不到目标"不抛异常；重复回收等非法状态仍会抛出异常以便暴露调用错误。
        /// </summary>
        /// <param name="obj">要回收的对象。</param>
        /// <returns>是否回收成功。</returns>
        public bool TryRecycle(T obj)
        {
            if (obj == null) return false;
            return TryRecycle(obj.Target);
        }

        /// <summary>
        /// 尝试回收对象。找不到对应目标对象时返回 false 并告警，不抛异常，供高频调用方优雅处理。
        /// 仅"找不到目标"不抛异常；重复回收等非法状态仍会抛出异常以便暴露调用错误。
        /// </summary>
        /// <param name="target">要回收的目标对象。</param>
        /// <returns>是否回收成功。</returns>
        public bool TryRecycle(object target)
        {
            if (target == null) return false;

            var obj = GetObject(target);
            if (obj == null)
            {
                FuLogger.LogWarning($"[ObjectPoolModule] 在对象池“{new TypeNamePair(typeof(T), Name)}”中找不到目标对象，回收失败（已忽略），目标类型为“{target.GetType().FullName}”。");
                return false;
            }

            RecycleInternal(obj);
            return true;
        }

        /// <summary>
        /// 执行对象回收，并在超出容量时触发裁剪。
        /// </summary>
        /// <param name="obj">要回收的池内对象。</param>
        private void RecycleInternal(T obj)
        {
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
                // 目标真实对象已被 Unity 销毁的无效对象不可获取，跳过（与 Spawn 的剔除逻辑保持一致）
                if (IsTargetDead(obj)) continue;

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