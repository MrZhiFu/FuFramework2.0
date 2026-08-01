using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Byte 数组变量类。
    /// 功能：
    ///     1. 可以像正常Byte数组一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarByteArray : GenericVariable<byte[]>
    {
        /// <summary>
        /// 初始化 VarByteArray 变量类的新实例。
        /// </summary>
        public VarByteArray() { }

        /// <summary>
        /// 从 System.Byte 数组到 VarByteArray 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarByteArray(byte[] value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarByteArray>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarByteArray 变量类到 System.Byte 数组的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator byte[](VarByteArray value)
        {
            return value.Value;
        }
    }
}
