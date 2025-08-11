// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// System.Double 变量类。
    /// 优点：可以像正常Double变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarDouble : Variable<double>
    {
        /// <summary>
        /// 初始化 System.Double 变量类的新实例。
        /// </summary>
        public VarDouble() { }

        /// <summary>
        /// 从 System.Double 到 System.Double 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarDouble(double value)
        {
            var varValue = ReferencePool.Acquire<VarDouble>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Double 变量类到 System.Double 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator double(VarDouble value) => value.Value;
    }
}