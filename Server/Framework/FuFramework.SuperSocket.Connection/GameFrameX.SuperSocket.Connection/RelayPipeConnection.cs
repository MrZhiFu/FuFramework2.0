using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Connection;

public class RelayPipeConnection : PipeConnection
{
	private static ConnectionOptions RebuildOptionsWithPipes(ConnectionOptions options, Pipe pipeIn, Pipe pipeOut)
	{
		options.Input = pipeIn;
		options.Output = pipeOut;
		return options;
	}

	public RelayPipeConnection(ConnectionOptions options, Pipe pipeIn, Pipe pipeOut)
		: base(RebuildOptionsWithPipes(options, pipeIn, pipeOut))
	{
	}

	protected override void Close()
	{
		base.Input.Writer.Complete();
		base.Output.Writer.Complete();
	}

	protected override async ValueTask<int> SendOverIoAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
	{
		PipeWriter writer = base.OutputWriter;
		int total = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> data = enumerator.Current;
			FlushResult flushResult = await writer.WriteAsync(data, cancellationToken);
			if (flushResult.IsCompleted)
			{
				total += data.Length;
			}
			else if (flushResult.IsCanceled)
			{
				break;
			}
		}
		return total;
	}

	protected override ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}
}
