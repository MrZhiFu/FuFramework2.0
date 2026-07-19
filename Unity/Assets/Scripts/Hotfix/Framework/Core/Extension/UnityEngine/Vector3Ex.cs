using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// Vector3 相关的扩展方法。
    /// </summary>
    public static class Vector3Ex
    {
        /// <summary>
        /// 取 Vector3 的 (x, y, z) 转换为 Vector2 的 (x, z)。
        /// </summary>
        /// <param name="vector3">要转换的 Vector3。</param>
        /// <returns>转换后的 Vector2。</returns>
        public static Vector2 ToVector2(this Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        /// <summary>
        /// 取 Vector3Int 的 (x, y, z) 转换为 Vector3 的 (x, y, z)。
        /// </summary>
        /// <param name="vector3">要转换的 Vector3Int。</param>
        /// <returns>转换后的 Vector3。</returns>
        public static Vector3 ToVector3(this Vector3Int vector3)
        {
            return new Vector3(vector3.x, vector3.y, vector3.z);
        }
    }
}
