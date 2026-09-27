using System.Collections.Generic;
using System.Reflection;
using UtilityAOT = AOT.Framework.Core.Utility.UtilityAOT;

// ReSharper disable once CheckNamespace
using AOT.Framework.Core.Utility;
namespace FuFramework.Core.Editor
{
	/// <summary>
	/// 类型相关的实用函数。
	///     功能：
	///         1. 获取运行时或编辑器程序集中指定基类的所有子类的名称。
	/// </summary>
	public static class Type
	{
		/// 运行时程序集名称列表
		private static readonly string[] RuntimeAssemblyNames = CollectAssemblyNames(true);

		/// 运行时或编辑器程序集名称列表
		private static readonly string[] RuntimeOrEditorAssemblyNames = CollectAssemblyNames(false);

		/// <summary>
		/// 在运行时程序集中获取指定基类的所有子类的名称。
		/// </summary>
		/// <param name="typeBase">基类类型。</param>
		/// <returns>指定基类的所有子类的名称。</returns>
		public static string[] GetRuntimeTypeNames(System.Type typeBase)
		{
			return GetTypeNames(typeBase, RuntimeAssemblyNames);
		}

		/// <summary>
		/// 在运行时或编辑器程序集中获取指定基类的所有子类的名称。
		/// </summary>
		/// <param name="typeBase">基类类型。</param>
		/// <returns>指定基类的所有子类的名称。</returns>
		internal static string[] GetRuntimeOrEditorTypeNames(System.Type typeBase)
		{
			return GetTypeNames(typeBase, RuntimeOrEditorAssemblyNames);
		}

		/// <summary>
		/// 获取指定基类的所有子类的名称。
		/// </summary>
		/// <param name="typeBase"></param>
		/// <param name="assemblyNames"></param>
		/// <returns></returns>
		private static string[] GetTypeNames(System.Type typeBase, string[] assemblyNames)
		{
			var typeNames = new List<string>();
			foreach (var assemblyName in assemblyNames)
			{
				Assembly assembly;
				try
				{
					assembly = Assembly.Load(assemblyName);
				}
				catch
				{
					continue;
				}

				if (assembly == null) continue;

				var types = assembly.GetTypes();
				foreach (var type in types)
				{
					if (!type.IsClass || type.IsAbstract || !typeBase.IsAssignableFrom(type)) continue;
					typeNames.Add(type.FullName);
				}
			}

			typeNames.Sort();
			return typeNames.ToArray();
		}

		/// <summary>
		/// 收集当前已加载程序集的全名列表。
		/// </summary>
		/// <param name="excludeEditor">是否排除 Editor 程序集。</param>
		/// <returns>程序集全名列表。</returns>
		private static string[] CollectAssemblyNames(bool excludeEditor)
		{
			var assemblies = UtilityAOT.Assembly.GetAssemblies();
			var names      = new List<string>(assemblies.Length);
			foreach (var assembly in assemblies)
			{
				if (excludeEditor && assembly.FullName.Contains("Editor")) continue;
				names.Add(assembly.FullName);
			}

			return names.ToArray();
		}
	}
}