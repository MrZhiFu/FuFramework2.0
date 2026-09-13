using System;
using UnityEngine;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池内的对象基类。实现了引用对象的接口。
    /// 功能：
    ///     1. 记录了对象的基本信息，如对象名称、目标真实对象、是否被加锁、优先级、上次使用时间，自定义销毁检查标记等属性。
    ///     2. 定义了对象生成时、回收时、销毁时等生命周期事件。
    /// </summary>
    public abstract class ObjectBase : IReference
    {
        /// <summary>
        /// 对象名称。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 对象的目标真实对象。如GameObject。
        /// </summary>
        public object Target { get; private set; }

        /// <summary>
        /// 对象是否被加锁。
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// 对象的优先级。
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 对象上次使用时间，单位秒。取自<b>单调时钟</b> Time.unscaledTimeAsDouble：
        /// 单调递增、不受系统时间调整（NTP 校时/用户改表）影响，也不受 Time.timeScale 影响。
        /// 仅用于池内“闲置时长”比较（now - LastUseTime），不是墙钟时刻、跨会话无意义、不做存档。
        /// 用 double 而非 float：长会话下 float 的秒级精度会退化到十几毫秒以上，闲置时长判定会失真。
        /// </summary>
        public double LastUseTime { get; private set; }

        /// <summary>
        /// 获取对象池中对象的获取计数（引用计数）。
        /// </summary>
        public int SpawnCount { get; private set; }

        /// <summary>
        /// 获取对象是否正在使用中。
        /// </summary>
        public bool IsInUse => SpawnCount > 0;

        /// <summary>
        /// 自定义是否可销毁标记。默认为true。
        /// </summary>
        public virtual bool CustomCanDisposeFlag => true;

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象的目标真实对象。如GameObject。</param>
        protected void Initialize(string name, object target) => _Initialize(name, target, false, 0);

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象的目标真实对象。如GameObject。</param>
        /// <param name="locked">对象是否被加锁。</param>
        protected void Initialize(string name, object target, bool locked) => _Initialize(name, target, locked, 0);

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象的目标真实对象。如GameObject。</param>
        /// <param name="priority">对象的优先级。</param>
        protected void Initialize(string name, object target, int priority) => _Initialize(name, target, false, priority);

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象的目标真实对象。如GameObject。</param>
        /// <param name="locked">对象是否被加锁。</param>
        /// <param name="priority">对象的优先级。</param>
        private void _Initialize(string name, object target, bool locked, int priority)
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("[ObjectBase] 对象名称不能为空，对象必须命名后才能注册到对象池.");

            Name        = name;
            Target      = target ?? throw new InvalidOperationException($"[ObjectBase] 对象“{name}”为空.");
            Locked      = locked;
            Priority    = priority;
            LastUseTime = Time.unscaledTimeAsDouble;
            SpawnCount  = 0;
        }

        /// <summary>
        /// 生成对象。
        /// </summary>
        internal void Spawn()
        {
            SpawnCount++;
            var lastUseTime = LastUseTime;
            try
            {
                LastUseTime = Time.unscaledTimeAsDouble;
                OnSpawn();
            }
            catch
            {
                SpawnCount--;
                LastUseTime = lastUseTime;
                throw;
            }
        }

        /// <summary>
        /// 回收对象。
        /// 与 Spawn() 的"事件抛异常即回滚"对称：无论 OnRecycle() 是否抛异常，都必须推进状态
        /// （生成计数递减 + 刷新最后使用时间），否则对象会因事件异常而永久停留在 IsInUse 状态，
        /// 既无法再次被获取，也无法被销毁。异常照常向上抛出，由调用方感知。
        /// </summary>
        internal void Recycle()
        {
            if (SpawnCount <= 0)
                throw new InvalidOperationException($"[ObjectBase] 对象 '{Name}' 生成次数已经为 0, 回收失败.");

            try
            {
                OnRecycle();
            }
            finally
            {
                SpawnCount--;
                LastUseTime = Time.unscaledTimeAsDouble;
            }
        }

        /// <summary>
        /// 清理对象基类。
        /// </summary>
        public virtual void Clear()
        {
            Name        = null;
            Target      = null;
            Locked      = false;
            Priority    = 0;
            LastUseTime = default;
            SpawnCount  = 0;
        }

        /// <summary>
        /// 生成对象时的事件。
        /// </summary>
        protected virtual void OnSpawn() { }

        /// <summary>
        /// 回收对象时的事件。
        /// </summary>
        protected virtual void OnRecycle() { }

        /// <summary>
        /// 销毁对象时的事件。
        /// </summary>
        protected internal abstract void OnDispose();
    }
}