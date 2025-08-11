using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        /// <summary>
        /// 随机相关的实用函数。
        /// 功能：
        /// 1. 设置随机数种子；
        /// 2. 返回非负随机数；
        /// 3. 返回一个小于所指定最大值的非负随机数；
        /// 4. 返回一个指定范围内的随机数；
        /// 5. 返回一个介于 0.0 和 1.0 之间的随机数；
        /// 6. 用随机数填充指定字节数组的元素；
        /// </summary>
        public static class Random
        {
            private static System.Random _random = new((int)DateTime.UtcNow.Ticks);

            /// <summary>
            /// 设置随机数种子。
            /// </summary>
            /// <param name="seed">随机数种子。</param>
            public static void SetSeed(int seed) => _random = new System.Random(seed);

            /// <summary>
            /// 返回非负随机数。
            /// </summary>
            /// <returns>大于等于零且小于 System.Int32.MaxValue 的 32 位带符号整数。</returns>
            public static int GetRandom() => _random.Next();

            /// <summary>
            /// 返回一个小于所指定最大值的非负随机数。
            /// </summary>
            /// <param name="maxValue">要生成的随机数的上界（随机数不能取该上界值）。maxValue 必须大于等于零。</param>
            /// <returns>大于等于零且小于 maxValue 的 32 位带符号整数，即：返回值的范围通常包括零但不包括 maxValue。不过，如果 maxValue 等于零，则返回 maxValue。</returns>
            public static int GetRandom(int maxValue) => _random.Next(maxValue);

            /// <summary>
            /// 返回一个指定范围内的随机数。
            /// </summary>
            /// <param name="minValue">返回的随机数的下界（随机数可取该下界值）。</param>
            /// <param name="maxValue">返回的随机数的上界（随机数不能取该上界值）。maxValue 必须大于等于 minValue。</param>
            /// <returns>一个大于等于 minValue 且小于 maxValue 的 32 位带符号整数，即：返回的值范围包括 minValue 但不包括 maxValue。如果 minValue 等于 maxValue，则返回 minValue。</returns>
            public static int GetRandom(int minValue, int maxValue) => _random.Next(minValue, maxValue);

            /// <summary>
            /// 返回一个介于 0.0 和 1.0 之间的随机数。
            /// </summary>
            /// <returns>大于等于 0.0 并且小于 1.0 的双精度浮点数。</returns>
            public static double GetRandomDouble() => _random.NextDouble();

            /// <summary>
            /// 用随机数填充指定字节数组的元素。
            /// </summary>
            /// <param name="buffer">包含随机数的字节数组。</param>
            public static void GetRandomBytes(byte[] buffer) => _random.NextBytes(buffer);
        }
    }
}