using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.ProtoBase.ProxyProtocol;

namespace FuFramework.SuperSocket.Connection;

public abstract class ConnectionBase : IConnection
{
	public bool IsClosed { get; private set; }

	public EndPoint RemoteEndPoint { get; protected set; }

	public EndPoint LocalEndPoint { get; protected set; }

	public CloseReason? CloseReason { get; protected set; }

	public DateTimeOffset LastActiveTime { get; protected set; } = DateTimeOffset.Now;

	public CancellationToken ConnectionToken { get; protected set; }

	public ProxyInfo ProxyInfo { get; protected set; }

	public event EventHandler<CloseEventArgs> Closed;

	public abstract IAsyncEnumerable<TPackageInfo> RunAsync<TPackageInfo>(IPipelineFilter<TPackageInfo> pipelineFilter);

	public abstract ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken));

	public abstract ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default(CancellationToken));

	public abstract ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken = default(CancellationToken));

	protected virtual void OnClosed()
	{
		IsClosed = true;
		EventHandler<CloseEventArgs> eventHandler = this.Closed;
		if (eventHandler != null && !(Interlocked.CompareExchange(ref this.Closed, null, eventHandler) != eventHandler))
		{
			CloseReason reason = (CloseReason.HasValue ? CloseReason.Value : FuFramework.SuperSocket.Connection.CloseReason.Unknown);
			eventHandler(this, new CloseEventArgs(reason));
		}
	}

	public abstract ValueTask CloseAsync(CloseReason closeReason);

	public abstract ValueTask DetachAsync();
}
