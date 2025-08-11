// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// System.UInt32 变量类。
    /// 优点：可以像正常 UInt32 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarUInt32 : Variable<uint>
    {
        /// <summary>
        /// 初始化 System.UInt32 变量类的新实例。
        /// </summary>
        public VarUInt32() { }

        /// <summary>
        /// 从 System.UInt32 到 System.UInt32 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarUInt32(uint value)
        {
            var varValue = ReferencePool.Acquire<VarUInt32>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.UInt32 变量类到 System.UInt32 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator uint(VarUInt32 value) => value.Value;
    }
}