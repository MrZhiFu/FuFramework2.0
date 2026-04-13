using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// 自定义 Texture 变量类。
    /// 功能：
    ///     1. 可以像正常 Texture 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarTexture : Variable<Texture>
    {
        /// <summary>
        /// 初始化 VarTexture 变量类的新实例。
        /// </summary>
        public VarTexture() { }

        /// <summary>
        /// 从 UnityEngine.Texture 到 VarTexture 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarTexture(Texture value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarTexture>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarTexture 变量类到 UnityEngine.Texture 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Texture(VarTexture value) => value.Value;
    }
}