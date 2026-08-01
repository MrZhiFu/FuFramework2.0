using Hotfix.Framework.Core;
﻿// ReSharper disable once CheckNamespace

namespace Hotfix.Framework.Variable
{
    /// <summary>
    /// 自定义 Int32 变量类。
    /// 功能：
    ///     1. 可以像正常Int32变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarInt32 : GenericVariable<int>
    {
        /// <summary>
        /// 初始化 VarInt32 变量类的新实例。
        /// </summary>
        public VarInt32() { }

        /// <summary>
        /// 从 System.Int32 到 VarInt32 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarInt32(int value)
        {
            var varValue = GlobalModule.ReferencePoolModule.Acquire<VarInt32>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 VarInt32 变量类到 System.Int32 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator int(VarInt32 value) => value.Value;
    }
}
