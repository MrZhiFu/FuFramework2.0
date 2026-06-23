using System;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Primitives;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Server.Abstractions;

public interface IPackageHandlingScheduler<TPackageInfo>
{
	void Initialize(IPackageHandler<TPackageInfo> packageHandler, Func<IAppSession, PackageHandlingException<TPackageInfo>, ValueTask<bool>> errorHandler);

	ValueTask HandlePackage(IAppSession session, TPackageInfo package, CancellationToken cancellationToken);
}
