using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Connection;

public class StreamPipeConnection : PipeConnection, IStreamConnection
{
	private Stream _stream;

	Stream IStreamConnection.Stream => _stream;

	public StreamPipeConnection(Stream stream, EndPoint remoteEndPoint, ConnectionOptions options)
		: this(stream, remoteEndPoint, null, options)
	{
	}

	public StreamPipeConnection(Stream stream, EndPoint remoteEndPoint, EndPoint localEndPoint, ConnectionOptions options)
		: base(options)
	{
		_stream = stream;
		base.RemoteEndPoint = remoteEndPoint;
		base.LocalEndPoint = localEndPoint;
	}

	protected override void Close()
	{
		_stream.Close();
	}

	protected override void OnClosed()
	{
		_stream = null;
		base.OnClosed();
	}

	protected override async ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
	{
		return await _stream.ReadAsync(memory, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	protected override async ValueTask<int> SendOverIoAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
	{
		int total = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> data = enumerator.Current;
			await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			total += data.Length;
		}
		await _stream.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return total;
	}

	protected override bool IsIgnorableException(Exception e)
	{
		if (base.IsIgnorableException(e))
		{
			return true;
		}
		if (e is SocketException se && se.IsIgnorableSocketException())
		{
			return true;
		}
		return false;
	}
}
