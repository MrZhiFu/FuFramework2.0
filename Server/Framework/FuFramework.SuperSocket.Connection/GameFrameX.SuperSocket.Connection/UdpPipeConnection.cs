using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;

namespace FuFramework.SuperSocket.Connection;

/// <summary>
/// Represents a UDP-based pipe connection.
/// </summary>
public class UdpPipeConnection : VirtualConnection, IConnectionWithSessionIdentifier
{
	private Socket _socket;

	private bool _enableSendingPipe;

	/// <summary>
	/// Gets the session identifier for the connection.
	/// </summary>
	public string SessionIdentifier { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.UdpPipeConnection" /> class with the specified socket, connection options, and remote endpoint.
	/// </summary>
	/// <param name="socket">The socket used for the connection.</param>
	/// <param name="options">The connection options.</param>
	/// <param name="remoteEndPoint">The remote endpoint of the connection.</param>
	public UdpPipeConnection(Socket socket, ConnectionOptions options, IPEndPoint remoteEndPoint)
		: this(socket, options, remoteEndPoint, $"{remoteEndPoint.Address}:{remoteEndPoint.Port}")
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Connection.UdpPipeConnection" /> class with the specified socket, connection options, remote endpoint, and session identifier.
	/// </summary>
	/// <param name="socket">The socket used for the connection.</param>
	/// <param name="options">The connection options.</param>
	/// <param name="remoteEndPoint">The remote endpoint of the connection.</param>
	/// <param name="sessionIdentifier">The session identifier for the connection.</param>
	public UdpPipeConnection(Socket socket, ConnectionOptions options, IPEndPoint remoteEndPoint, string sessionIdentifier)
		: base(options)
	{
		_socket = socket;
		_enableSendingPipe = "true".Equals(options.Values?["enableSendingPipe"], StringComparison.OrdinalIgnoreCase);
		base.RemoteEndPoint = remoteEndPoint;
		SessionIdentifier = sessionIdentifier;
	}

	/// <summary>
	/// Closes the connection and completes the input writer.
	/// </summary>
	protected override void Close()
	{
		base.Input.Writer.Complete();
	}

	/// <summary>
	/// Throws a <see cref="T:System.NotSupportedException" /> as filling the pipe with data is not supported for UDP connections.
	/// </summary>
	/// <param name="memory">The memory buffer to fill with data.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	protected override ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Sends data over the UDP connection using the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>The total number of bytes sent.</returns>
	protected override async ValueTask<int> SendOverIoAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
	{
		if (_enableSendingPipe || buffer.IsSingleSegment)
		{
			int num = 0;
			ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ReadOnlyMemory<byte> current = enumerator.Current;
				int num2 = num;
				num = num2 + await _socket.SendToAsync(GetArrayByMemory(current), SocketFlags.None, base.RemoteEndPoint).ConfigureAwait(continueOnCapturedContext: false);
			}
			return num;
		}
		ArrayPool<byte> pool = ArrayPool<byte>.Shared;
		byte[] destBuffer = pool.Rent((int)buffer.Length);
		try
		{
			MergeBuffer(ref buffer, destBuffer);
			return await _socket.SendToAsync(new ArraySegment<byte>(destBuffer, 0, (int)buffer.Length), SocketFlags.None, base.RemoteEndPoint).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			pool.Return(destBuffer);
		}
	}

	/// <summary>
	/// Processes send operations for the connection.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	protected override Task ProcessSends()
	{
		if (_enableSendingPipe)
		{
			return base.ProcessSends();
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// Merges the specified buffer into the destination buffer.
	/// </summary>
	/// <param name="buffer">The source buffer to merge.</param>
	/// <param name="destBuffer">The destination buffer to fill.</param>
	private void MergeBuffer(ref ReadOnlySequence<byte> buffer, byte[] destBuffer)
	{
		Span<byte> destination = destBuffer;
		int num = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = buffer.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> current = enumerator.Current;
			current.Span.CopyTo(destination);
			num += current.Length;
			destination = destination.Slice(current.Length);
		}
	}

	/// <summary>
	/// Sends data asynchronously using the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the data to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		if (_enableSendingPipe)
		{
			await base.SendAsync(buffer, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			await SendOverIoAsync(new ReadOnlySequence<byte>(buffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	/// <summary>
	/// Sends a package asynchronously using the specified encoder and package.
	/// </summary>
	/// <typeparam name="TPackage">The type of the package.</typeparam>
	/// <param name="packageEncoder">The encoder used to encode the package.</param>
	/// <param name="package">The package to send.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async ValueTask SendAsync<TPackage>(IPackageEncoder<TPackage> packageEncoder, TPackage package, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_enableSendingPipe)
		{
			await base.SendAsync(packageEncoder, package, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		try
		{
			await base.SendLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			PipeWriter outputWriter = base.OutputWriter;
			WritePackageWithEncoder(outputWriter, packageEncoder, package);
			await outputWriter.FlushAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			await ProcessOutputRead(base.Output.Reader).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			base.SendLock.Release();
		}
	}

	/// <summary>
	/// Sends data asynchronously using the specified write action.
	/// </summary>
	/// <param name="write">The action to write data to the pipe writer.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken)
	{
		if (_enableSendingPipe)
		{
			await base.SendAsync(write, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		throw new NotSupportedException("The method SendAsync(Action<PipeWriter> write) cannot be used when noSendingPipe is true.");
	}
}
