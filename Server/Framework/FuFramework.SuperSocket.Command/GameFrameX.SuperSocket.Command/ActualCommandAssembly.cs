using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Represents an actual command assembly.
/// </summary>
public class ActualCommandAssembly : AssemblyBaseCommandSource, ICommandSource
{
	/// <summary>
	/// Gets or sets the assembly containing commands.
	/// </summary>
	public Assembly Assembly { get; set; }

	/// <summary>
	/// Retrieves command types from the assembly that match the specified criteria.
	/// </summary>
	/// <param name="criteria">The criteria to filter command types.</param>
	/// <returns>An enumerable collection of command types.</returns>
	public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
	{
		return from t in GetCommandTypesFromAssembly(Assembly)
			where criteria(t)
			select t;
	}
}
