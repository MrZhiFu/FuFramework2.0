using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FuFramework.SuperSocket.ClientEngine;

public static class Extensions
{
	private static Random m_Random = new Random();

	public static int IndexOf<T>(this IList<T> source, T target, int pos, int length) where T : IEquatable<T>
	{
		for (int i = pos; i < pos + length; i++)
		{
			if (source[i].Equals(target))
			{
				return i;
			}
		}
		return -1;
	}

	public static int? SearchMark<T>(this IList<T> source, T[] mark) where T : IEquatable<T>
	{
		return source.SearchMark(0, source.Count, mark, 0);
	}

	public static int? SearchMark<T>(this IList<T> source, int offset, int length, T[] mark) where T : IEquatable<T>
	{
		return source.SearchMark(offset, length, mark, 0);
	}

	public static int? SearchMark<T>(this IList<T> source, int offset, int length, T[] mark, int matched) where T : IEquatable<T>
	{
		int num = offset;
		int num2 = offset + length - 1;
		int num3 = matched;
		T val2;
		if (matched > 0)
		{
			for (int i = num3; i < mark.Length; i++)
			{
				T val = source[num++];
				ref T reference = ref val;
				val2 = default(T);
				if (val2 == null)
				{
					val2 = reference;
					reference = ref val2;
				}
				if (!reference.Equals(mark[i]))
				{
					break;
				}
				num3++;
				if (num > num2)
				{
					if (num3 == mark.Length)
					{
						return offset;
					}
					return -num3;
				}
			}
			if (num3 == mark.Length)
			{
				return offset;
			}
			num = offset;
			num3 = 0;
		}
		while (true)
		{
			num = source.IndexOf(mark[num3], num, length - num + offset);
			if (num >= 0)
			{
				num3++;
				for (int j = num3; j < mark.Length; j++)
				{
					int num4 = num + j;
					if (num4 <= num2)
					{
						T val3 = source[num4];
						ref T reference2 = ref val3;
						val2 = default(T);
						if (val2 == null)
						{
							val2 = reference2;
							reference2 = ref val2;
						}
						if (!reference2.Equals(mark[j]))
						{
							break;
						}
						num3++;
						continue;
					}
					return -num3;
				}
				if (num3 == mark.Length)
				{
					break;
				}
				num++;
				num3 = 0;
				continue;
			}
			return null;
		}
		return num;
	}

	public static int SearchMark<T>(this IList<T> source, int offset, int length, SearchMarkState<T> searchState) where T : IEquatable<T>
	{
		int? num = source.SearchMark(offset, length, searchState.Mark, searchState.Matched);
		if (!num.HasValue)
		{
			searchState.Matched = 0;
			return -1;
		}
		if (num.Value < 0)
		{
			searchState.Matched = -num.Value;
			return -1;
		}
		searchState.Matched = 0;
		return num.Value;
	}

	public static int StartsWith<T>(this IList<T> source, T[] mark) where T : IEquatable<T>
	{
		return source.StartsWith(0, source.Count, mark);
	}

	public static int StartsWith<T>(this IList<T> source, int offset, int length, T[] mark) where T : IEquatable<T>
	{
		int num = offset + length - 1;
		for (int i = 0; i < mark.Length; i++)
		{
			int num2 = offset + i;
			if (num2 > num)
			{
				return i;
			}
			T val = source[num2];
			ref T reference = ref val;
			T val2 = default(T);
			if (val2 == null)
			{
				val2 = reference;
				reference = ref val2;
			}
			if (!reference.Equals(mark[i]))
			{
				return -1;
			}
		}
		return mark.Length;
	}

	public static bool EndsWith<T>(this IList<T> source, T[] mark) where T : IEquatable<T>
	{
		return source.EndsWith(0, source.Count, mark);
	}

	public static bool EndsWith<T>(this IList<T> source, int offset, int length, T[] mark) where T : IEquatable<T>
	{
		if (mark.Length > length)
		{
			return false;
		}
		for (int i = 0; i < Math.Min(length, mark.Length); i++)
		{
			T val = mark[i];
			if (val == null)
			{
				val = default(T);
			}
			if (!val.Equals(source[offset + length - mark.Length + i]))
			{
				return false;
			}
		}
		return true;
	}

	public static T[] CloneRange<T>(this T[] source, int offset, int length)
	{
		T[] array = new T[length];
		Array.Copy(source, offset, array, 0, length);
		return array;
	}

	public static T[] RandomOrder<T>(this T[] source)
	{
		int num = source.Length / 2;
		for (int i = 0; i < num; i++)
		{
			int num2 = m_Random.Next(0, source.Length - 1);
			int num3 = m_Random.Next(0, source.Length - 1);
			if (num2 != num3)
			{
				T val = source[num3];
				source[num3] = source[num2];
				source[num2] = val;
			}
		}
		return source;
	}

	public static string GetValue(this NameValueCollection collection, string key)
	{
		return collection.GetValue(key, string.Empty);
	}

	public static string GetValue(this NameValueCollection collection, string key, string defaultValue)
	{
		if (string.IsNullOrEmpty(key))
		{
			throw new ArgumentNullException("key");
		}
		if (collection == null)
		{
			return defaultValue;
		}
		string text = collection[key];
		if (text == null)
		{
			return defaultValue;
		}
		return text;
	}
}
