using System;

namespace Hotfix.Framework.Asset
{
    /// <summary>
    /// 加载缓存键：资源路径 + 加载类型。
    /// 同一路径以不同类型加载时视为不同条目，避免不同型 LoadAsync 相互驱逐/误释放共享句柄
    /// （否则 LoadAsync&lt;T1&gt;(path) 缓存句柄会被 LoadAsync&lt;T2&gt;(path) 的类型不匹配分支驱逐卸载）。
    /// </summary>
    public readonly struct LoadKey : IEquatable<LoadKey>
    {
        /// <summary>
        /// 资源路径。
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// 加载类型（null 表示不指定类型）。
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// 构造缓存键。
        /// </summary>
        /// <param name="path">资源路径。</param>
        /// <param name="type">加载类型。</param>
        public LoadKey(string path, Type type)
        {
            Path = path;
            Type = type;
        }

        /// <summary>
        /// 判断与另一缓存键是否相等（路径与类型均相同）。
        /// </summary>
        /// <param name="other">另一缓存键。</param>
        /// <returns>是否相等。</returns>
        public bool Equals(LoadKey other) => Path == other.Path && Type == other.Type;

        /// <summary>
        /// 判断与对象是否相等。
        /// </summary>
        /// <param name="obj">比较对象。</param>
        /// <returns>是否相等。</returns>
        public override bool Equals(object obj) => obj is LoadKey other && Equals(other);

        /// <summary>
        /// 计算哈希值（组合路径与类型哈希）。
        /// </summary>
        /// <returns>哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Path             != null ? Path.GetHashCode() : 0;
                hash = (hash * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                return hash;
            }
        }
    }
}