using System;
﻿using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// ID生成器。
        /// 功能：
        ///     1. 生成唯一的长整型ID。
        ///     2. 生成唯一的整型ID。
        /// 
        /// 注意：
        ///     1. 生成的ID是基于时间戳的，确保在不同时间点生成的ID是唯一的。
        ///     2. 生成的ID是原子性地递增的，确保在多线程环境下生成的ID是唯一的。
        /// </summary>
        public static class IdGenerator
        {
            /// <summary>
            /// 全局UTC起始时间，用作计数器的基准时间点
            /// 设置为2020年1月1日0时0分0秒(UTC)
            /// </summary>
            private static readonly DateTime UtcTime = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // 共享计数器
            private static long m_Counter    = (long)(DateTime.UtcNow - UtcTime).TotalSeconds;
            private static int  m_CounterInt = (int)(DateTime.UtcNow  - UtcTime).TotalSeconds;

            /// <summary>
            /// 使用Interlocked.Increment生成唯一ID的方法
            /// </summary>
            /// <returns>返回一个唯一的长整型ID</returns>
            public static long GetNextUniqueId()
            {
                // 原子性地递增值，确保即使多个线程同时尝试递增同一个变量
                return Interlocked.Increment(ref m_Counter);
            }

            /// <summary>
            /// 使用Interlocked.Increment生成唯一ID的方法
            /// </summary>
            /// <returns>返回一个唯一的整型ID</returns>
            public static int GetNextUniqueIntId()
            {
                // 原子性地递增值，确保即使多个线程同时尝试递增同一个变量
                return Interlocked.Increment(ref m_CounterInt);
            }
        }
    }
}