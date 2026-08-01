using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// Float 变量类。
    /// 功能：
    ///     1. 可以像正常System.Float变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarFloat : GenericVariable<float>
    {
        /// <summary>
        /// 初始化 VarFloat 变量类的新实例。
        /// </summary>
        public VarFloat() { }

        /// <summary>
        /// 从 System.Single 到 VarFloat 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarFloat(float value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarFloat>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarFloat 变量类到 System.Float 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator float(VarFloat value) => value.Value;
    }
}
