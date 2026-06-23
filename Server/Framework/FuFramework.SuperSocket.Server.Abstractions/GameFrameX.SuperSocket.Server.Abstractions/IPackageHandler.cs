using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Server.Abstractions;

public interface IPackageHandler<TReceivePackageInfo>
{
	ValueTask Handle(IAppSession session, TReceivePackageInfo package, CancellationToken cancellationToken);
}
