using Hotfix.Framework.ReferencePools;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Int64 变量类。
    /// 功能：
    ///     1. 可以像正常Int64变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarInt64 : GenericVariable<long>
    {
        /// <summary>
        /// 初始化 VarInt64 变量类的新实例。
        /// </summary>
        public VarInt64() { }

        /// <summary>
        /// 从 System.Int64 到 VarInt64 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarInt64(long value)
        {
            var varValue = ReferencePool.Acquire<VarInt64>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarInt64 变量类到 System.Int64 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator long(VarInt64 value) => value.Value;
    }
}
