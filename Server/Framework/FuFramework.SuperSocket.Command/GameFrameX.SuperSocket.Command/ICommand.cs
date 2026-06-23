using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Command;

public interface ICommand
{
}
public interface ICommand<TPackageInfo> : ICommand<IAppSession, TPackageInfo>, ICommand
{
}
public interface ICommand<TAppSession, TPackageInfo> : ICommand where TAppSession : IAppSession
{
	void Execute(TAppSession session, TPackageInfo package);
}
