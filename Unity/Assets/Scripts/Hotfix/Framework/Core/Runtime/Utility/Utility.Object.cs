using System;
// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// Object相关的实用函数。
        /// 功能：
        ///     1. 交换两个对象的值。
        /// </summary>
        public static class Object
        {
            /// <summary>
            /// 交换两个对象的值。
            /// </summary>
            /// <param name="t1">第一个对象的引用。</param>
            /// <param name="t2">第二个对象的引用。</param>
            /// <typeparam name="T">对象的类型。</typeparam>
            public static void Swap<T>(ref T t1, ref T t2) => (t1, t2) = (t2, t1);
        }
    }
}