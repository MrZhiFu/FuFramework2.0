using Hotfix.Framework.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 UnityEngine.Vector3 变量类。
    /// 功能：
    ///     1. 可以像正常 Vector3 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarVector3 : GenericVariable<Vector3>
    {
        /// <summary>
        /// 初始化 VarVector3 变量类的新实例。
        /// </summary>
        public VarVector3() { }

        /// <summary>
        /// 从 UnityEngine.Vector3 到 VarVector3 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarVector3(Vector3 value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarVector3>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarVector3 变量类到 UnityEngine.Vector3 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Vector3(VarVector3 value) => value.Value;
    }
}
