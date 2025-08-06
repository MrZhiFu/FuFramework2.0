using System;
using GameFrameX.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    internal sealed partial class ObjectPoolManager : GameFrameworkModule, IObjectPoolManager
    {
        /// <summary>
        /// 内部对象。
        /// 包装一个对象池内的对象，创建并管理对象生命周期。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        private sealed class Object<T> : IReference where T : ObjectBase
        {
            /// 对象池内的对象实例
            private T m_Object;

            /// 对象获取计数
            private int m_SpawnCount;

            /// <summary>
            /// 初始化内部对象的新实例。
            /// </summary>
            public Object()
            {
                m_Object     = null;
                m_SpawnCount = 0;
            }

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
            public bool IsInUse => m_SpawnCount > 0;

            /// <summary>
            /// 获取对象的获取计数。
            /// </summary>
            public int SpawnCount => m_SpawnCount;

            /// <summary>
            /// 创建内部对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否提前生成，如果是，则会创建时调用 OnSpawn 事件。</param>
            /// <returns>创建的内部对象。</returns>
            public static Object<T> Create(T obj, bool spawned)
            {
                if (obj == null) throw new GameFrameworkException("要创建的对象不能为空.");

                var internalObject = ReferencePool.Acquire<Object<T>>();
                internalObject.m_Object     = obj;
                internalObject.m_SpawnCount = spawned ? 1 : 0;
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
                m_SpawnCount = 0;
            }

            /// <summary>
            /// 查看对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Peek() => m_Object;

            /// <summary>
            /// 获取对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Spawn()
            {
                m_SpawnCount++;
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
                m_SpawnCount--;
                if (m_SpawnCount < 0)
                    throw new GameFrameworkException(Utility.Text.Format("对象 '{0}' 生成次数已经小于 0, 回收失败.", Name));
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            /// <param name="isShutdown">是否是销毁对象池时触发的释放。</param>
            public void Release(bool isShutdown)
            {
                m_Object.Release(isShutdown);
                ReferencePool.Release(m_Object);
            }
        }
    }
}