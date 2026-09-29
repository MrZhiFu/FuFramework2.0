using System;
using Hotfix.Game.Config;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.RedDot
{
	/// <summary>
	/// 红点节点统一标识符
	/// 支持隐式转换：ERedDotKey 枚举 → RedDotKey，string → RedDotKey
	/// 内部统一为 string 存储（枚举 ToString() 或动态字符串），可直接用作 Dictionary/HashSet 的 Key
	/// </summary>
	public readonly struct RedDotKey : IEquatable<RedDotKey>
	{
		/// <summary>
		/// 非枚举 Key 的默认值标记
		/// </summary>
		private const int INVALID_ENUM_VALUE = -1;

		/// <summary>
		/// 内部字符串值（枚举名或动态字符串）
		/// </summary>
		private readonly string m_key;

		/// <summary>
		/// 枚举 int 值（仅由 ERedDotKey 隐式转换时设置，-1 表示非枚举）
		/// </summary>
		private readonly int m_enumValue;

		private RedDotKey(string key, int enumValue)
		{
			m_key       = key ?? "";
			m_enumValue = enumValue;
		}

		/// <summary>
		/// 隐式转换：ERedDotKey 枚举 → RedDotKey（如 ERedDotKey.Mail → "Mail"）
		/// </summary>
		/// <param name="key">ERedDotKey 枚举值</param>
		/// <returns>对应的 RedDotKey</returns>
		public static implicit operator RedDotKey(ERedDotKey key) => new(key.ToString(), (int)key);

		/// <summary>
		/// 隐式转换：string → RedDotKey
		/// </summary>
		/// <param name="key">字符串 Key</param>
		/// <returns>对应的 RedDotKey</returns>
		public static implicit operator RedDotKey(string key) => new(key, INVALID_ENUM_VALUE);

		/// <summary>
		/// 尝试获取枚举 int 值
		/// </summary>
		/// <param name="value">枚举 int 值输出</param>
		/// <returns>是否为枚举 Key</returns>
		public bool TryGetEnumValue(out int value)
		{
			value = m_enumValue;
			return m_enumValue != INVALID_ENUM_VALUE;
		}

		public override string ToString() => m_key ?? "";

		public bool Equals(RedDotKey other) => string.Equals(m_key, other.m_key);

		public override bool Equals(object obj) => obj is RedDotKey other && Equals(other);

		public override int GetHashCode() => m_key?.GetHashCode() ?? 0;

		public static bool operator ==(RedDotKey left, RedDotKey right) => left.Equals(right);

		public static bool operator !=(RedDotKey left, RedDotKey right) => !left.Equals(right);
	}
}
