using System;
using Hotfix.Framework.Core;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    /// <summary>
    /// 对象池的基类。
    /// 功能：
    ///     1.记录了对象池的名称、类型、数量、可销毁数量、是否允许多次获取、自动销毁间隔、容量、过期秒数、优先级等信息。
    ///     2.定义了对象池轮询、关闭并清理对象池、销毁对象池中的可销毁对象、尝试销毁对象池中的指定数量的对象、获取所有对象信息等接口。
    /// </summary>
    public abstract class ObjectPoolBase
    {
        /// <summary>
        /// 获取对象池名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取对象池完整名称。
        /// </summary>
        public string FullName => new TypeNamePair(ObjectType, Name).ToString();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">对象池名称。</param>
        /// <exception cref="InvalidOperationException">对象池名称不能为空。</exception>
        protected ObjectPoolBase(string name)
        {
            // 硬性要求：对象池必须命名，才能与其他同名类型池区分
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("[ObjectPoolBase] 对象池名称不能为空，对象池必须命名.");

            Name = name;
        }

        #region 抽象属性

        /// <summary>
        /// 获取对象池对象类型。
        /// </summary>
        public abstract Type ObjectType { get; }

        /// <summary>
        /// 获取对象池中对象的数量。
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// 获取对象池中能被销毁的对象的数量。
        /// </summary>
        public abstract int CanDisposeCount { get; }

        /// <summary>
        /// 获取对象池中的对象时，是否允许获取正在被使用的对象。一般都为false。
        /// false--对象只能在回收后才能再次被获取，即池中会存在多个同名对象;
        /// true --对象能在未回收的状态下就能再次被获取，这样会使得池中的对象只有一个，每次获取之后这个对象的引用计数++
        /// </summary>
        public abstract bool AllowSpawnInUse { get; }

        /// <summary>
        /// 获取或设置对象池自动销毁可销毁对象的间隔秒数。
        /// </summary>
        public abstract float AutoDisposeInterval { get; set; }

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

        #endregion

        #region 抽象方法

        /// <summary>
        /// 对象池轮询。
        /// </summary>
        /// <param name="unscaledDeltaTime">无缩放的帧间隔时间。</param>
        internal abstract void Update(float unscaledDeltaTime);

        /// <summary>
        /// 关闭并清理对象池。
        /// </summary>
        internal abstract void OnDispose();

        /// <summary>
        /// 销毁对象池中的所有未使用对象。
        /// </summary>
        public abstract void DisposeAllUnused();

        /// <summary>
        /// 销毁对象池中超过容量的可销毁对象。
        /// </summary>
        public abstract void DisposeOverCapacity();

        /// <summary>
        /// 获取所有对象信息。
        /// </summary>
        /// <returns>所有对象信息。</returns>
        public abstract ObjectInfo[] GetAllObjectInfos();

        #endregion
    }
}