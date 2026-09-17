using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Command;

public interface IAsyncCommand<TPackageInfo> : IAsyncCommand<IAppSession, TPackageInfo>, ICommand
{
}
public interface IAsyncCommand<TAppSession, TPackageInfo> : ICommand where TAppSession : IAppSession
{
	ValueTask ExecuteAsync(TAppSession session, TPackageInfo package, CancellationToken cancellationToken);
}
