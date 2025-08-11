using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 引用池。
    /// </summary>
    public static partial class ReferencePool
    {
        /// <summary>
        /// 记录指定类型下的引用对象集合的字典, key:指定类型--Value:该类型下的引用对象信息集合
        /// </summary>
        private static readonly Dictionary<Type, ReferenceCollection> s_ReferenceCollectionDict = new();

        /// <summary>
        /// 是否开启强制检查。
        /// 当开启时，会检查所有传入的引用类型是否合法，合法的定义为：非空，非抽象类，实现了 IReference 接口，
        /// 同时，在 Release 调用时，会检查传入的引用是否已经可以重复归还-EnQueue。
        /// </summary>
        public static bool EnableStrictCheck { get; set; }

        /// <summary>
        /// 获取引用池的数量。
        /// </summary>
        // ReSharper disable once InconsistentlySynchronizedField
        public static int Count => s_ReferenceCollectionDict?.Count ?? 0;

        /// <summary>
        /// 获取所有引用池的信息。
        /// </summary>
        /// <returns>所有引用池的信息。</returns>
        public static ReferencePoolInfo[] GetAllReferencePoolInfos()
        {
            var index = 0;

            ReferencePoolInfo[] results;

            lock (s_ReferenceCollectionDict)
            {
                results = new ReferencePoolInfo[s_ReferenceCollectionDict.Count];
                foreach (var (type, refCollection) in s_ReferenceCollectionDict)
                {
                    results[index++] = new ReferencePoolInfo(type, refCollection.UnusedReferenceCount, refCollection.UsingReferenceCount,
                                                             refCollection.AcquireReferenceCount, refCollection.ReleaseReferenceCount,
                                                             refCollection.AddReferenceCount, refCollection.RemoveReferenceCount);
                }
            }

            return results;
        }

        /// <summary>
        /// 清除所有引用池。
        /// </summary>
        public static void ClearAll()
        {
            lock (s_ReferenceCollectionDict)
            {
                foreach (var (_, refCollection) in s_ReferenceCollectionDict)
                {
                    refCollection.RemoveAll();
                }

                s_ReferenceCollectionDict.Clear();
            }
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <returns>引用。</returns>
        public static T Acquire<T>() where T : class, IReference, new()
        {
            return _GetReferenceCollection(typeof(T)).Acquire<T>();
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <returns>引用。</returns>
        public static IReference Acquire(Type refType)
        {
            _CheckReferenceType(refType);
            return _GetReferenceCollection(refType).Acquire();
        }

        /// <summary>
        /// 将引用归还引用池。
        /// </summary>
        /// <param name="reference">要归还的引用。</param>
        public static void Release(IReference reference)
        {
            if (reference == null)
                throw new FuException("要归还的引用对象为空.");

            var refType = reference.GetType();
            _CheckReferenceType(refType);
            _GetReferenceCollection(refType).Release(reference);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">追加数量。</param>
        public static void Add<T>(int count) where T : class, IReference, new()
        {
            _GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <param name="count">追加数量。</param>
        public static void Add(Type refType, int count)
        {
            _CheckReferenceType(refType);
            _GetReferenceCollection(refType).Add(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量。</param>
        public static void Remove<T>(int count) where T : class, IReference
        {
            _GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        /// <param name="count">移除数量。</param>
        public static void Remove(Type refType, int count)
        {
            _CheckReferenceType(refType);
            _GetReferenceCollection(refType).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除所有的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public static void RemoveAll<T>() where T : class, IReference
        {
            _GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 从引用池中移除所有的引用。
        /// </summary>
        /// <param name="refType">引用类型。</param>
        public static void RemoveAll(Type refType)
        {
            _CheckReferenceType(refType);
            _GetReferenceCollection(refType).RemoveAll();
        }

        /// <summary>
        /// 检查引用类型是否合法
        /// 合法的定义为：非空，非抽象类，实现了 IReference 接口
        /// </summary>
        /// <param name="refType"></param>
        private static void _CheckReferenceType(Type refType)
        {
            if (!EnableStrictCheck) return;

            if (refType == null)
                throw new FuException("引用类型为空.");

            if (!refType.IsClass || refType.IsAbstract)
                throw new FuException("引用类型不是非抽象类类型.");

            if (!typeof(IReference).IsAssignableFrom(refType))
                throw new FuException(Utility.Text.Format("引用类型 '{0}' 不是 IReference 类型.", refType.FullName));
        }

        /// <summary>
        /// 获取指定类型下的引用信息集合
        /// </summary>
        /// <param name="refType"></param>
        /// <returns>引用信息集合</returns>
        private static ReferenceCollection _GetReferenceCollection(Type refType)
        {
            if (refType == null)
                throw new FuException("引用类型为空.");

            ReferenceCollection referenceCollection;

            lock (s_ReferenceCollectionDict)
            {
                if (s_ReferenceCollectionDict.TryGetValue(refType, out referenceCollection))
                    return referenceCollection;

                referenceCollection = new ReferenceCollection(refType);
                s_ReferenceCollectionDict.Add(refType, referenceCollection);
            }

            return referenceCollection;
        }
    }
}