using System;
using System.Reflection;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Provides extension methods for configuring command options.
/// </summary>
public static class CommandOptionsExtensions
{
	/// <summary>
	/// Adds a command to the command options.
	/// </summary>
	/// <typeparam name="TCommand">The type of the command to add.</typeparam>
	/// <param name="commandOptions">The command options to configure.</param>
	public static void AddCommand<TCommand>(this CommandOptions commandOptions)
	{
		commandOptions.CommandSources.Add(new ActualCommand
		{
			CommandType = typeof(TCommand)
		});
	}

	/// <summary>
	/// Adds a command to the command options.
	/// </summary>
	/// <param name="commandOptions">The command options to configure.</param>
	/// <param name="commandType">The type of the command to add.</param>
	public static void AddCommand(this CommandOptions commandOptions, Type commandType)
	{
		commandOptions.CommandSources.Add(new ActualCommand
		{
			CommandType = commandType
		});
	}

	/// <summary>
	/// Adds a command assembly to the command options.
	/// </summary>
	/// <param name="commandOptions">The command options to configure.</param>
	/// <param name="commandAssembly">The assembly containing commands.</param>
	public static void AddCommandAssembly(this CommandOptions commandOptions, Assembly commandAssembly)
	{
		commandOptions.CommandSources.Add(new ActualCommandAssembly
		{
			Assembly = commandAssembly
		});
	}

	/// <summary>
	/// Adds a global command filter to the command options.
	/// </summary>
	/// <typeparam name="TCommandFilter">The type of the command filter to add.</typeparam>
	/// <param name="commandOptions">The command options to configure.</param>
	public static void AddGlobalCommandFilter<TCommandFilter>(this CommandOptions commandOptions) where TCommandFilter : CommandFilterBaseAttribute
	{
		commandOptions.AddGlobalCommandFilterType(typeof(TCommandFilter));
	}

	/// <summary>
	/// Adds a global command filter to the command options.
	/// </summary>
	/// <param name="commandOptions">The command options to configure.</param>
	/// <param name="commandFilterType">The type of the command filter to add.</param>
	/// <exception cref="T:System.Exception">Thrown if the command filter type does not inherit from <see cref="T:FuFramework.SuperSocket.Command.CommandFilterBaseAttribute" />.</exception>
	public static void AddGlobalCommandFilter(this CommandOptions commandOptions, Type commandFilterType)
	{
		if (!typeof(CommandFilterBaseAttribute).IsAssignableFrom(commandFilterType))
		{
			throw new Exception("The command filter type must inherit CommandFilterBaseAttribute.");
		}
		commandOptions.AddGlobalCommandFilterType(commandFilterType);
	}
}
