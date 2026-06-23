using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FuFramework.Core.Config;

/// <summary>
/// 提供字符串处理的静态方法。
/// </summary>
public static class StringUtil
{
	/// <summary>
	/// 将对象转换为字符串表示形式。
	/// </summary>
	/// <param name="o">要转换的对象。</param>
	/// <returns>对象的字符串表示形式。</returns>
	public static string ToStr(object o)
	{
		return ToStr(o, new StringBuilder());
	}

	/// <summary>
	/// 将对象转换为字符串表示形式，并使用提供的StringBuilder进行构建。
	/// </summary>
	/// <param name="o">要转换的对象。</param>
	/// <param name="sb">用于构建字符串的StringBuilder。</param>
	/// <returns>对象的字符串表示形式。</returns>
	public static string ToStr(object o, StringBuilder sb)
	{
		FieldInfo[] fields = o.GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			StringBuilder stringBuilder = sb;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder);
			handler.AppendFormatted(fieldInfo.Name);
			handler.AppendLiteral(" = ");
			handler.AppendFormatted<object>(fieldInfo.GetValue(o));
			handler.AppendLiteral(",");
			stringBuilder2.Append(ref handler);
		}
		PropertyInfo[] properties = o.GetType().GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			StringBuilder stringBuilder = sb;
			StringBuilder stringBuilder3 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder);
			handler.AppendFormatted(propertyInfo.Name);
			handler.AppendLiteral(" = ");
			handler.AppendFormatted<object>(propertyInfo.GetValue(o));
			handler.AppendLiteral(",");
			stringBuilder3.Append(ref handler);
		}
		return sb.ToString();
	}

	/// <summary>
	/// 将数组转换为字符串表示形式。
	/// </summary>
	/// <param name="arr">要转换的数组。</param>
	/// <typeparam name="T">数组元素的类型。</typeparam>
	/// <returns>数组的字符串表示形式。</returns>
	public static string ArrayToString<T>(T[] arr)
	{
		return "[" + string.Join(",", arr) + "]";
	}

	/// <summary>
	/// 将集合转换为字符串表示形式。
	/// </summary>
	/// <param name="arr">要转换的集合。</param>
	/// <typeparam name="T">集合元素的类型。</typeparam>
	/// <returns>集合的字符串表示形式。</returns>
	public static string CollectionToString<T>(IEnumerable<T> arr)
	{
		return "[" + string.Join(",", arr) + "]";
	}

	/// <summary>
	/// 将字典转换为字符串表示形式。
	/// </summary>
	/// <param name="dic">要转换的字典。</param>
	/// <typeparam name="TK">字典键的类型。</typeparam>
	/// <typeparam name="TV">字典值的类型。</typeparam>
	/// <returns>字典的字符串表示形式。</returns>
	public static string CollectionToString<TK, TV>(IDictionary<TK, TV> dic)
	{
		StringBuilder stringBuilder = new StringBuilder(123);
		foreach (KeyValuePair<TK, TV> item in dic)
		{
			stringBuilder.Append(item.Key).Append(':');
			stringBuilder.Append(item.Value).Append(',');
		}
		stringBuilder.Append('}');
		return stringBuilder.ToString();
	}
}
