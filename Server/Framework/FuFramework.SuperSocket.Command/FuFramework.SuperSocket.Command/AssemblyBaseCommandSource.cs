using System;
using System.Collections.Generic;
using System.Reflection;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Represents a base class for retrieving command types from an assembly.
/// </summary>
public abstract class AssemblyBaseCommandSource
{
	/// <summary>
	/// Retrieves all exported types from the specified assembly.
	/// </summary>
	/// <param name="assembly">The assembly to retrieve types from.</param>
	/// <returns>An enumerable collection of exported types.</returns>
	public IEnumerable<Type> GetCommandTypesFromAssembly(Assembly assembly)
	{
		return assembly.GetExportedTypes();
	}
}
