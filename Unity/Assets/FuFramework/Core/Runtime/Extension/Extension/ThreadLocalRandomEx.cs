using System;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 线程私有random对象。
    /// 每个线程都有自己的随机数生成器，保证线程安全
    /// </summary>
    public static class ThreadLocalRandomEx
    {
        private static int _seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        /// <summary>
        /// 获取当前线程的随机数生成器
        /// </summary>
        public static Random Current => Rng.Value;
    }
}