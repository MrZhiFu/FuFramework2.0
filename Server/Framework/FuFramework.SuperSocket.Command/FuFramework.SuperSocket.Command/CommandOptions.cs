using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Represents options for configuring commands in a SuperSocket application.
/// </summary>
public class CommandOptions : ICommandSource
{
	private List<Type> _globalCommandFilterTypes;

	/// <summary>
	/// Gets or sets the assemblies containing commands.
	/// </summary>
	public CommandAssemblyConfig[] Assemblies { get; set; }

	/// <summary>
	/// Gets or sets the list of command sources.
	/// </summary>
	public List<ICommandSource> CommandSources { get; set; }

	/// <summary>
	/// Gets the list of global command filter types.
	/// </summary>
	public IReadOnlyList<Type> GlobalCommandFilterTypes => _globalCommandFilterTypes;

	internal object UnknownPackageHandler { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Command.CommandOptions" /> class.
	/// </summary>
	public CommandOptions()
	{
		CommandSources = new List<ICommandSource>();
		_globalCommandFilterTypes = new List<Type>();
	}

	/// <summary>
	/// Registers a handler for unknown packages.
	/// </summary>
	/// <typeparam name="TPackageInfo">The type of the package.</typeparam>
	/// <param name="unknownPackageHandler">The handler for unknown packages.</param>
	public void RegisterUnknownPackageHandler<TPackageInfo>(Func<IAppSession, TPackageInfo, CancellationToken, ValueTask> unknownPackageHandler)
	{
		UnknownPackageHandler = unknownPackageHandler;
	}

	/// <summary>
	/// Retrieves command types that match the specified criteria.
	/// </summary>
	/// <param name="criteria">The criteria to filter command types.</param>
	/// <returns>An enumerable collection of command types.</returns>
	public IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria)
	{
		List<ICommandSource> commandSources = CommandSources;
		CommandAssemblyConfig[] assemblies = Assemblies;
		if (assemblies != null && assemblies.Any())
		{
			commandSources.AddRange(assemblies);
		}
		List<Type> list = new List<Type>();
		foreach (ICommandSource item in commandSources)
		{
			list.AddRange(item.GetCommandTypes(criteria));
		}
		return list;
	}

	internal void AddGlobalCommandFilterType(Type commandFilterType)
	{
		_globalCommandFilterTypes.Add(commandFilterType);
	}
}
