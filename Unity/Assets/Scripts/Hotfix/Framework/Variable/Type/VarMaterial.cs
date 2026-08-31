using Hotfix.Framework.Core;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Material 变量类。
    /// 功能：
    ///     1. 可以像正常Material变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarMaterial : GenericVariable<Material>
    {
        /// <summary>
        /// 初始化 VarMaterial 变量类的新实例。
        /// </summary>
        public VarMaterial() { }

        /// <summary>
        /// 从 UnityEngine.Material 到 VarMaterial 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarMaterial(Material value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarMaterial>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarMaterial 变量类到 UnityEngine.Material 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Material(VarMaterial value) => value.Value;
    }
}
