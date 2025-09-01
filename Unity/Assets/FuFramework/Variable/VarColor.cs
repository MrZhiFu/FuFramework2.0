using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// UnityEngine.Color 变量类。
    /// 优点：可以像正常Color变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarColor : Variable<Color>
    {
        /// <summary>
        /// 初始化 UnityEngine.Color 变量类的新实例。
        /// </summary>
        public VarColor() { }

        /// <summary>
        /// 从 UnityEngine.Color 到 UnityEngine.Color 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarColor(Color value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarColor>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Color 变量类到 UnityEngine.Color 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Color(VarColor value) => value.Value;
    }
}