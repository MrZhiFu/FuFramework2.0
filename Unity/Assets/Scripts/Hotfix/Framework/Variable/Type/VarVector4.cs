using Hotfix.Framework.ReferencePools;
﻿using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 UnityEngine.Vector4 变量类。
    /// 功能：
    ///     1. 可以像正常 Vector4 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarVector4 : GenericVariable<Vector4>
    {
        /// <summary>
        /// 初始化 VarVector4 变量类的新实例。
        /// </summary>
        public VarVector4() { }

        /// <summary>
        /// 从 UnityEngine.Vector4 到 VarVector4 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarVector4(Vector4 value)
        {
            var varValue = ReferencePool.Acquire<VarVector4>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarVector4 变量类到 UnityEngine.Vector4 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Vector4(VarVector4 value) => value.Value;
    }
}
