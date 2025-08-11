
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// UnityEngine.Object 变量类。
    /// 优点：可以像正常 UnityObject 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarUnityObject : Variable<Object>
    {
        /// <summary>
        /// 初始化 UnityEngine.Object 变量类的新实例。
        /// </summary>
        public VarUnityObject() { }

        /// <summary>
        /// 从 UnityEngine.Object 到 UnityEngine.Object 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarUnityObject(Object value)
        {
            var varValue = ReferencePool.Acquire<VarUnityObject>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Object 变量类到 UnityEngine.Object 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Object(VarUnityObject value) => value.Value;
    }
}