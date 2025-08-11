using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 对象池内的对象基类。实现了引用对象的接口。
    /// 功能：
    ///     1. 记录了对象的基本信息，如对象名称、目标真实对象、是否被加锁、优先级、上次使用时间，自定义释放检查标记等属性。
    ///     2. 定义了对象生成时、回收时、释放时等生命周期事件。
    /// </summary>
    public abstract class ObjectBase : IReference
    {
        /// 对象名称。
        public string Name { get; private set; }

        /// 对象的目标真实对象。如GameObject
        public object Target { get; private set; }

        /// 对象是否被加锁。
        public bool Locked { get; set; }

        /// 对象的优先级。
        public int Priority { get; set; }

        /// 对象上次使用时间。
        public DateTime LastUseTime { get; internal set; }

        /// 自定义是否可释放标记。。默认为true。
        public virtual bool CustomCanReleaseFlag => true;

        /// <summary>
        /// 初始化对象基类的新实例。
        /// </summary>
        protected ObjectBase()
        {
            Name        = null;
            Target      = null;
            Locked      = false;
            Priority    = 0;
            LastUseTime = default;
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="target">对象的目标真实对象。如GameObject。</param>
        protected void Initialize(object target) => _Initialize(null, target, false, 0);

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
            Name        = name   ?? string.Empty;
            Target      = target ?? throw new FuException(Utility.Text.Format("对象“{0}”为空.", name));
            Locked      = locked;
            Priority    = priority;
            LastUseTime = DateTime.UtcNow;
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
        }

        /// <summary>
        /// 生成对象时的事件。
        /// </summary>
        protected internal virtual void OnSpawn() { }

        /// <summary>
        /// 回收对象时的事件。
        /// </summary>
        protected internal virtual void OnRecycle() { }

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="isShutdown">是否是销毁对象池时触发。</param>
        protected internal abstract void Release(bool isShutdown);
    }
}