using System;
using FuFramework.Core.Runtime;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.ObjectPool.Runtime
{
    public sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 内部对象。
        /// 包装一个对象池内的目标对象，创建并管理对象生命周期。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        private sealed class Object<T> : IReference where T : ObjectBase
        {
            /// 对象池内的目标对象
            private T m_Object;

            /// <summary>
            /// 获取对象名称。
            /// </summary>
            public string Name => m_Object.Name;

            /// <summary>
            /// 获取对象是否被加锁。
            /// </summary>
            public bool Locked
            {
                get => m_Object.Locked;
                internal set => m_Object.Locked = value;
            }

            /// <summary>
            /// 获取对象的优先级。
            /// </summary>
            public int Priority
            {
                get => m_Object.Priority;
                internal set => m_Object.Priority = value;
            }

            /// <summary>
            /// 获取自定义是否可释放标记。
            /// </summary>
            public bool CustomCanReleaseFlag => m_Object.CustomCanReleaseFlag;

            /// <summary>
            /// 获取对象上次使用时间。
            /// </summary>
            public DateTime LastUseTime => m_Object.LastUseTime;

            /// <summary>
            /// 获取对象是否正在使用。
            /// </summary>
            public bool IsInUse => SpawnCount > 0;

            /// <summary>
            /// 获取对象的获取计数。
            /// </summary>
            public int SpawnCount { get; private set; }

            /// <summary>
            /// 创建对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否提前生成，如果是，则会创建时调用 OnSpawn 事件。</param>
            /// <returns>创建的内部对象。</returns>
            public static Object<T> Create(T obj, bool spawned)
            {
                if (obj == null) throw new FuException("[ObjectPoolModule] 要创建的对象不能为空.");

                var internalObject = ReferencePool.Runtime.ReferencePool.Acquire<Object<T>>();
                internalObject.m_Object     = obj;
                internalObject.SpawnCount = spawned ? 1 : 0;
                
                if (spawned) 
                    obj.OnSpawn();

                return internalObject;
            }

            /// <summary>
            /// 清理内部对象。
            /// </summary>
            public void Clear()
            {
                m_Object     = null;
                SpawnCount = 0;
            }

            /// <summary>
            /// 查看对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Peek() => m_Object;

            /// <summary>
            /// 获取已存在的对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Spawn()
            {
                SpawnCount++;
                m_Object.LastUseTime = DateTime.UtcNow;
                m_Object.OnSpawn();
                return m_Object;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            public void Recycle()
            {
                m_Object.OnRecycle();
                m_Object.LastUseTime = DateTime.UtcNow;
                SpawnCount--;
                if (SpawnCount < 0)
                    throw new FuException($"[ObjectPoolModule] 对象 '{Name}' 生成次数已经小于 0, 回收失败.");
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="isShutdown">是否是销毁对象池时触发的释放。</param>
            public void Release(bool isShutdown)
            {
                m_Object.Release(isShutdown);
                ReferencePool.Runtime.ReferencePool.Release(m_Object);
            }
        }
    }
}