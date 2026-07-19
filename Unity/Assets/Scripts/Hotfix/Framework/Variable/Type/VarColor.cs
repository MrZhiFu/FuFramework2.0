using Hotfix.Framework.ReferencePools;
﻿using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Color 变量类。
    /// 功能：
    ///     1. 可以像正常UnityEngine.Color变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarColor : GenericVariable<Color>
    {
        /// <summary>
        /// 初始化 VarColor 变量类的新实例。
        /// </summary>
        public VarColor() { }

        /// <summary>
        /// 从 UnityEngine.Color 到 VarColor 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarColor(Color value)
        {
            var varValue = ReferencePool.Acquire<VarColor>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarColor 变量类到 UnityEngine.Color 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Color(VarColor value) => value.Value;
    }
}
