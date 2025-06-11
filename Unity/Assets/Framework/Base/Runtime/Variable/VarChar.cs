//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFrameX.Runtime
{
    /// <summary>
    /// System.Char 变量类。
    /// 优点：可以像正常Char变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarChar : Variable<char>
    {
        /// <summary>
        /// 初始化 System.Char 变量类的新实例。
        /// </summary>
        public VarChar() { }

        /// <summary>
        /// 从 System.Char 到 System.Char 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarChar(char value)
        {
            var varValue = ReferencePool.Acquire<VarChar>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Char 变量类到 System.Char 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator char(VarChar value) => value.Value;
    }
}