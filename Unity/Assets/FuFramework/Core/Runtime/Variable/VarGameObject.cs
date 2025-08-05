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
    /// UnityEngine.GameObject 变量类。
    /// 优点：可以像正常GameObject变量一样使用，且底层使用引用池优化了内存。
    /// </summary>
    public sealed class VarGameObject : Variable<GameObject>
    {
        /// <summary>
        /// 初始化 UnityEngine.GameObject 变量类的新实例。
        /// </summary>
        public VarGameObject() { }

        /// <summary>
        /// 从 UnityEngine.GameObject 到 UnityEngine.GameObject 变量类的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator VarGameObject(GameObject value)
        {
            var varValue = ReferencePool.Acquire<VarGameObject>();
            varValue.Value = value;
            return varValue;
        }

        /// <summary>
        /// 从 UnityEngine.GameObject 变量类到 UnityEngine.GameObject 的隐式转换。
        /// </summary>
        /// <param name="value">值。</param>
        public static implicit operator GameObject(VarGameObject value) => value.Value;
    }
}