//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFrameX.Runtime
{
    /// <summary>
    /// System.SByte 变量类。
    /// 优点：可以像正常 SByte 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarSByte : Variable<sbyte>
    {
        /// <summary>
        /// 初始化 System.SByte 变量类的新实例。
        /// </summary>
        public VarSByte() { }

        /// <summary>
        /// 从 System.SByte 到 System.SByte 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarSByte(sbyte value)
        {
            var varValue = ReferencePool.Acquire<VarSByte>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.SByte 变量类到 System.SByte 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator sbyte(VarSByte value) => value.Value;
    }
}