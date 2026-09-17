using System;
using System.Text;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 提供 StringBuilder 的缓存可重用实例
/// </summary>
public static class StringBuilderCache
{
	private const int MaxBuilderSize = 360;

	[ThreadStatic]
	private static StringBuilder _cachedInstance;

	/// <summary>
	/// 获取指定大小的 StringBuilder
	/// </summary>
	/// <param name="capacity">长度,默认为 16</param>
	/// <returns>StringBuilder 对象</returns>
	public static StringBuilder Acquire(int capacity = 16)
	{
		if (capacity <= 360)
		{
			StringBuilder cachedInstance = _cachedInstance;
			if (cachedInstance != null && capacity <= cachedInstance.Capacity)
			{
				_cachedInstance = null;
				cachedInstance.Clear();
				return cachedInstance;
			}
		}
		return new StringBuilder(capacity);
	}

	/// <summary>
	/// 如果指定的构建器不是太大，则将其放在缓存中
	/// </summary>
	/// <param name="sb">StringBuilder 对象</param>
	public static void Release(StringBuilder sb)
	{
		if (sb.Capacity <= 360)
		{
			_cachedInstance = sb;
		}
	}

	/// <summary>
	/// ToString（） 字符串生成器，将其释放到缓存中并返回结果字符串
	/// </summary>
	/// <param name="sb">StringBuilder 对象</param>
	/// <returns>返回其生成的字符串</returns>
	public static string GetStringAndRelease(StringBuilder sb)
	{
		string result = sb.ToString();
		Release(sb);
		return result;
	}
}
