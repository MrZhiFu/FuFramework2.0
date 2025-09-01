using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// UnityEngine.Quaternion 变量类。
    /// 优点：可以像正常Quaternion变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarQuaternion : Variable<Quaternion>
    {
        /// <summary>
        /// 初始化 UnityEngine.Quaternion 变量类的新实例。
        /// </summary>
        public VarQuaternion() { }

        /// <summary>
        /// 从 UnityEngine.Quaternion 到 UnityEngine.Quaternion 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarQuaternion(Quaternion value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarQuaternion>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Quaternion 变量类到 UnityEngine.Quaternion 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Quaternion(VarQuaternion value) => value.Value;
    }
}