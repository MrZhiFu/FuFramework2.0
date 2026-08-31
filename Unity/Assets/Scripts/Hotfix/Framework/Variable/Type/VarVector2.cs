using Hotfix.Framework.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 UnityEngine.Vector2 变量类。
    /// 功能：
    ///     1. 可以像正常 Vector2 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarVector2 : GenericVariable<Vector2>
    {
        /// <summary>
        /// 初始化 VarVector2 变量类的新实例。
        /// </summary>
        public VarVector2() { }

        /// <summary>
        /// 从 UnityEngine.Vector2 到 VarVector2 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarVector2(Vector2 value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarVector2>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarVector2 变量类到 UnityEngine.Vector2 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Vector2(VarVector2 value) => value.Value;
    }
}
