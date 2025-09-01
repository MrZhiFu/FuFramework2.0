using System;
using FuFramework.ReferencePool.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Variable.Runtime
{
    /// <summary>
    /// 变量。
    /// 实现了使用引用池来优化变量的内存分配和释放，并提供统一的接口来获取变量类型、值、值类型等。
    /// </summary>
    public abstract class Variable : IReference
    {
        /// <summary>
        /// 初始化变量的新实例。
        /// </summary>
        protected Variable() { }

        /// <summary>
        /// 获取变量类型。
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// 获取变量值。
        /// </summary>
        /// <returns>变量值。</returns>
        public abstract object GetValue();

        /// <summary>
        /// 设置变量值。
        /// </summary>
        /// <param name="value">变量值。</param>
        public abstract void SetValue(object value);

        /// <summary>
        /// 清理变量值。
        /// </summary>
        public abstract void Clear();
    }
}