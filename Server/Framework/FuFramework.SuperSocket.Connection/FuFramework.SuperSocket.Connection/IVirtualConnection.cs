using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Connection;

public interface IVirtualConnection : IConnection
{
	ValueTask<FlushResult> WritePipeDataAsync(Memory<byte> memory, CancellationToken cancellationToken);
}
