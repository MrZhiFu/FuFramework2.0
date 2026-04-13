using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// 自定义 Color32 变量类。
    /// 功能：
    ///     1. 可以像正常UnityEngine.Color32变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarColor32 : Variable<Color32>
    {
        /// <summary>
        /// 初始化 VarColor32 变量类的新实例。
        /// </summary>
        public VarColor32() { }

        /// <summary>
        /// 从 UnityEngine.Color32 到 VarColor32 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarColor32(Color32 value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarColor32>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarColor32 变量类到 UnityEngine.Color32 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Color32(VarColor32 value) => value.Value;
    }
}