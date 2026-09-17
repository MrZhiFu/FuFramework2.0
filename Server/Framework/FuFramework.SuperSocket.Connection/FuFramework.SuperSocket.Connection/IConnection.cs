using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.ProtoBase.ProxyProtocol;

namespace FuFramework.SuperSocket.Connection;

public interface IConnection
{
	bool IsClosed { get; }

	EndPoint RemoteEndPoint { get; }

	EndPoint LocalEndPoint { get; }

	DateTimeOffset LastActiveTime { get; }

	CloseReason? CloseReason { get; }

	CancellationToken ConnectionToken { get; }

	ProxyInfo ProxyInfo { get; }

	event EventHandler<CloseEventArgs> Closed;

	IAsyncEnumerable<TPackageInfo> RunAsync<TPackageInfo>(IPipelineFilter<TPackageInfo> pipelineFilter);

	ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default(CancellationToken));

	ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default(CancellationToken));

	ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken = default(CancellationToken));

	ValueTask CloseAsync(CloseReason closeReason);

	ValueTask DetachAsync();
}
