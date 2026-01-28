// ReSharper disable once CheckNamespace

namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// System.Int64 变量类。
    /// 优点：可以像正常Int64变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarInt64 : Variable<long>
    {
        /// <summary>
        /// 初始化 System.Int64 变量类的新实例。
        /// </summary>
        public VarInt64() { }

        /// <summary>
        /// 从 System.Int64 到 System.Int64 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarInt64(long value)
        {
            var varValue = ReferencePool.Runtime.ReferencePool.Acquire<VarInt64>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Int64 变量类到 System.Int64 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator long(VarInt64 value) => value.Value;
    }
}