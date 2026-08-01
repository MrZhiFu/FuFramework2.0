using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 SByte 变量类。
    /// 功能：
    ///     1. 可以像正常 SByte 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarSByte : GenericVariable<sbyte>
    {
        /// <summary>
        /// 初始化 VarSByte 变量类的新实例。
        /// </summary>
        public VarSByte() { }

        /// <summary>
        /// 从 System.SByte 到 VarSByte 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarSByte(sbyte value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarSByte>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarSByte 变量类到 System.SByte 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator sbyte(VarSByte value) => value.Value;
    }
}
