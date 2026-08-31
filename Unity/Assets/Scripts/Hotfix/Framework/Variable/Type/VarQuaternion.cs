using Hotfix.Framework.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Quaternion 变量类。
    /// 功能：
    ///     1. 可以像正常Quaternion变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarQuaternion : GenericVariable<Quaternion>
    {
        /// <summary>
        /// 初始化 VarQuaternion 变量类的新实例。
        /// </summary>
        public VarQuaternion() { }

        /// <summary>
        /// 从 UnityEngine.Quaternion 到 VarQuaternion 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarQuaternion(Quaternion value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarQuaternion>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarQuaternion 变量类到 UnityEngine.Quaternion 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Quaternion(VarQuaternion value) => value.Value;
    }
}
