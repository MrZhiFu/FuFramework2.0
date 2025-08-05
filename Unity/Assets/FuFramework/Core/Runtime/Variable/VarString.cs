//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FuFramework.Core.Runtime;

namespace GameFrameX.Runtime
{
    /// <summary>
    /// System.String 变量类。
    /// 优点：可以像正常 String 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarString : Variable<string>
    {
        /// <summary>
        /// 初始化 System.String 变量类的新实例。
        /// </summary>
        public VarString() { }

        /// <summary>
        /// 从 System.String 到 System.String 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarString(string value)
        {
            var varValue = ReferencePool.Acquire<VarString>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.String 变量类到 System.String 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator string(VarString value) => value.Value;
    }
}