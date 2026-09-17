using System;

namespace FuFramework.SuperSocket.Command;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class CommandFilterAttribute : CommandFilterBaseAttribute
{
	/// <summary>
	/// Called when [command executing].
	/// </summary>
	/// <param name="commandContext">The command context.</param>
	public abstract bool OnCommandExecuting(CommandExecutingContext commandContext);

	/// <summary>
	/// Called when [command executed].
	/// </summary>
	/// <param name="commandContext">The command context.</param>
	public abstract void OnCommandExecuted(CommandExecutingContext commandContext);
}
