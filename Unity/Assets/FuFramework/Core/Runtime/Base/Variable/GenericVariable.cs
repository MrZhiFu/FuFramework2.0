using System;

// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 通用变量。
    /// </summary>
    /// <typeparam name="T">变量类型。</typeparam>
    public abstract class Variable<T> : Variable
    {
        /// <summary>
        /// 初始化变量的新实例。
        /// </summary>
        public Variable() => Value = default;

        /// <summary>
        /// 获取变量类型。
        /// </summary>
        public override Type Type => typeof(T);

        /// <summary>
        /// 获取或设置变量值。
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// 获取变量值。
        /// </summary>
        /// <returns>变量值。</returns>
        public override object GetValue() => Value;

        /// <summary>
        /// 设置变量值。
        /// </summary>
        /// <param name="value">变量值。</param>
        public override void SetValue(object value) => Value = (T)value;

        /// <summary>
        /// 清理变量值。
        /// </summary>
        public override void Clear() => Value = default;

        /// <summary>
        /// 获取变量字符串。
        /// </summary>
        /// <returns>变量字符串。</returns>
        public override string ToString() => Value != null ? Value.ToString() : "<Null>";
    }
}