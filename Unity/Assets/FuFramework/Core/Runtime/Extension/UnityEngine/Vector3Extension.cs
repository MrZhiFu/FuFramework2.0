using UnityEngine;

namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// <see cref="Vector3" /> 类的扩展方法。
    /// </summary>
    public static class Vector3Extension
    {
        /// <summary>
        /// 取 <see cref="Vector3" /> 的 (x, y, z) 转换为 <see cref="Vector2" /> 的 (x, z)。
        /// </summary>
        /// <param name="vector3">要转换的 Vector3。</param>
        /// <returns>转换后的 Vector2。</returns>
        public static Vector2 ToVector2(this global::UnityEngine.Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        /// <summary>
        /// 取 <see cref="Vector2" /> 的 (x, y) 转换为 <see cref="Vector3" /> 的 (x, 0, y)。
        /// </summary>
        /// <param name="vector3">要转换的 Vector3。</param>
        /// <returns>转换后的 Vector3。</returns>
        public static global::UnityEngine.Vector3 ToVector3(this Vector3Int vector3)
        {
            return new global::UnityEngine.Vector3(vector3.x, vector3.y, vector3.z);
        }
    }
}