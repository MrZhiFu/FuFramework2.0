//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FuFramework.Core.Runtime;

namespace GameFrameX.Runtime
{
    /// <summary>
    /// System.Float 变量类。
    /// 优点：可以像正常 Float 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarFloat : Variable<float>
    {
        /// <summary>
        /// 初始化 System.Single 变量类的新实例。
        /// </summary>
        public VarFloat() { }

        /// <summary>
        /// 从 System.Single 到 System.Single 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarFloat(float value)
        {
            var varValue = ReferencePool.Acquire<VarFloat>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 System.Single 变量类到 System.Single 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator float(VarFloat value) => value.Value;
    }
}