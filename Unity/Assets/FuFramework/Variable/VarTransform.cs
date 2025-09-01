using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// UnityEngine.Transform 变量类。
    /// 优点：可以像正常 Transform 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarTransform : Variable<Transform>
    {
        /// <summary>
        /// 初始化 UnityEngine.Transform 变量类的新实例。
        /// </summary>
        public VarTransform() { }

        /// <summary>
        /// 从 UnityEngine.Transform 到 UnityEngine.Transform 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarTransform(Transform value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarTransform>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Transform 变量类到 UnityEngine.Transform 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Transform(VarTransform value) => value.Value;
    }
}