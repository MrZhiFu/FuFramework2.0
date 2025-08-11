// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// Byte 变量类。
    /// 优点：可以像正常Byte值类型一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarByte : Variable<byte>
    {
        /// <summary>
        /// 初始化 Byte 变量类的新实例。
        /// </summary>
        public VarByte() { }

        /// <summary>
        /// 从 System.Byte 到 Byte 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarByte(byte value)
        {
            var varValue = ReferencePool.Acquire<VarByte>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Byte 变量类到 Byte 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator byte(VarByte value) => value.Value;
    }
}