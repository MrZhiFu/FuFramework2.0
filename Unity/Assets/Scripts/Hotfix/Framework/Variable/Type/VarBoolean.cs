using Hotfix.Framework.ReferencePools;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Boolean 变量类。
    /// 功能：
    ///     1. 可以像正常bool值类型一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarBoolean : GenericVariable<bool>
    {
        /// <summary>
        /// 初始化 VarBoolean 变量类的新实例。
        /// </summary>
        public VarBoolean() { }

        /// <summary>
        /// 从 System.Boolean 到 VarBoolean 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarBoolean(bool value)
        {
            var varValue = ReferencePool.Acquire<VarBoolean>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarBoolean 变量类到 System.Boolean 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator bool(VarBoolean value) => value.Value;
    }
}
