//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using FuFramework.Core.Runtime;
using UnityEngine;

namespace GameFrameX.Runtime
{
    /// <summary>
    /// UnityEngine.Color32 变量类。
    /// 优点：可以像正常Color变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarColor32 : Variable<Color32>
    {
        /// <summary>
        /// 初始化 UnityEngine.Color32 变量类的新实例。
        /// </summary>
        public VarColor32() { }

        /// <summary>
        /// 从 UnityEngine.Color32 到 UnityEngine.Color32 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarColor32(Color32 value)
        {
            var varValue = ReferencePool.Acquire<VarColor32>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Color32 变量类到 UnityEngine.Color32 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Color32(VarColor32 value) => value.Value;
    }
}