// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Object 变量类。
    /// 功能：
    ///     1. 可以像正常Object变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarObject : GenericVariable<object>
    {
        /// <summary>
        /// 初始化 VarObject 变量类的新实例。
        /// </summary>
        public VarObject() { }
    }
}
