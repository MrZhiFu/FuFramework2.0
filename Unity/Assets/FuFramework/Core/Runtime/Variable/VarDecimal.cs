// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// System.Decimal 变量类。
    /// 优点：可以像正常Decimal变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarDecimal : Variable<decimal>
    {
        /// <summary>
        /// 初始化 System.Decimal 变量类的新实例。
        /// </summary>
        public VarDecimal() { }

        /// <summary>
        /// 从 System.Decimal 到 System.Decimal 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarDecimal(decimal value)
        {
            var varValue = ReferencePool.Acquire<VarDecimal>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Decimal 变量类到 System.Decimal 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator decimal(VarDecimal value) => value.Value;
    }
}