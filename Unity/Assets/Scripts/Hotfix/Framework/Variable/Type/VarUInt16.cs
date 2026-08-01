using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 UInt16 变量类。
    /// 功能：
    ///     1. 可以像正常 UInt16 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarUInt16 : GenericVariable<ushort>
    {
        /// <summary>
        /// 初始化 VarUInt16 变量类的新实例。
        /// </summary>
        public VarUInt16() { }

        /// <summary>
        /// 从 System.UInt16 到 VarUInt16 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarUInt16(ushort value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarUInt16>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarUInt16 变量类到 System.UInt16 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ushort(VarUInt16 value) => value.Value;
    }
}
