using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// UnityEngine.Texture 变量类。
    /// 优点：可以像正常 Texture 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarTexture : Variable<Texture>
    {
        /// <summary>
        /// 初始化 UnityEngine.Texture 变量类的新实例。
        /// </summary>
        public VarTexture() { }

        /// <summary>
        /// 从 UnityEngine.Texture 到 UnityEngine.Texture 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarTexture(Texture value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarTexture>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Texture 变量类到 UnityEngine.Texture 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Texture(VarTexture value) => value.Value;
    }
}