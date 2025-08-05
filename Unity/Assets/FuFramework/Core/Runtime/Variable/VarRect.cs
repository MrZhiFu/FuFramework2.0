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
    /// UnityEngine.Rect 变量类。
    /// 优点：可以像正常 Rect 变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarRect : Variable<Rect>
    {
        /// <summary>
        /// 初始化 UnityEngine.Rect 变量类的新实例。
        /// </summary>
        public VarRect() { }

        /// <summary>
        /// 从 UnityEngine.Rect 到 UnityEngine.Rect 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarRect(Rect value)
        {
            var varValue = ReferencePool.Acquire<VarRect>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.Rect 变量类到 UnityEngine.Rect 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator Rect(VarRect value) => value.Value;
    }
}