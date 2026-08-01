using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Int16 变量类。
    /// 功能：
    ///     1. 可以像正常Int16变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarInt16 : GenericVariable<short>
    {
        /// <summary>
        /// 初始化 VarInt16 变量类的新实例。
        /// </summary>
        public VarInt16() { }

        /// <summary>
        /// 从 System.Int16 到 VarInt16 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarInt16(short value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarInt16>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarInt16 变量类到 System.Int16 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator short(VarInt16 value) => value.Value;
    }
}
