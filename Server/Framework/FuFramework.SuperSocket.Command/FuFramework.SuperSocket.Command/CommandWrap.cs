using System;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;

namespace FuFramework.SuperSocket.Command;

internal class CommandWrap<TAppSession, TPackageInfo, IPackageInterface, TCommand> : ICommand<TAppSession, TPackageInfo>, ICommand, ICommandWrap where TAppSession : IAppSession where TPackageInfo : IPackageInterface where TCommand : ICommand<TAppSession, IPackageInterface>
{
	public TCommand InnerCommand { get; }

	ICommand ICommandWrap.InnerCommand => InnerCommand;

	public CommandWrap(TCommand command)
	{
		InnerCommand = command;
	}

	public CommandWrap(IServiceProvider serviceProvider)
	{
		InnerCommand = (TCommand)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TCommand));
	}

	public void Execute(TAppSession session, TPackageInfo package)
	{
		InnerCommand.Execute(session, (IPackageInterface)(object)package);
	}
}
