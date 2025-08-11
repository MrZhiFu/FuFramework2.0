using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 对象信息。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ObjectInfo
    {
        /// <summary>
        /// 初始化对象信息的新实例。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="locked">对象是否被加锁。</param>
        /// <param name="customCanReleaseFlag">对象自定义释放检查标记。</param>
        /// <param name="priority">对象的优先级。</param>
        /// <param name="lastUseTime">对象上次使用时间。</param>
        /// <param name="spawnCount">对象的获取计数。</param>
        // Preserve the constructor for Unity's serialization
        public ObjectInfo(string name, bool locked, bool customCanReleaseFlag, int priority, DateTime lastUseTime, int spawnCount)
        {
            Name = name;
            Locked = locked;
            CustomCanReleaseFlag = customCanReleaseFlag;
            Priority = priority;
            LastUseTime = lastUseTime;
            SpawnCount = spawnCount;
        }

        /// <summary>
        /// 获取对象名称。
        /// </summary>
        // Preserve the Name property for Unity's serialization
        public string Name { get; }

        /// <summary>
        /// 获取对象是否被加锁。
        /// </summary>
        // Preserve the Locked property for Unity's serialization
        public bool Locked { get; }

        /// <summary>
        /// 获取对象自定义释放检查标记。
        /// </summary>
        // Preserve the CustomCanReleaseFlag property for Unity's serialization
        public bool CustomCanReleaseFlag { get; }

        /// <summary>
        /// 获取对象的优先级。
        /// </summary>
        // Preserve the Priority property for Unity's serialization
        public int Priority { get; }

        /// <summary>
        /// 获取对象上次使用时间。
        /// </summary>
        // Preserve the LastUseTime property for Unity's serialization
        public DateTime LastUseTime { get; }

        /// <summary>
        /// 获取对象的获取计数。
        /// </summary>
        // Preserve the SpawnCount property for Unity's serialization
        public int SpawnCount { get; }

        /// <summary>
        /// 获取对象是否正在使用。
        /// </summary>
        // Preserve the IsInUse property for Unity's serialization
        public bool IsInUse => SpawnCount > 0;
    }
}
