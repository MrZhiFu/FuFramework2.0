using System;

namespace GameFrameX.Runtime
{
    /// <summary>
    /// 游戏框架异常静态方法
    /// </summary>
    public static class GameFrameworkGuard
    {
        /// <summary>
        /// 确保指定的值不为null。
        /// </summary>
        /// <param name="value">要检查的值。</param>
        /// <param name="name">值的名称。</param>
        /// <exception cref="ArgumentNullException">当值为null时引发。</exception>
        public static void NotNullOrEmpty(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(name, "不能为空.");
        }


        /// <summary>
        /// 确保指定的值不为null。
        /// </summary>
        /// <typeparam name="T">值的类型。</typeparam>
        /// <param name="value">要检查的值。</param>
        /// <param name="name">值的名称。</param>
        /// <exception cref="ArgumentNullException">当值为null时引发。</exception>
        public static void NotNull<T>(T value, string name) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(name, "不能为空.");
        }

        /// <summary>
        /// 检查值是否在指定范围内，如果不在范围内则抛出 ArgumentOutOfRangeException 异常。
        /// </summary>
        /// <param name="value">要检查的值。</param>
        /// <param name="min">允许的最小值。</param>
        /// <param name="max">允许的最大值。</param>
        /// <param name="name">值的名称。</param>
        /// <exception cref="ArgumentOutOfRangeException">当值不在指定范围内时抛出。</exception>
        public static void NotRange(int value, int min, int max, string name)
        {
            if (value > max || value < min)
                throw new ArgumentOutOfRangeException(name, "值必须在" + min + " 到 " + max + "之间.");
        }
    }
}