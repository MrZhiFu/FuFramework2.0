using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.Connection;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.Server.Abstractions.Session;

public interface IAppSession : IGameAppSession
{
	DateTimeOffset StartTime { get; }

	DateTimeOffset LastActiveTime { get; }

	IConnection Connection { get; }

	EndPoint RemoteEndPoint { get; }

	EndPoint LocalEndPoint { get; }

	IServerInfo Server { get; }

	object DataContext { get; set; }

	object this[object name] { get; set; }

	SessionState State { get; }

	event AsyncEventHandler Connected;

	event AsyncEventHandler<CloseEventArgs> Closed;

	ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default(CancellationToken));

	ValueTask CloseAsync(CloseReason reason);

	void Initialize(IServerInfo server, IConnection connection);

	void Reset();
}
