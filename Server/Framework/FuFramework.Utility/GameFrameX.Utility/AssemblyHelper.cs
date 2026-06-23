using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FuFramework.Utility.Extensions;

namespace FuFramework.Utility;

/// <summary>
/// 程序集辅助器
/// </summary>
public static class AssemblyHelper
{
	private static readonly Assembly[] Assemblies;

	private static readonly Dictionary<string, Type> CachedTypes;

	static AssemblyHelper()
	{
		CachedTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
		Assemblies = AppDomain.CurrentDomain.GetAssemblies();
	}

	/// <summary>
	/// 获取已加载的程序集。
	/// </summary>
	/// <returns>已加载的程序集数组。</returns>
	public static Assembly[] GetAssemblies()
	{
		return Assemblies;
	}

	/// <summary>
	/// 获取已加载的程序集中的所有类型。
	/// </summary>
	/// <returns>已加载的程序集中的所有类型数组。</returns>
	public static Type[] GetTypes()
	{
		List<Type> list = new List<Type>();
		Assembly[] assemblies = Assemblies;
		foreach (Assembly assembly in assemblies)
		{
			list.AddRange(assembly.GetTypes());
		}
		return list.ToArray();
	}

	/// <summary>
	/// 获取已加载的程序集中的所有类型，并将结果添加到指定的列表中。
	/// </summary>
	/// <param name="results">用于存储结果的列表。</param>
	public static void GetTypes(List<Type> results)
	{
		if (results == null)
		{
			throw new Exception("Results is invalid.");
		}
		results.Clear();
		Assembly[] assemblies = Assemblies;
		foreach (Assembly assembly in assemblies)
		{
			results.AddRange(assembly.GetTypes());
		}
	}

	/// <summary>
	/// 获取已加载的程序集中的指定类型。
	/// </summary>
	/// <param name="typeName">要获取的类型名。</param>
	/// <returns>已加载的程序集中的指定类型，如果未找到则返回 null。</returns>
	public static Type GetType(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			throw new Exception("Type name is invalid.");
		}
		if (CachedTypes.TryGetValue(typeName, out var value))
		{
			return value;
		}
		value = Type.GetType(typeName);
		if (value != null)
		{
			CachedTypes.Add(typeName, value);
			return value;
		}
		Assembly[] assemblies = Assemblies;
		foreach (Assembly assembly in assemblies)
		{
			value = Type.GetType(typeName + ", " + assembly.FullName);
			if (value != null)
			{
				CachedTypes.Add(typeName, value);
				return value;
			}
		}
		return null;
	}

	/// <summary>
	/// 获取已加载的程序集中的指定类型的子类实例化列表。
	/// </summary>
	/// <typeparam name="T">指定类型。</typeparam>
	/// <returns>指定类型的子类实例化列表。</returns>
	public static List<T> GetRuntimeImplementTypeNamesInstance<T>()
	{
		List<Type> runtimeImplementTypeNames = GetRuntimeImplementTypeNames(typeof(T));
		List<T> list = new List<T>(runtimeImplementTypeNames.Count);
		foreach (Type item in runtimeImplementTypeNames)
		{
			list.Add((T)Activator.CreateInstance(item));
		}
		return list;
	}

	/// <summary>
	/// 获取已加载的程序集中的指定类型的子类列表。
	/// </summary>
	/// <typeparam name="T">指定类型。</typeparam>
	/// <returns>指定类型的子类列表。</returns>
	public static List<Type> GetRuntimeImplementTypeNames<T>()
	{
		return GetRuntimeImplementTypeNames(typeof(T));
	}

	/// <summary>
	/// 获取已加载的程序集中的指定类型的子类列表，并过滤出具有指定特性的类型。
	/// </summary>
	/// <typeparam name="T">指定类型。</typeparam>
	/// <typeparam name="TAttribute">指定自定义的特性标记。</typeparam>
	/// <returns>指定类型的子类列表，且这些类型具有指定的特性。</returns>
	public static List<Type> GetRuntimeImplementTypeNames<T, TAttribute>() where TAttribute : Attribute
	{
		return (from t in GetRuntimeImplementTypeNames(typeof(T))
			where t.GetCustomAttribute<TAttribute>() != null
			select t).ToList();
	}

	/// <summary>
	/// 获取已加载的程序集中的指定类型的子类列表，并返回它们的全名。
	/// </summary>
	/// <param name="type">指定类型。</param>
	/// <returns>指定类型的子类列表的全名。</returns>
	public static List<string> GetRuntimeTypeNames(Type type)
	{
		List<string> list = new List<string>();
		foreach (Type runtimeImplementTypeName in GetRuntimeImplementTypeNames(type))
		{
			list.Add(runtimeImplementTypeName.FullName);
		}
		return list;
	}

	/// <summary>
	/// 获取已加载的程序集中的指定类型的子类列表。
	/// </summary>
	/// <param name="type">指定类型。</param>
	/// <returns>指定类型的子类列表。</returns>
	public static List<Type> GetRuntimeImplementTypeNames(Type type)
	{
		Type[] types = GetTypes();
		List<Type> list = new List<Type>();
		Type[] array = types;
		foreach (Type type2 in array)
		{
			if (!type2.IsAbstract && type2.IsClass && (type2.IsSubclassOf(type) || type2.IsImplWithInterface(type)))
			{
				list.Add(type2);
			}
		}
		return list;
	}
}
