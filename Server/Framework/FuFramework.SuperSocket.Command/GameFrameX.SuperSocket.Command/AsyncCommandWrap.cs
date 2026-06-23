using System;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;

namespace FuFramework.SuperSocket.Command;

internal class AsyncCommandWrap<TAppSession, TPackageInfo, IPackageInterface, TAsyncCommand> : IAsyncCommand<TAppSession, TPackageInfo>, ICommand, ICommandWrap where TAppSession : IAppSession where TPackageInfo : IPackageInterface where TAsyncCommand : IAsyncCommand<TAppSession, IPackageInterface>
{
	public TAsyncCommand InnerCommand { get; }

	ICommand ICommandWrap.InnerCommand => InnerCommand;

	public AsyncCommandWrap(TAsyncCommand command)
	{
		InnerCommand = command;
	}

	public AsyncCommandWrap(IServiceProvider serviceProvider)
	{
		InnerCommand = (TAsyncCommand)ActivatorUtilities.CreateInstance(serviceProvider, typeof(TAsyncCommand));
	}

	public async ValueTask ExecuteAsync(TAppSession session, TPackageInfo package, CancellationToken cancellationToken)
	{
		await InnerCommand.ExecuteAsync(session, (IPackageInterface)(object)package, cancellationToken);
	}
}
