using Hotfix.Framework.Core;
// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 String 变量类。
    /// 功能：
    ///     1. 可以像正常 String 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarString : GenericVariable<string>
    {
        /// <summary>
        /// 初始化 VarString 变量类的新实例。
        /// </summary>
        public VarString() { }

        /// <summary>
        /// 从 System.String 到 VarString 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarString(string value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarString>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarString 变量类到 System.String 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator string(VarString value) => value.Value;
    }
}
