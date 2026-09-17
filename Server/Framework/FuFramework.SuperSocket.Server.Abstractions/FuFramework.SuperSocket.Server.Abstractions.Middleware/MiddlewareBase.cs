using System.Threading.Tasks;
using FuFramework.SuperSocket.Server.Abstractions.Session;

namespace FuFramework.SuperSocket.Server.Abstractions.Middleware;

public abstract class MiddlewareBase : IMiddleware
{
	public int Order { get; protected set; }

	public virtual void Start(IServer server)
	{
	}

	public virtual void Shutdown(IServer server)
	{
	}

	public virtual ValueTask<bool> RegisterSession(IAppSession session)
	{
		return new ValueTask<bool>(result: true);
	}

	public virtual ValueTask<bool> UnRegisterSession(IAppSession session)
	{
		return new ValueTask<bool>(result: true);
	}
}
