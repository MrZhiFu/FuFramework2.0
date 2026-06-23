using System;

namespace FuFramework.SuperSocket.Command;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class CommandFilterBaseAttribute : Attribute, ICommandFilter
{
	/// <summary>
	/// Gets or sets the execution order.
	/// </summary>
	/// <value>
	/// The order.
	/// </value>
	public int Order { get; set; }
}
