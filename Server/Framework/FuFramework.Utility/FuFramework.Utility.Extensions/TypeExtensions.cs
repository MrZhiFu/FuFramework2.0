using System;

namespace FuFramework.Utility.Extensions;

/// <summary>
/// 提供对 <see cref="T:System.Type" /> 类型的扩展方法。
/// </summary>
public static class TypeExtensions
{
	/// <summary>
	/// 判断类型是否实现了指定的接口。
	/// </summary>
	/// <param name="self">要判断的类型。</param>
	/// <param name="target">要判断的接口类型。</param>
	/// <returns>如果类型实现了指定的接口且不是接口类型或抽象类型，则为 true；否则为 false。</returns>
	public static bool IsImplWithInterface(this Type self, Type target)
	{
		if (target == null)
		{
			return false;
		}
		if (!self.IsInterface && !self.IsAbstract && target.FullName != null)
		{
			return self.GetInterface(target.FullName) != null;
		}
		return false;
	}
}
