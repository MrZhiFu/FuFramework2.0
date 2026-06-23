using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Represents a configuration for a command assembly.
/// </summary>
public class CommandAssemblyConfig : AssemblyBaseCommandSource, ICommandSource
{
	/// <summary>
	/// Gets or sets the name of the assembly.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Retrieves command types from the assembly that match the specified criteria.
	/// </summary>
	/// <param name="criteria">The criteria to filter command types.</param>
	/// <returns>An enumerable collection of command types.</returns>
	public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
	{
		return from t in GetCommandTypesFromAssembly(Assembly.Load(Name))
			where criteria(t)
			select t;
	}
}
