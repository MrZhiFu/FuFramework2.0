// ReSharper disable once CheckNamespace

namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// System.Object 变量类。
    /// 优点：可以像正常Object变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarObject : Variable<object>
    {
        /// <summary>
        /// 初始化 System.Object 变量类的新实例。
        /// </summary>
        public VarObject() { }
    }
}