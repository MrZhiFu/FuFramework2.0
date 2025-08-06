using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 对象池的基类。
    /// 功能：
    ///     1.记录了对象池的名称、类型、数量、可释放数量、允许多次获取、自动释放间隔、容量、过期秒数、优先级等信息。
    ///     2.定义了对象池轮询、关闭并清理对象池、释放对象池中的可释放对象、尝试释放对象池中的指定数量的对象、获取所有对象信息等接口。
    /// </summary>
    public abstract class ObjectPoolBase
    {
        /// <summary>
        /// 初始化对象池基类的新实例。
        /// </summary>
        protected ObjectPoolBase() : this(null) { }

        /// <summary>
        /// 初始化对象池基类的新实例。
        /// </summary>
        /// <param name="name">对象池名称。</param>
        protected ObjectPoolBase(string name) => Name = name ?? string.Empty;

        /// <summary>
        /// 获取对象池名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取对象池完整名称。
        /// </summary>
        public string FullName => new TypeNamePair(ObjectType, Name).ToString();

        /// <summary>
        /// 获取对象池对象类型。
        /// </summary>
        public abstract Type ObjectType { get; }

        /// <summary>
        /// 获取对象池中对象的数量。
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// 获取对象池中能被释放的对象的数量。
        /// </summary>
        public abstract int CanReleaseCount { get; }

        /// <summary>
        /// 是否允许对象在使用时获取。
        /// false--对象只能在回收后才能再次被获取，即池中会存在多个同名对象;
        /// true--对象能在未回收的状态下就能再次被获取，这样会使得池中的对象永远只有一个
        /// </summary>
        public abstract bool AllowSpawnInUse { get; }

        /// <summary>
        /// 获取或设置对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        public abstract float AutoReleaseInterval { get; set; }

        /// <summary>
        /// 获取或设置对象池的容量。
        /// </summary>
        public abstract int Capacity { get; set; }

        /// <summary>
        /// 获取或设置对象池对象过期秒数。
        /// </summary>
        public abstract float ExpireTime { get; set; }

        /// <summary>
        /// 获取或设置对象池的优先级。
        /// </summary>
        public abstract int Priority { get; set; }

        /// <summary>
        /// 对象池轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        
        internal abstract void Update(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 关闭并清理对象池。
        /// </summary>
        internal abstract void Shutdown();

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        public abstract void Release();

        /// <summary>
        /// 尝试释放对象池中的指定数量的对象。
        /// </summary>
        /// <param name="toReleaseCount">尝试释放对象数量。</param>
        public abstract void Release(int toReleaseCount);

        /// <summary>
        /// 释放对象池中的所有未使用对象。
        /// </summary>
        public abstract void ReleaseAllUnused();

        /// <summary>
        /// 获取所有对象信息。
        /// </summary>
        /// <returns>所有对象信息。</returns>
        public abstract ObjectInfo[] GetAllObjectInfos();
    }
}