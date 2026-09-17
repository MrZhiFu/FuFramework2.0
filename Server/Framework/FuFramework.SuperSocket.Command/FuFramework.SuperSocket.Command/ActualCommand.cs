using System;
using System.Collections.Generic;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Represents an actual command with a specific type.
/// </summary>
public class ActualCommand : ICommandSource
{
	/// <summary>
	/// Gets or sets the type of the command.
	/// </summary>
	public Type CommandType { get; set; }

	/// <summary>
	/// Retrieves the command type if it matches the specified criteria.
	/// </summary>
	/// <param name="criteria">The criteria to filter command types.</param>
	/// <returns>An enumerable collection containing the command type if it matches the criteria.</returns>
	public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
	{
		if (criteria(CommandType))
		{
			yield return CommandType;
		}
	}
}
