using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public interface ISessionEventHost
{
	ValueTask HandleSessionConnectedEvent(IAppSession session);

	ValueTask HandleSessionClosedEvent(IAppSession session, CloseReason reason);
}
