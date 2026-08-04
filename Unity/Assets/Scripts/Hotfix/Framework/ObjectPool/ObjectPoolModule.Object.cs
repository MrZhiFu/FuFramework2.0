using System;
using Hotfix.Framework.Core;
using Hotfix.Framework.ReferencePool;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.ObjectPool
{
    public sealed partial class ObjectPoolModule
    {
        /// <summary>
        /// 内部数据对象。
        /// 功能：
        ///     1. 包装一个对象池内的目标对象，创建并管理对象生命周期。
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        private sealed class Object<T> : IReference where T : ObjectBase
        {
            /// 对象池内的目标对象
            internal T TargetObject { get; private set; }

            /// <summary>
            /// 获取对象名称。
            /// </summary>
            public string Name => TargetObject.Name;

            /// <summary>
            /// 对象是否被加锁(加锁的对象不会被释放)。
            /// </summary>
            public bool Locked
            {
                get => TargetObject.Locked;
                internal set => TargetObject.Locked = value;
            }

            /// <summary>
            /// 对象的优先级。
            /// </summary>
            public int Priority
            {
                get => TargetObject.Priority;
                internal set => TargetObject.Priority = value;
            }

            /// <summary>
            /// 获取自定义是否可释放标记。
            /// </summary>
            public bool CustomCanReleaseFlag => TargetObject.CustomCanReleaseFlag;

            /// <summary>
            /// 获取对象上次使用时间。
            /// </summary>
            public DateTime LastUseTime => TargetObject.LastUseTime;

            /// <summary>
            /// 获取对象是否正在使用。
            /// </summary>
            public bool IsInUse => SpawnCount > 0;

            /// <summary>
            /// 对象的生成计数。
            /// </summary>
            public int SpawnCount { get; private set; }

            /// <summary>
            /// 创建对象。
            /// </summary>
            /// <param name="obj">对象。</param>
            /// <param name="spawned">对象是否已被提前生成，如果是，则会调用OnSpawn。</param>
            /// <returns>创建的内部对象。</returns>
            public static Object<T> Create(T obj, bool spawned)
            {
                if (obj == null) throw new InvalidOperationException("[ObjectPoolModule] 要创建的对象不能为空.");

                var tempObj = GlobalModule.ReferencePoolModule.Acquire<Object<T>>();
                tempObj.TargetObject = obj;
                tempObj.SpawnCount   = spawned ? 1 : 0;

                if (spawned)
                    obj.OnSpawn();

                return tempObj;
            }

            /// <summary>
            /// 清理内部对象。
            /// </summary>
            public void Clear()
            {
                TargetObject = null;
                SpawnCount   = 0;
            }

            /// <summary>
            /// 获取已存在的对象。
            /// </summary>
            /// <returns>对象。</returns>
            public T Spawn()
            {
                SpawnCount++;
                try
                {
                    TargetObject.LastUseTime = DateTime.UtcNow;
                    TargetObject.OnSpawn();
                }
                catch
                {
                    // OnSpawn 异常时回滚计数，避免生成计数与真实状态失配
                    SpawnCount--;
                    throw;
                }

                return TargetObject;
            }

            /// <summary>
            /// 回收对象。
            /// </summary>
            public void Recycle()
            {
                if (SpawnCount <= 0)
                    throw new InvalidOperationException($"[ObjectPoolModule] 对象 '{Name}' 生成次数已经为 0, 回收失败.");

                TargetObject.OnRecycle();
                TargetObject.LastUseTime = DateTime.UtcNow;
                SpawnCount--;
            }

            /// <summary>
            /// 释放对象。
            /// </summary>
            public void OnRelease()
            {
                try
                {
                    TargetObject.OnRelease();
                }
                finally
                {
                    // 即使 OnRelease 异常也回收目标对象到引用池，避免跳过清理
                    GlobalModule.ReferencePoolModule.Recycle(TargetObject);
                }
            }
        }
    }
}
