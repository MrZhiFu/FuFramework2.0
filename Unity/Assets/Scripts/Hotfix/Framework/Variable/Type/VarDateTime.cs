using Hotfix.Framework.ReferencePools;
﻿using System;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 DateTime 变量类。
    /// 功能：
    ///     1. 可以像正常System.DateTime变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarDateTime : GenericVariable<DateTime>
    {
        /// <summary>
        /// 初始化 VarDateTime 变量类的新实例。
        /// </summary>
        public VarDateTime() { }

        /// <summary>
        /// 从 System.DateTime 到 VarDateTime 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarDateTime(DateTime value)
        {
            var varValue = ReferencePool.Acquire<VarDateTime>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarDateTime 变量类到 System.DateTime 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator DateTime(VarDateTime value) => value.Value;
    }
}
