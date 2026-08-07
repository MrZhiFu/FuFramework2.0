using System;
using Hotfix.Framework.ReferencePool;

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
        /// 对象上次使用时间。
        /// </summary>
        public DateTime LastUseTime { get; private set; }

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
            LastUseTime = DateTime.UtcNow;
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
                LastUseTime = DateTime.UtcNow;
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
        /// </summary>
        internal void Recycle()
        {
            if (SpawnCount <= 0)
                throw new InvalidOperationException($"[ObjectBase] 对象 '{Name}' 生成次数已经为 0, 回收失败.");

            OnRecycle();
            LastUseTime = DateTime.UtcNow;
            SpawnCount--;
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