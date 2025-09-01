// ReSharper disable once CheckNamespace

namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// System.UInt64 变量类。
    /// 优点：可以像正常 UInt64 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarUInt64 : Variable<ulong>
    {
        /// <summary>
        /// 初始化 System.UInt64 变量类的新实例。
        /// </summary>
        public VarUInt64() { }

        /// <summary>
        /// 从 System.UInt64 到 System.UInt64 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarUInt64(ulong value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarUInt64>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.UInt64 变量类到 System.UInt64 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator ulong(VarUInt64 value) => value.Value;
    }
}