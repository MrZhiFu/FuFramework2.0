using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Char 数组变量类。
    /// 功能：
    ///     1. 可以像正常Char数组一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarCharArray : GenericVariable<char[]>
    {
        /// <summary>
        /// 初始化 VarCharArray 变量类的新实例。
        /// </summary>
        public VarCharArray() { }

        /// <summary>
        /// 从 System.Char 数组到 VarCharArray 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarCharArray(char[] value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarCharArray>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarCharArray 变量类到 System.Char 数组的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator char[](VarCharArray value) => value.Value;
    }
}
