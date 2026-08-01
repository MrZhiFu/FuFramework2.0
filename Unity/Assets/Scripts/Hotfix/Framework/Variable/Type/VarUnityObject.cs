using Hotfix.Framework.Core;
﻿
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 UnityEngine.Object 变量类。
    /// 功能：
    ///     1. 可以像正常 UnityObject 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarUnityObject : GenericVariable<Object>
    {
        /// <summary>
        /// 初始化 VarUnityObject 变量类的新实例。
        /// </summary>
        public VarUnityObject() { }

        /// <summary>
        /// 从 UnityEngine.Object 到 VarUnityObject 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarUnityObject(Object value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarUnityObject>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarUnityObject 变量类到 UnityEngine.Object 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Object(VarUnityObject value) => value.Value;
    }
}
