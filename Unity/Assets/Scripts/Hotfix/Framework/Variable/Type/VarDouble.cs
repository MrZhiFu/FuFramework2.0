using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// Double 变量类。
    /// 功能：
    ///     1. 可以像正常System.Double变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarDouble : GenericVariable<double>
    {
        /// <summary>
        /// 初始化 VarDouble 变量类的新实例。
        /// </summary>
        public VarDouble() { }

        /// <summary>
        /// 从 System.Double 到 VarDouble 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarDouble(double value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarDouble>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarDouble 变量类到 System.Double 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator double(VarDouble value) => value.Value;
    }
}
